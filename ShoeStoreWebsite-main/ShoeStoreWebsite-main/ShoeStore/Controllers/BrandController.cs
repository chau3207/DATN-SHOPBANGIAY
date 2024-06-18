using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;
using ShoeStore.DataAccess.Repository.IRepository;
using ShoeStore.Models;
using ShoeStore.Ultitity;
using System.Security.Cryptography;
using X.PagedList;

namespace ShoeStore.Controllers
{
    //là m?t attribute c?a controller ?? yêu c?u xác th?c và ?y quy?n, 
    //ch? cho phép ng??i dùng v?i vai trò "Admin" truy c?p các ph??ng th?c 
    //trong controller.
    [Authorize(Roles = SD.Role_Admin)]
    public class BrandController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        //Constructor này nh?n m?t ??i t??ng IUnitOfWork thông qua Dependency Injection.IUnitOfWork ???c 
        //s? d?ng ?? t??ng tác v?i c? s? d? li?u thông qua các repository.
        public BrandController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        //Ph??ng th?c này tr? v? m?t view ch?a danh sách t?t c? các th??ng hi?u 
        //trong h? th?ng, s? d?ng repository ?? l?y d? li?u.
        // GET: Brand
        //public async Task<IActionResult> Index()
        //{
        //      return View(await _unitOfWork.Brands.GetAllAsync());
        //}

        public async Task<IActionResult> Index(int? page)
        {
            int pageNumber = page ?? 1;
            int pageSize = 10; // Số lượng mục trên mỗi trang

            var brands = await _unitOfWork.Brands.GetAllAsync();
            var paginatedBrands = brands.ToPagedList(pageNumber, pageSize);

            return View(paginatedBrands);
        }
        //Ph??ng th?c này tr? v? view chi ti?t c?a m?t th??ng hi?u d?a trên ID.
        //N?u không tìm th?y th??ng hi?u, tr? v? 404 Not Found.
        // GET: Brand/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var brand = await _unitOfWork.Brands.FirstOrDefaultAsync(m => m.Id == id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }
        //Ph??ng th?c này tr? v? view ?? t?o m?i m?t th??ng hi?u.
        public IActionResult Create()
        {
            return View();
        }
        //Ph??ng th?c này x? lý vi?c t?o m?i th??ng hi?u d?a trên d? li?u ???c g?i t? form.
        //S? d?ng[Bind] ?? ch? bind nh?ng tr??ng c?n thi?t.
        //N?u d? li?u h?p l?, thêm th??ng hi?u m?i vào c? s? d? li?u và chuy?n h??ng 
        //v? trang danh sách th??ng hi?u.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Brand brand)
        {
            if (ModelState.IsValid)
            {
                brand.Created = DateTime.Now;
                brand.Edited = DateTime.Now;
                await _unitOfWork.Brands.AddAsync(brand);
                await _unitOfWork.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thương hiệu đã được thêm thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }
        //Ph??ng th?c này tr? v? view ?? ch?nh s?a thông tin c?a m?t th??ng hi?u d?a trên ID.
        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _unitOfWork.Brands.FirstOrDefaultAsync(e => e.Id == id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }
        //Ph??ng th?c này x? lý vi?c c?p nh?t thông tin th??ng hi?u d?a trên 
        //d? li?u ???c g?i t? form.
        //N?u d? li?u h?p l?, c?p nh?t th??ng hi?u trong c? s? d? li?u và 
        //chuy?n h??ng v? trang danh sách th??ng hi?u.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Brand brand)
        {
            var brandFromDb = await _unitOfWork.Brands.FirstOrDefaultAsync(e => e.Id == id);
            
            if (id != brand.Id || brandFromDb == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                brand.Created = brandFromDb.Created;
                brand.Edited = DateTime.Now;
                _unitOfWork.Brands.Update(brand);
                await _unitOfWork.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thông tin đã được sửa thành công";
                return RedirectToAction("Index");
            }

            return View(brand);
        }
        //Ph??ng th?c này tr? v? view ?? xác nh?n xóa m?t th??ng hi?u d?a trên ID.
        // GET: Brand/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _unitOfWork.Brands.FirstOrDefaultAsync(m => m.Id == id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }
        //Ph??ng th?c này x? lý vi?c xóa th??ng hi?u d?a trên ID.
        //N?u có các mô hình giày thu?c th??ng hi?u này, thông báo l?i và không 
        //xóa; ng??c l?i, xóa th??ng hi?u và chuy?n h??ng v? trang danh sách 
        //th??ng hi?u.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var brand = await _unitOfWork.Brands.FirstOrDefaultAsync(e => e.Id == id);
            if (brand == null)
            {
                return NotFound();
            }

            var shoes = await _unitOfWork.Shoes.GetAllAsync(e => e.BrandId == brand.Id);
            if(shoes.Count == 0)
            {
                _unitOfWork.Brands.Remove(brand);
                await _unitOfWork.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thông tin đã được xóa thành công";
            }
            else
            {
                TempData[SD.Error] = "Some shoe models is belong to this Brand. Can not delete it!";
                
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
