﻿using DocumentFormat.OpenXml.Spreadsheet;
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
    
    [Authorize(Roles = SD.Role_Admin)]
    public class BrandController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
       
        public BrandController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        
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
        
        // GET: Brand/Details/5
        //public async Task<IActionResult> Details(int id)
        //{
        //    var brand = await _unitOfWork.Brands.FirstOrDefaultAsync(m => m.Id == id);
        //    if (brand == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(brand);
        //}
        //Ph??ng th?c này tr? v? view ?? t?o m?i m?t th??ng hi?u.
        public IActionResult Create()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Brand brand)
        {
            if (ModelState.IsValid)
            {
                brand.Name = brand.Name.Trim();
                var normalizedBrandName = brand.Name.ToLower();
                if (_unitOfWork.Brands.Any(e => e.Name.ToLower() == normalizedBrandName))
                {
                    ModelState.AddModelError("name", "Tên nhãn đã tồn tại!");
                    return Create();
                }
                brand.Created = DateTime.Now;
                brand.Edited = DateTime.Now;
                await _unitOfWork.Brands.AddAsync(brand);
                await _unitOfWork.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thương hiệu đã được thêm thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }
        
        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _unitOfWork.Brands.FirstOrDefaultAsync(e => e.Id == id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Brand brand)
        {
            var brandFromDb = await _unitOfWork.Brands.FirstOrDefaultAsync(e => e.Id == id);
            
            if (id != brand.Id || brandFromDb == null)
            {
                return NotFound();
            }
            
            var brandNameLower = brand.Name.Trim().ToLower();

            var brands = await _unitOfWork.Brands.GetAllAsync(); // Giả sử có phương thức GetAllAsync() để lấy tất cả các màu
            var existingBrand = brands
                .FirstOrDefault(c => c.Name.Trim().ToLower() == brandNameLower && c.Id != brand.Id);
            //var existingBrand = await _unitOfWork.Brands.FirstOrDefaultAsync(e => e.Name == brand.Name && e.Id != id);
            if (existingBrand != null)
            {
                ModelState.AddModelError("Name", "Tên nhãn hàng đã tồn tại.");
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
        
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _unitOfWork.Brands.FirstOrDefaultAsync(m => m.Id == id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }
        
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
                TempData["SuccessMessage"] = "Thông tin nhãn hàng đã được xóa thành công";
            }
            else
            {
                TempData["ErrorMessage"] = "Một số mẫu giày thuộc về Thương hiệu này. Không thể xóa nó!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
