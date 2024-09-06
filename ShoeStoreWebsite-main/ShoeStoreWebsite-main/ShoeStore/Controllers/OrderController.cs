using System.Security.Claims;
using System.Text;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.DataAccess.Repository.IRepository;
using ShoeStore.Models;
using ShoeStore.Models.ViewModels;
using ShoeStore.Ultitity;
using Stripe;
using Stripe.Checkout;
using X.PagedList;
using DocumentFormat.OpenXml;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ShoeStore.Controllers;

public class OrderController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index(string? status, string? phoneNumber, int? page)
    {
        IEnumerable<ShopOrder> orders;

        if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
        {
            orders = await _unitOfWork.Orders
                .GetAllAsync(include: e =>
                    e.Include(e => e.ApplicationUser));
        }
        else
        {
            ClaimsIdentity? claimsIdentity = (ClaimsIdentity?)User.Identity;
            Claim? claim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);
            string? applicationUserId = claim?.Value;

            if (applicationUserId != null)
            {
                orders = await _unitOfWork.Orders
                    .GetAllAsync(e => e.ApplicationUserId == applicationUserId,
                        include: e =>
                            e.Include(e => e.ApplicationUser));
            }
            else if (phoneNumber == null)
            {
                return RedirectToAction("SearchOrder");
            }
            else
            {
                orders = await _unitOfWork.Orders.GetAllAsync(e => e.PhoneNumber == phoneNumber,
                    include: e =>
                        e.Include(e => e.ApplicationUser));
            }
        }

        switch (status)
        {
            case "pending":
                orders = orders.Where(e => e.PaymentStatus == SD.PaymentStatusDelayedPayment);
                break;
            case "inprocess":
                orders = orders.Where(e => e.OrderStatus == SD.StatusInpProcess);
                break;
            case "completed":
                orders = orders.Where(e => e.OrderStatus == SD.StatusShipped);
                break;
            case "approved":
                orders = orders.Where(e => e.OrderStatus == SD.StatusApproved);
                break;
            case "all":
                break;
        }

        int pageNumber = page ?? 1;
        int pageSize = 10;
        var paginatedOrders = orders.ToPagedList(pageNumber,pageSize);
        return View(paginatedOrders);
    }

    public IActionResult SearchOrder()
    {
        return View();
    }

    public async Task<IActionResult> Details(int id)
    {
        ShopOrder? orderFromDb = await _unitOfWork.Orders.FirstOrDefaultAsync(e => e.Id == id,
            include: o => o.Include(e => e.ApplicationUser));
        if (orderFromDb == null)
        {
            return NotFound();
        }

        OrderViewModel orderViewModel = new OrderViewModel()
        {
            Order = orderFromDb,
            OrderDetails = await _unitOfWork.OrderDetails.GetAllAsync(e => e.OrderId == id,
                include: o => o
                    .Include(e => e.ShoeSize)
                    .ThenInclude(e => e.ShoeColor)
                    .ThenInclude(e => e.Shoe)
                    .Include(e => e.ShoeSize)
                    .ThenInclude(e => e.ShoeColor)
                    .ThenInclude(e => e.Color)!
                    .Include(e => e.ShoeSize)
                    .ThenInclude(e => e.Size)
            )
        };

        return View(orderViewModel);
    }

    [ActionName(nameof(Details))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Details_PAY_NOW(OrderViewModel orderViewModel)
    {
        ShopOrder? orderFromDb = await _unitOfWork.Orders.FirstOrDefaultAsync(
            e => orderViewModel.Order != null && e.Id == orderViewModel.Order.Id,
            include: o => o.Include(e => e.ApplicationUser));
        if (orderFromDb == null)
        {
            return NotFound();
        }

        orderViewModel.Order = orderFromDb;

        orderViewModel.OrderDetails = await _unitOfWork.OrderDetails.GetAllAsync(e => e.OrderId == orderFromDb.Id,
            include: o => o.Include(e => e.ShoeSize));

        //--------
        // stripe settings
        var domain = $"{Request.Scheme}://{Request.Host.Value}";
        // var options = new SessionCreateOptions
        var options = new SessionCreateOptions
        {
            LineItems = new List<SessionLineItemOptions>(),
            Mode = "payment",
            SuccessUrl = $"{domain}/Cart/PaymentConfirmation?id={orderViewModel.Order.Id}",
            CancelUrl = $"{domain}/Order/Details?id={orderViewModel.Order.Id}",
            PaymentMethodTypes = new List<string>()
            {
                "card",
            },
        };

        foreach (var orderDetail in orderFromDb.OrderDetails)
        {
            ShoeColor shoeColor = (await _unitOfWork.ShoeColors.FirstOrDefaultAsync(
                e => e.ShoeSizes!.Any(ss => ss.Id == orderDetail.ShoeSizeId),
                include: o => o.Include(e => e.Shoe)
                    .Include(e => e.Images)!
                    .Include(e => e.Color)!
            ))!;

            var s = shoeColor?.Images?.Select(e => Url.Content(e.Path)).ToList() ?? new List<string>();

            var sessionLineItem = new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(orderDetail.PriceEach * 100),
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"{shoeColor.Shoe.Name} {shoeColor.Color?.Name}",
                        Images = shoeColor?.Images?.Select(e => $"{domain}{e.Path}").ToList() ?? new List<string>(),
                    },
                },
                Quantity = orderDetail.Count,
            };

            options.LineItems.Add(sessionLineItem);
        }

        var service = new SessionService();
        Session session = service.Create(options);

        await _unitOfWork.Orders.UpdateStripePaymentId(orderViewModel.Order.Id, session.Id, session.PaymentIntentId);
        await _unitOfWork.SaveChangesAsync();

        Response.Headers.Add("Location", session.Url);
        return new StatusCodeResult(303);
        //--------
    }

    public async Task<IActionResult> PaymentConfirmation(int id)
    {
        ShopOrder? order = await _unitOfWork.Orders.FirstOrDefaultAsync(e => e.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        if (order.PaymentStatus == SD.PaymentStatusDelayedPayment)
        {
            var service = new SessionService();
            Session session = service.Get(order.SessionId);
            // check the stripe status
            if (session.PaymentStatus.ToLower() == "paid")
            {
                await _unitOfWork.Orders.UpdateStatusAsync(id, order.OrderStatus, SD.PaymentStatusApproved);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        return View(id);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
    public async Task<IActionResult> UpdateOrderDetail(OrderViewModel orderViewModel)
    {
        ShopOrder? orderFromDb = await _unitOfWork.Orders.FirstOrDefaultAsync(
            e => orderViewModel.Order != null && e.Id == orderViewModel.Order.Id,
            include: o => o.Include(e => e.ApplicationUser));
        if (orderFromDb == null)
        {
            return NotFound();
        }

        if (orderViewModel.Order != null)
        {
            orderFromDb.Name = orderViewModel.Order.Name;
            orderFromDb.PhoneNumber = orderViewModel.Order.PhoneNumber;
            orderFromDb.Address = orderViewModel.Order.Address;
            orderFromDb.City = orderViewModel.Order.City;
            orderFromDb.District = orderViewModel.Order.District;
            orderFromDb.Ward = orderViewModel.Order.Ward;
            orderFromDb.PostalCode = orderViewModel.Order.PostalCode;

            if (orderViewModel.Order.Carrier != null)
            {
                orderFromDb.Carrier = orderViewModel.Order.Carrier;
            }

            if (orderViewModel.Order.TrackingNumber != null)
            {
                orderFromDb.TrackingNumber = orderViewModel.Order.TrackingNumber;
            }
        }

        _unitOfWork.Orders.Update(orderFromDb);
        await _unitOfWork.SaveChangesAsync();
        TempData[SD.Success] = "Chi tiết đơn hàng được cập nhật thành công!";

        return RedirectToAction("Details", "Order", new { id = orderFromDb.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
    public async Task<IActionResult> StartProcessing(OrderViewModel orderViewModel)
    {
        ShopOrder? orderFromDb = await _unitOfWork.Orders.FirstOrDefaultAsync(
            e => orderViewModel.Order != null && e.Id == orderViewModel.Order.Id,
            include: o => o.Include(e => e.ApplicationUser));

        if (orderFromDb == null)
        {
            return NotFound();
        }

        await _unitOfWork.Orders.UpdateStatusAsync(orderFromDb.Id, SD.StatusInpProcess);
        await _unitOfWork.SaveChangesAsync();
        TempData[SD.Success] = "Trạng thái đơn hàng được cập nhật thành công!";

        return RedirectToAction("Details", "Order", new { id = orderFromDb.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
    public async Task<IActionResult> ShipOrder(OrderViewModel orderViewModel)
    {
        ShopOrder? orderFromDb = await _unitOfWork.Orders.FirstOrDefaultAsync(
            e => orderViewModel.Order != null && e.Id == orderViewModel.Order.Id,
            include: o => o.Include(e => e.ApplicationUser));

        if (orderFromDb == null)
        {
            return NotFound();
        }

        if (orderViewModel.Order != null)
        {
            orderFromDb.ShippingDate = DateTime.Now;
            orderFromDb.OrderStatus = SD.StatusShipped;
            orderFromDb.TrackingNumber = orderViewModel.Order.TrackingNumber;
            orderFromDb.Carrier = orderViewModel.Order.Carrier;
            if (orderFromDb.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderFromDb.PaymentDueDate = DateTime.Now.AddDays(30);
            }
        }

        _unitOfWork.Orders.Update(orderFromDb);
        await _unitOfWork.SaveChangesAsync();
        TempData[SD.Success] = "Trạng thái đơn hàng được cập nhật thành công!";

        return RedirectToAction("Details", "Order", new { id = orderFromDb.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
    public async Task<IActionResult> CancelOrder(OrderViewModel orderViewModel)
    {
        ShopOrder? orderFromDb = await _unitOfWork.Orders.FirstOrDefaultAsync(
            e => orderViewModel.Order != null && e.Id == orderViewModel.Order.Id,
            include: o => o.Include(e => e.ApplicationUser));

        if (orderFromDb == null)
        {
            return NotFound();
        }

        // begin transaction
        var transaction = await _unitOfWork.BeginTransactionAsync();

        List<OrderDetail> orderDetails = await _unitOfWork.OrderDetails.GetAllAsync(e => e.OrderId == orderFromDb.Id);

        foreach (var orderDetail in orderDetails)
        {
            ShoeSize? shoeSize =
                await _unitOfWork.ShoeSizes.FirstOrDefaultAsync(e => e.Id == orderDetail.ShoeSizeId);
            if (shoeSize == null)
            {
                throw new Exception("Kích thước giày không tồn tại!");
            }

            shoeSize.Quantity += orderDetail.Count;
            _unitOfWork.ShoeSizes.Update(shoeSize);
        }

        if (orderFromDb.PaymentStatus == SD.PaymentStatusApproved)
        {
            var options = new RefundCreateOptions()
            {
                Reason = RefundReasons.RequestedByCustomer,
                PaymentIntent = orderFromDb.PaymentIntentId,
            };

            //var service = new RefundService();
            //Refund refund = await service.CreateAsync(options);

            await _unitOfWork.Orders.UpdateStatusAsync(orderFromDb.Id, SD.StatusCancelled, SD.StatusRefunded);
        }
        else
        {
            await _unitOfWork.Orders.UpdateStatusAsync(orderFromDb.Id, SD.StatusCancelled, SD.StatusCancelled);
        }

        await _unitOfWork.SaveChangesAsync();

        await transaction.CommitAsync();
        // end Transaction

        TempData[SD.Success] = "Đơn hàng đã được hủy thành công!";

        return RedirectToAction("Details", "Order", new { id = orderFromDb.Id });
    }

    public async Task<IActionResult> ExportToExcel(int id)
    {
        ShopOrder? orderFromDb = await _unitOfWork.Orders.FirstOrDefaultAsync(e => e.Id == id,
            include: o => o.Include(e => e.ApplicationUser));
        if (orderFromDb == null)
        {
            return NotFound();
        }

        var orderDetails = await _unitOfWork.OrderDetails.GetAllAsync(e => e.OrderId == id,
            include: o => o
                .Include(e => e.ShoeSize)
                .ThenInclude(e => e.ShoeColor)
                .ThenInclude(e => e.Shoe)
                .Include(e => e.ShoeSize)
                .ThenInclude(e => e.ShoeColor)
                .ThenInclude(e => e.Color)!
                .Include(e => e.ShoeSize)
                .ThenInclude(e => e.Size)
        );

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Order Details");

        // Adding header with title
        worksheet.Cells[1, 1].Value = "Chi tiết đơn hàng";
        worksheet.Cells[1, 1, 1, 7].Merge = true;
        worksheet.Cells[1, 1].Style.Font.Size = 16;
        worksheet.Cells[1, 1].Style.Font.Bold = true;
        worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells[1, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        worksheet.Cells[1, 1].Style.Border.Top.Style = ExcelBorderStyle.Thick;
        worksheet.Cells[1, 1].Style.Border.Left.Style = ExcelBorderStyle.Thick;
        worksheet.Cells[1, 1].Style.Border.Right.Style = ExcelBorderStyle.Thick;
        worksheet.Cells[1, 1].Style.Border.Bottom.Style = ExcelBorderStyle.Thick;

        // Adding order information
        worksheet.Cells[3, 2].Value = "Mã đơn hàng:";
        worksheet.Cells[3, 3].Value = orderFromDb.Id;
        worksheet.Cells[4, 2].Value = "Tên:";
        worksheet.Cells[4, 3].Value = orderFromDb.Name;
        worksheet.Cells[5, 2].Value = "Số điện thoại:";
        worksheet.Cells[5, 3].Value = orderFromDb.PhoneNumber;
        worksheet.Cells[6, 2].Value = "Địa chỉ:";
        worksheet.Cells[6, 3].Value = orderFromDb.Address;
        worksheet.Cells[7, 2].Value = "Thành phố:";
        worksheet.Cells[7, 3].Value = orderFromDb.City;
        worksheet.Cells[8, 2].Value = "Quận:";
        worksheet.Cells[8, 3].Value = orderFromDb.District;
        worksheet.Cells[9, 2].Value = "Phường:";
        worksheet.Cells[9, 3].Value = orderFromDb.Ward;
        worksheet.Cells[10, 2].Value = "Mã bưu điện:";
        worksheet.Cells[10, 3].Value = orderFromDb.PostalCode;
        worksheet.Cells[11, 2].Value = "Email:";
        worksheet.Cells[11, 3].Value = orderFromDb.ApplicationUser.Email;
        worksheet.Cells[12, 2].Value = "Ngày đặt hàng:";
        worksheet.Cells[12, 3].Value = orderFromDb.OrderDate.ToShortDateString();
        worksheet.Cells[13, 2].Value = "Người giao hàng:";
        worksheet.Cells[13, 3].Value = orderFromDb.Carrier;
        worksheet.Cells[14, 2].Value = "Tracking Number:";
        worksheet.Cells[14, 3].Value = orderFromDb.TrackingNumber;
        worksheet.Cells[15, 2].Value = "Ngày vận chuyển:";
        worksheet.Cells[15, 3].Value = orderFromDb.ShippingDate.ToShortDateString();
        worksheet.Cells[16, 2].Value = "Ngày thanh toán:";
        worksheet.Cells[16, 3].Value = orderFromDb.PaymentDueDate.ToShortDateString();
        worksheet.Cells[17, 2].Value = "Tình trạng thanh toán:";
        worksheet.Cells[17, 3].Value = orderFromDb.PaymentStatus;

        // Add a space before the table
        worksheet.Cells[19, 1].Value = "Bảng thông tin đơn hàng";
        worksheet.Cells[19, 1, 19, 7].Merge = true;
        worksheet.Cells[19, 1].Style.Font.Size = 14;
        worksheet.Cells[19, 1].Style.Font.Bold = true;
        worksheet.Cells[19, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells[19, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

        // Adding table headers
        worksheet.Cells[21, 1].Value = "Mã đơn hàng";
        worksheet.Cells[21, 2].Value = "Tên giày";
        worksheet.Cells[21, 3].Value = "Màu sắc";
        worksheet.Cells[21, 4].Value = "Kích thước";
        worksheet.Cells[21, 5].Value = "Đơn giá";
        worksheet.Cells[21, 6].Value = "Số lượng";
        worksheet.Cells[21, 7].Value = "Tổng tiền";

        // Apply bold and border to the header row
        using (var range = worksheet.Cells[21, 1, 21, 7])
        {
            range.Style.Font.Bold = true;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thick;
        }

        // Adding data rows
        int row = 22;
        foreach (var detail in orderDetails)
        {
            worksheet.Cells[row, 1].Value = detail.OrderId;
            worksheet.Cells[row, 2].Value = detail.ShoeSize.ShoeColor.Shoe.Name;
            worksheet.Cells[row, 3].Value = detail.ShoeSize.ShoeColor.Color.Name;
            worksheet.Cells[row, 4].Value = detail.ShoeSize.Size.Value;
            worksheet.Cells[row, 5].Value = detail.PriceEach;
            worksheet.Cells[row, 6].Value = detail.Count;
            worksheet.Cells[row, 7].Value = detail.Count * detail.PriceEach;
            row++;
        }

        // Formatting the table
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        // Formatting the entire sheet
        //worksheet.Cells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        //worksheet.Cells.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        //worksheet.Cells.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        //worksheet.Cells.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

        var stream = new MemoryStream();
        package.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"Order_{id}_Details.xlsx";
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}