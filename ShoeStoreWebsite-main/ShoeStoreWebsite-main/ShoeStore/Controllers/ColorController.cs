using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoeStore.DataAccess.Repository.IRepository;
using ShoeStore.Models;
using ShoeStore.Ultitity;
using X.PagedList;

namespace ShoeStore.Controllers
{
    [Authorize(Roles = SD.Role_Admin)]
    public class ColorController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ColorController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index(int? page)
        {
            int pageNumber = page ?? 1;
            int pageSize = 10;
            var colors = await _unitOfWork.Colors.GetAllAsync();
            var paginatedColors = colors.ToPagedList(pageNumber, pageSize);
            return View(paginatedColors);
        }

        //public async Task<IActionResult> Details(int id)
        //{
        //    var color = await _unitOfWork.Colors.FirstOrDefaultAsync(m => m.Id == id);
        //    if (color == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(color);
        //}

        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Priority")] Color color)
        {
            if (ModelState.IsValid)
            {
                color.Name = color.Name.Trim();
                var normalizedColorName = color.Name.ToLower(); // Chuyển đổi tên màu về chữ thường
                if (_unitOfWork.Colors.Any(e => e.Name.ToLower() == normalizedColorName))
                {
                    //ModelState.AddModelError("name", "This color has already existed!");
                    TempData[SD.Error] = "Màu sắc này đã tồn tại!";
                    return await Create();
                }
                
                await _unitOfWork.Colors.AddAsync(color);
                await _unitOfWork.SaveChangesAsync();
                TempData["SuccessMessage"] = "Màu sắc mới được thêm thành công";
                return RedirectToAction(nameof(Index));
            }

            return View(color);
        }

        // GET: Color/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var color = await _unitOfWork.Colors.FirstOrDefaultAsync(e => e.Id == id);
            if (color == null)
            {
                return NotFound();
            }

            return View(color);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Priority")] Color color)
        {
            var colorFromDb = await _unitOfWork.Colors.FirstOrDefaultAsync(e => e.Id == id);
            if (colorFromDb == null)
            {
                return NotFound();
            }

            // Chuyển tên màu sắc đang chỉnh sửa thành chữ thường
            var colorNameLower = color.Name.Trim().ToLower();

            // Tải tất cả các màu sắc vào bộ nhớ và thực hiện so sánh tại cấp ứng dụng
            var colors = await _unitOfWork.Colors.GetAllAsync(); // Giả sử có phương thức GetAllAsync() để lấy tất cả các màu
            var existingColor = colors
                .FirstOrDefault(c => c.Name.Trim().ToLower() == colorNameLower && c.Id != color.Id);

            //var existingColor = await _unitOfWork.Colors.FirstOrDefaultAsync(c => c.Name == color.Name && c.Id != color.Id);
            if (existingColor != null) 
            {
                ModelState.AddModelError("Name","Tên màu sắc đã tồn tại");
            }

            if (!ModelState.IsValid)
                return View(color);

            _unitOfWork.Colors.Update(color);
            await _unitOfWork.SaveChangesAsync();
            TempData["SuccessMessage"] = "Màu sắc được sửa thành công";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var color = await _unitOfWork.Colors.FirstOrDefaultAsync(m => m.Id == id);
            if (color == null)
            {
                return NotFound();
            }

            return View(color);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var color = await _unitOfWork.Colors.FirstOrDefaultAsync(e => e.Id == id);
            if (color == null)
            {
                return NotFound();
            }
            
            var shoeColors = await _unitOfWork.ShoeColors.GetAllAsync(e => e.ColorId == color.Id);
            if(shoeColors.Count == 0)
            {
                _unitOfWork.Colors.Remove(color);
                await _unitOfWork.SaveChangesAsync();
                TempData["SuccessMessage"] = "Màu sắc được xóa thành công";
            }
            else
            {
                TempData["ErrorMessage"] = "Một số mẫu màu sắc giày thuộc về Màu này. Không thể xóa nó!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}