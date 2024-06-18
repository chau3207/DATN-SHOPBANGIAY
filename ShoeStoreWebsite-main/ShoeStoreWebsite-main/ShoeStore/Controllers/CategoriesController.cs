using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoeStore.DataAccess.Repository.IRepository;
using ShoeStore.Models;
using ShoeStore.Ultitity;
using System;
using System.Security.Cryptography;
using X.PagedList;

namespace ShoeStore.Controllers
{
    //?o?n mã này ??m b?o ch? nh?ng ng??i dùng có vai trò "Admin" m?i ???c ?y quy?n 
    //truy c?p các hành ??ng trong controller này.Tên vai trò ???c ??nh ngh?a 
    //trong l?p SD (ShoeStore.Utility).
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoriesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        //Controller được khởi tạo với sự phụ thuộc vào IUnitOfWork,
        //được sử dụng để tương tác với lớp dữ liệu.
        public CategoriesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        //Hành động này lấy danh sách các danh mục từ cơ sở dữ liệu sử dụng
        //IUnitOfWork 
        //và hiển thị trang web "Index".
        public async Task<IActionResult> Index(int? page)
        {
            int pageNumber = page ?? 1;
            int pageSize = 10;
            var categories = await _unitOfWork.Categories.GetAllAsync(include: o => o.Include(e => e.Parent)!);
            var paginatedCategories = categories.ToPagedList(pageNumber, pageSize);
            return View(paginatedCategories);
        }
        //Hành động này lấy chi tiết về một danh mục cụ thể dựa trên id được 
        //cung cấp và hiển thị trang web "Details".
        public async Task<IActionResult> Details(int id)
        {
            // var category = await _unitOfWork.Categories
            //     .FirstOrDefaultAsyncWithParent(m => m.Id == id);
            var category = await _unitOfWork.Categories
                .FirstOrDefaultAsync(e => e.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }
        //Các hành động này xử lý việc tạo một danh mục mới.Hành động
        //đầu tiên hiển thị trang web "Create" với các tùy chọn để chọn
        //danh mục cha, và hành động thứ hai xử lý việc gửi biểu mẫu.
        public async Task<IActionResult> Create()
        {
            ViewBag.ParentId = new SelectList(await _unitOfWork.Categories.GetAllAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,DisplayName,Description,ParentId")] Category category)
        {
            if (ModelState.IsValid)
            {
                await _unitOfWork.Categories.AddAsync(category);
                await _unitOfWork.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thông tin danh mục được thêm thành công";
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }
        //Tương tự như các hành động tạo, những hành động này xử lý việc sửa 
        //đổi một danh mục đã tồn tại.Hành động đầu tiên hiển thị trang web 
        //"Edit", và hành động thứ hai xử lý việc gửi biểu mẫu.
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(e => e.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            List<Category> list = (await _unitOfWork.Categories.GetAllAsync()).ToList();
            list.RemoveAll(e => e.Id == category.Id);

            ViewBag.ParentId = new SelectList(list, "Id", "Name");

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("Id,Name,DisplayName,Description,ParentId")] Category category)
        {
            var categoryFromDb = await _unitOfWork.Categories.FirstOrDefaultAsync(e => e.Id == category.Id);

            if (categoryFromDb == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var list = await _unitOfWork.Categories.GetAllAsync();
                list.RemoveAll(e => e.Id == category.Id);

                ViewBag.ParentId = new SelectList(list, "Id", "Name");
                return View(category);
            }

            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveChangesAsync();
            TempData["SuccessMessage"] = "Danh mục được sửa thành công";
            return RedirectToAction(nameof(Index));
        }
        //Các hành động này xử lý việc xóa một danh mục.Hành động 
        //đầu tiên hiển thị trang web "Delete", và hành động thứ hai xử lý việc 
        //gửi biểu mẫu.Trước khi xóa, nó kiểm tra xem có bất kỳ mô hình giày 
        //nào liên quan đến danh mục không và ngăn chặn xóa nếu có.
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(e => e.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            var shoes = await _unitOfWork.Shoes.GetAllAsync(e => e.CategoryId == category.Id);
            if (shoes.Count == 0)
            {
                _unitOfWork.Categories.Remove(category);
                await _unitOfWork.SaveChangesAsync();
                TempData["SuccessMessage"] = "Danh mục được xóa thành công";
            }
            else
            {
                TempData[SD.Error] = "Some ShoeModels is belong to this Category. Can not delete it!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}