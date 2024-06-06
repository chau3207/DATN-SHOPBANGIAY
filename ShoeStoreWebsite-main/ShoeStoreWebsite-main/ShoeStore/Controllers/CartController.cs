using System.Security.Claims;
using System.Security.Policy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NuGet.Configuration;
using ShoeStore.DataAccess.Repository.IRepository;
using ShoeStore.Models;
using ShoeStore.Models.ViewModels;
using ShoeStore.Ultitity;
using Stripe.Checkout;

namespace ShoeStore.Controllers;

public class CartController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public Cart Cart { get; set; }
    //Constructor này nhận một đối tượng IUnitOfWork 
    //thông qua Dependency Injection để tương tác với cơ sở dữ liệu.
    //Nhận một đối tượng Cart, đại diện cho giỏ hàng, cũng được đưa vào qua 
    //Dependency Injection.
    public CartController(IUnitOfWork unitOfWork, Cart cart)
    {
        _unitOfWork = unitOfWork;
        Cart = cart;
    }
    //Hiển thị giỏ hàng và chi tiết sản phẩm trong giỏ hàng.
    //Nếu người dùng đã đăng nhập, sử dụng IUnitOfWork để lấy danh
    //sách sản phẩm trong giỏ hàng từ cơ sở dữ liệu.
    //Nếu không, sử dụng đối tượng Cart trực tiếp.
    public async Task<IActionResult> Index(string? returnUrl)
    {
        CartViewModel cartViewModel = new CartViewModel()
        {
            ReturnUrl = returnUrl ?? "/",
        };

        ClaimsIdentity? claimsIdentity = (ClaimsIdentity?)User.Identity;
        Claim? claim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);
        string? applicationUserId = claim?.Value;

        List<CartItem> cartItemList;

        if (applicationUserId != null)
        {
            cartItemList = await _unitOfWork.CartItems.GetAllAsync(e => e.ApplicationUserId == applicationUserId);
        }
        else
        {
            cartItemList = Cart.CartItemsList;
        }

        foreach (var cartItem in cartItemList)
        {
            ShoeSize? shoeSize = await _unitOfWork.ShoeSizes.FirstOrDefaultAsync(e => e.Id == cartItem.ShoeSizeId,
                include: o => o.Include(e => e.Size)
                    .Include(e => e.ShoeColor)
                    .ThenInclude(e => e.Images)
                    .Include(e => e.ShoeColor)
                    .ThenInclude(e => e.Shoe)
                    .ThenInclude(e => e.Brand));

            if (shoeSize != null)
            {
                cartViewModel.ShopOrder.OrderTotal += shoeSize.ShoeColor.SalePrice * cartItem.Count;

                cartViewModel.ProductCartsList.Add(
                    new ProductCartViewModel()
                    {
                        ShoeSizeId = shoeSize.Id,
                        CartItemId = cartItem.Id,
                        ProductName =
                            $"{shoeSize.ShoeColor.Shoe.Name}",
                        Size = $"{shoeSize.Size?.Unit} {shoeSize.Size?.Value}",
                        BrandName = shoeSize.ShoeColor.Shoe?.Brand.Name,
                        Price = shoeSize.ShoeColor.SalePrice,
                        Quantity = cartItem.Count,
                        ImgPath = shoeSize.ShoeColor.Images?.First().Path,
                        ProductUrl = shoeSize.ShoeColor.Url!
                    }
                );
            }
        }

        return View(cartViewModel);
    }
    //Tăng số lượng của một sản phẩm trong giỏ hàng.
    //Nếu người dùng đã đăng nhập, sử dụng IUnitOfWork để cập nhật cơ sở dữ liệu.
    //Nếu không, sử dụng phương thức AddItem của đối tượng Cart.
    public async Task<IActionResult> Increment(int shoeSizeId, string? returnUrl)
    {
        ShoeSize? shoeSize = await _unitOfWork.ShoeSizes.FirstOrDefaultAsync(e => e.Id == shoeSizeId);

        if (shoeSize == null)
        {
            return NotFound();
        }

        ClaimsIdentity? claimsIdentity = (ClaimsIdentity?)User.Identity;
        Claim? claim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);
        string? applicationUserId = claim?.Value;

        CartItem? cartItem;

        if (applicationUserId != null)
        {
            cartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(e =>
                e.ApplicationUserId == applicationUserId && e.ShoeSizeId == shoeSizeId);
        }
        else
        {
            cartItem = Cart.CartItemsList.FirstOrDefault(e => e.ShoeSizeId == shoeSizeId);
        }

        if (cartItem == null || cartItem.Count < 1)
        {
            return NotFound();
        }


        if (cartItem.Count >= shoeSize.Quantity)
        {
            TempData[SD.Error] = "Can't add more item of this product!";
            return RedirectToAction("Index", new { returnUrl });
        }


        if (applicationUserId != null)
        {
            cartItem.Count++;
            _unitOfWork.CartItems.Update(cartItem);
            await _unitOfWork.SaveChangesAsync();
        }
        else
        {
            Cart.AddItem(shoeSizeId, 1);
        }

        TempData[SD.Success] = "Increase an item's quantity by 1!";

        return RedirectToAction("Index", new { returnUrl });
    }
    //Giảm số lượng của một sản phẩm trong giỏ hàng.
    //Nếu người dùng đã đăng nhập, sử dụng IUnitOfWork để cập nhật cơ sở dữ liệu.
    //Nếu không, sử dụng phương thức SubtractItem của đối tượng Cart.
    public async Task<IActionResult> Decrement(int shoeSizeId, string? returnUrl)
    {
        ShoeSize? shoeSize = await _unitOfWork.ShoeSizes.FirstOrDefaultAsync(e => e.Id == shoeSizeId);
        if (shoeSize == null)
        {
            return NotFound();
        }

        ClaimsIdentity? claimsIdentity = (ClaimsIdentity?)User.Identity;
        Claim? claim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);
        string? applicationUserId = claim?.Value;

        CartItem? cartItem;

        if (applicationUserId != null)
        {
            cartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(e =>
                e.ApplicationUserId == applicationUserId && e.ShoeSizeId == shoeSizeId);
        }
        else
        {
            cartItem = Cart.CartItemsList.FirstOrDefault(e => e.ShoeSizeId == shoeSizeId);
        }

        if (cartItem == null || cartItem.Count <= 0)
        {
            return NotFound();
        }

        if (cartItem.Count <= 1)
        {
            TempData[SD.Error] = "Please use remove button instead!";
            return RedirectToAction("Index", new { returnUrl });
        }

        if (applicationUserId != null)
        {
            cartItem.Count--;
            _unitOfWork.CartItems.Update(cartItem);
            await _unitOfWork.SaveChangesAsync();
        }
        else
        {
            Cart.SubtractItem(shoeSizeId, 1);
        }

        TempData[SD.Success] = "Decreased 1 item's quantity!";
        return RedirectToAction("Index", new { returnUrl });
    }
    //Xóa một sản phẩm khỏi giỏ hàng.
    //Nếu người dùng đã đăng nhập, sử dụng IUnitOfWork để cập nhật cơ sở dữ liệu.
    //Nếu không, sử dụng phương thức RemoveLine của đối tượng Cart.
    public async Task<IActionResult> Remove(int shoeSizeId, string? returnUrl)
    {
        ShoeSize? shoeSize = await _unitOfWork.ShoeSizes.FirstOrDefaultAsync(e => e.Id == shoeSizeId);
        if (shoeSize == null)
        {
            return NotFound();
        }

        ClaimsIdentity? claimsIdentity = (ClaimsIdentity?)User.Identity;
        Claim? claim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);
        string? applicationUserId = claim?.Value;

        CartItem? cartItem;

        if (applicationUserId != null)
        {
            cartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(e =>
                e.ApplicationUserId == applicationUserId && e.ShoeSizeId == shoeSizeId);
        }
        else
        {
            cartItem = Cart.CartItemsList.FirstOrDefault(e => e.ShoeSizeId == shoeSizeId);
        }

        if (cartItem == null)
        {
            return NotFound();
        }

        if (applicationUserId != null)
        {
            _unitOfWork.CartItems.Remove(cartItem);
            await _unitOfWork.SaveChangesAsync();
        }
        else
        {
            Cart.RemoveLine(shoeSizeId);
        }

        TempData[SD.Success] = "Removed 1 item from cart!";
        return RedirectToAction("Index", new { returnUrl });
    }
    //Hiển thị trang tóm tắt đơn hàng và thông tin thanh toán.
    //Nếu người dùng đã đăng nhập, điền thông tin người dùng từ cơ sở dữ liệu vào mô hình CartViewModel.
    //Tính tổng giá trị đơn hàng và hiển thị thông tin sản phẩm trong giỏ hàng.
    public async Task<IActionResult> Summary(string? returnUrl)
    {
        returnUrl ??= "/";
        CartViewModel cartViewModel = new CartViewModel()
        {
            ReturnUrl = returnUrl ?? "/",
        };

        ClaimsIdentity? claimsIdentity = (ClaimsIdentity?)User.Identity;
        Claim? claim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);
        string? applicationUserId = claim?.Value;

        List<CartItem> cartItemList;
        if (applicationUserId != null)
        {
            cartItemList = await _unitOfWork.CartItems.GetAllAsync(e => e.ApplicationUserId == applicationUserId);
        }
        else
        {
            cartItemList = Cart.CartItemsList;
        }

        if (cartItemList.Count <= 0)
        {
            TempData[SD.Error] = "You must add somethings to Cart first!";
            return Redirect(returnUrl!);
        }

        foreach (var cartItem in cartItemList)
        {
            ShoeSize? shoeSize = await _unitOfWork.ShoeSizes.FirstOrDefaultAsync(e => e.Id == cartItem.ShoeSizeId,
                include: o => o.Include(e => e.Size)
                    .Include(e => e.ShoeColor)
                    .ThenInclude(e => e.Images)
                    .Include(e => e.ShoeColor)
                    .ThenInclude(e => e.Shoe)
                    .ThenInclude(e => e.Brand));

            if (shoeSize != null)
            {
                if (shoeSize.Quantity < cartItem.Count || cartItem.Count <= 0 || shoeSize.Quantity <= 0)
                {
                    return BadRequest();
                }

                cartViewModel.ShopOrder.OrderTotal += shoeSize.ShoeColor.SalePrice * cartItem.Count;

                cartViewModel.ProductCartsList.Add(
                    new ProductCartViewModel()
                    {
                        ShoeSizeId = shoeSize.Id,
                        CartItemId = cartItem.Id,
                        ProductName =
                            $"{shoeSize.ShoeColor.Shoe!.Name}",
                        Size = $"{shoeSize.Size?.Unit} {shoeSize.Size?.Value}",
                        BrandName = shoeSize.ShoeColor.Shoe!.Brand!.Name,
                        Price = shoeSize.ShoeColor.SalePrice,
                        Quantity = cartItem.Count,
                        ImgPath = shoeSize.ShoeColor.Images?.First().Path,
                        ProductUrl = shoeSize.ShoeColor.Url!
                    }
                );
            }
        }

        if (applicationUserId != null)
        {
            ApplicationUser? applicationUser =
                await _unitOfWork.ApplicationUsers.FirstOrDefaultAsync(e => e.Id == claim.Value);

            cartViewModel.ShopOrder.ApplicationUserId = applicationUserId;

            cartViewModel.ShopOrder.Name = applicationUser.Name;
            cartViewModel.ShopOrder.PhoneNumber = applicationUser.PhoneNumber;
            cartViewModel.ShopOrder.Address = applicationUser.Address;
            cartViewModel.ShopOrder.City = applicationUser.City;
            cartViewModel.ShopOrder.District = applicationUser.District;
            cartViewModel.ShopOrder.Ward = applicationUser.Ward;
            cartViewModel.ShopOrder.PostalCode = applicationUser.PostalCode;
        }


        return View(cartViewModel);
    }
    //Xử lý việc đặt hàng và thanh toán.
    //Kiểm tra và cập nhật cơ sở dữ liệu với thông tin đơn hàng và chi tiết đơn hàng.
    //Sử dụng Stripe để xử lý thanh toán và tạo phiên thanh toán.
    //Cập nhật thông tin thanh toán và chuyển hướng người dùng đến trang thanh toán Stripe.
    [HttpPost]
    [ActionName(nameof(Summary))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SummaryPost(CartViewModel cartViewModel)
    {
        cartViewModel.ShopOrder.OrderTotal = 0;

        if (ModelState.IsValid)
        {
            ClaimsIdentity? claimsIdentity = (ClaimsIdentity?)User.Identity;
            Claim? claim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);
            string? applicationUserId = claim?.Value;

            List<CartItem> cartItemList;
            if (applicationUserId != null)
            {
                cartItemList = await _unitOfWork.CartItems.GetAllAsync(e => e.ApplicationUserId == applicationUserId);
                cartViewModel.ShopOrder.ApplicationUserId = applicationUserId;
            }
            else
            {
                cartItemList = Cart.CartItemsList;
            }
            
            if (cartItemList.Count <= 0)
            {
                TempData[SD.Error] = "You must add somethings to Cart first!";
                return RedirectToAction("Index", "Home");
            }

            cartViewModel.ShopOrder.PaymentStatus = SD.PaymentStatusPending;
            cartViewModel.ShopOrder.OrderStatus = SD.StatusPending;
            cartViewModel.ShopOrder.OrderDate = DateTime.Now;

            foreach (var cartItem in cartItemList)
            {
                ShoeSize? shoeSize = await _unitOfWork.ShoeSizes.FirstOrDefaultAsync(e => e.Id == cartItem.ShoeSizeId,
                    include: e => e.Include(e => e.ShoeColor));

                if (shoeSize != null)
                {
                    if (shoeSize.Quantity < cartItem.Count || cartItem.Count <= 0 || shoeSize.Quantity <= 0)
                    {
                        return BadRequest();
                    }

                    cartItem.PriceEach = shoeSize.ShoeColor.SalePrice;

                    cartViewModel.ShopOrder.OrderTotal += shoeSize.ShoeColor.SalePrice * cartItem.Count;
                }
            }

            // start Transaction
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _unitOfWork.Orders.AddAsync(cartViewModel.ShopOrder);
                await _unitOfWork.SaveChangesAsync();

                foreach (var cartItem in cartItemList)
                {
                    ShoeSize? shoeSize =
                        (await _unitOfWork.ShoeSizes.FirstOrDefaultAsync(e => e.Id == cartItem.ShoeSizeId));
                    if (shoeSize == null)
                    {
                        throw new Exception("ShoeSize does not exist!");
                    }

                    shoeSize.Quantity -= cartItem.Count;
                    _unitOfWork.ShoeSizes.Update(shoeSize);

                    OrderDetail orderDetail = new OrderDetail
                    {
                        ShoeSizeId = cartItem.ShoeSizeId,
                        Count = cartItem.Count,
                        PriceEach = cartItem.PriceEach,
                        OrderId = cartViewModel.ShopOrder.Id
                    };
                    await _unitOfWork.OrderDetails.AddAsync(orderDetail);
                    await _unitOfWork.SaveChangesAsync();
                }

                // commit Transaction
                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // TempData[SD.Error] = e.Message;
                TempData[SD.Error] = "Somethings wrong happen.\nCheckout Failed!";
                return RedirectToAction("Index", "Home");
            }

            // stripe settings
            var domain = $"{Request.Scheme}://{Request.Host.Value}";
            var options = new SessionCreateOptions
            {
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    SetupFutureUsage = "off_session"
                },
                SuccessUrl = $"{domain}/Cart/OrderConfirmation?id={cartViewModel.ShopOrder.Id}",
                CancelUrl = $"{domain}/Cart/OrderConfirmation?id={cartViewModel.ShopOrder.Id}",
                // CancelUrl = $"{domain}/Cart/Index",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                PaymentMethodTypes = new List<string>()
                {
                    "card",
                },
            };

            foreach (var cartItem in cartItemList)
            {
                ShoeColor shoeColor = (await _unitOfWork.ShoeColors.FirstOrDefaultAsync(
                    e => e.ShoeSizes!.Any(ss => ss.Id == cartItem.ShoeSizeId),
                    include: o => o.Include(e => e.Shoe)
                        .Include(e => e.Images)
                        .Include(e => e.Color)!
                ))!;

                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(cartItem.PriceEach * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{shoeColor.Shoe!.Name} {shoeColor.Color?.Name}",
                            Images = shoeColor.Images?.Select(e => $"{domain}{e.Path}").ToList() ?? new List<string>(),
                        },
                    },
                    Quantity = cartItem.Count,
                };

                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);

            await _unitOfWork.Orders.UpdateStripePaymentId(cartViewModel.ShopOrder.Id, session.Id,
                session.PaymentIntentId);
            await _unitOfWork.SaveChangesAsync();

            // clear Cart
            if (applicationUserId != null)
            {
                _unitOfWork.CartItems.RemoveRange(cartItemList);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                Cart.Clear();
            }

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        TempData[SD.Error] = "Please enter all field!";
        return View(cartViewModel);
    }
    //Hiển thị trang xác nhận đơn hàng.
    //Kiểm tra trạng thái thanh toán từ Stripe và cập nhật trạng thái đơn hàng trong cơ sở dữ liệu.
    public async Task<IActionResult> OrderConfirmation(int id)
    {
        ShopOrder? order = await _unitOfWork.Orders.FirstOrDefaultAsync(e => e.Id == id);

        if (order.PaymentStatus != SD.PaymentStatusDelayedPayment)
        {
            var service = new SessionService();
            Session session = service.Get(order.SessionId);
            // check the stripe status
            if (session.PaymentStatus.ToLower() == "paid")
            {
                await _unitOfWork.Orders.UpdateStatusAsync(id, SD.StatusApproved, SD.PaymentStatusApproved);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        return View(id);
    }
}