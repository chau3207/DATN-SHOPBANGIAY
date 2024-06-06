using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ShoeStore.DataAccess.Repository.IRepository;
using ShoeStore.Models;
using ShoeStore.Models.ViewModels;
using ShoeStore.Ultitity;

namespace ShoeStore.Controllers;
//cho biết chỉ những người dùng được ủy quyền với vai trò admin mới có thể truy
//cập các hành động trong admin controller
[Authorize(Roles = SD.Role_Admin)]
public class AdminController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    //dùng để tương tác dữ liệu thông qua Repository
    public AdminController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
//    Đây là phương thức xử lý yêu cầu khi người dùng truy cập trang chủ của trang quản trị.
//Sử dụng IUnitOfWork để lấy tất cả các đơn đặt hàng từ cơ sở dữ liệu.
    public async Task<IActionResult> Index()
    {
        List<ShopOrder> orders = await _unitOfWork.Orders.GetAllAsync();

        DateTime now = DateTime.Now;
        DateTime monday = StartOfWeek(DateTime.Now, DayOfWeek.Monday);
        DateTime firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
        DateTime firstDayOfYear = new DateTime(now.Year, 1, 1);

        int totalOrdersToday = orders
            .FindAll(e => e.OrderDate.ToString("MM/dd/yyyy") == DateTime.Now.ToString("MM/dd/yyyy")).Count;
        int totalOrdersThisWeek = orders.FindAll(e => e.OrderDate > monday).Count;
        int totalOrdersThisMonth = orders.FindAll(e => e.OrderDate > firstDayOfMonth).Count;

        decimal orderTotalsToday = orders
            .FindAll(e => e.OrderDate.ToString("MM/dd/yyyy") == DateTime.Now.ToString("MM/dd/yyyy"))
            .Sum(e => e.OrderTotal);
        decimal orderTotalsThisWeek = orders.FindAll(e => e.OrderDate > monday).Sum(e => e.OrderTotal);
        decimal orderTotalsThisMonth = orders.FindAll(e => e.OrderDate > firstDayOfMonth).Sum(e => e.OrderTotal);
        decimal orderTotalsThisYear = orders.FindAll(e => e.OrderDate > firstDayOfYear).Sum(e => e.OrderTotal);

        DashBoardViewModel dashBoardViewModel = new DashBoardViewModel()
        {
            //            Sử dụng một số thống kê về đơn đặt hàng để hiển thị trên bảng điều khiển (dashboard).
            //totalOrdersToday, totalOrdersThisWeek, totalOrdersThisMonth: Số lượng đơn đặt hàng trong ngày, tuần và tháng hiện tại.
            //orderTotalsToday, orderTotalsThisWeek, orderTotalsThisMonth, orderTotalsThisYear: Tổng giá trị đơn đặt hàng trong ngày, tuần, tháng và năm hiện tại.
            //Dữ liệu được đặt vào một đối tượng DashBoardViewModel, sau đó truyền vào view để hiển thị trên trang chủ của trang quản trị.
            OrderTotalsToday = orderTotalsToday,
            OrderTotalsThisWeek = orderTotalsThisWeek,
            OrderTotalsThisMonth = orderTotalsThisMonth,
            OrderTotalsThisYear = orderTotalsThisYear,
            TotalOrdersToday = totalOrdersToday,
            TotalOrdersThisWeek = totalOrdersThisWeek,
            TotalOrdersThisMonth = totalOrdersThisMonth,
        };

        return View(dashBoardViewModel);
    }

    private static DateTime StartOfWeek(DateTime dt, DayOfWeek startOfWeek)
    {
        //Một phương thức tĩnh để tính ngày bắt đầu của tuần từ một ngày cụ thể.
        int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
        return dt.AddDays(-1 * diff).Date;
    }
}