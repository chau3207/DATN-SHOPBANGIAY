using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoeStore.DataAccess.Repository.IRepository;
using ShoeStore.Models;
using ShoeStore.Ultitity;
using X.PagedList;

namespace ShoeStore.Controllers
{
    [Authorize(Roles = SD.Role_Admin)]
    public class SizeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public SizeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index(int? page)
        {
            int pageNumber = page ?? 1;
            int pageSize = 10;
            var sizes = await _unitOfWork.Sizes.GetAllAsync();
            var paginatedSizes = sizes.ToPagedList(pageNumber, pageSize);
            return View(paginatedSizes);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Unit,Value")] Size size)
        {
            if (ModelState.IsValid)
            {
                size.Unit = size.Unit.Trim();
                Size? sizeFromDb = await _unitOfWork.Sizes.FirstOrDefaultAsync(e => e.Unit == size.Unit && Math.Abs(e.Value - size.Value) < 0.001);
                if (sizeFromDb != null)
                {
                    TempData[SD.Error] = "Kích thước đã tồn tại!";
                }
                else
                {
                    await _unitOfWork.Sizes.AddAsync(size);
                    await _unitOfWork.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Kích thước được thêm mới thành công";
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(size);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var size = await _unitOfWork.Sizes.FirstOrDefaultAsync(e => e.Id == id);
            if (size == null)
            {
                return NotFound();
            }

            return View(size);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Unit,Value,SortOrder")] Size size)
        {
            var sizeFromDb = await _unitOfWork.Sizes.FirstOrDefaultAsync(e => e.Id == id);
            if (id != size.Id || sizeFromDb == null)
            {
                return NotFound();
            }

            var existingSize = await _unitOfWork.Sizes.FirstOrDefaultAsync(s => s.Value == size.Value && s.Unit == size.Unit && s.Id != size.Id);
            if (existingSize != null)
            {
                ModelState.AddModelError("Value", "Giá trị này đã tồn tại");
            }

            if (ModelState.IsValid)
            {
                _unitOfWork.Sizes.Update(size);
                await _unitOfWork.SaveChangesAsync();
                TempData["SuccessMessage"] = "Kích thước được sửa thành công";
                return RedirectToAction(nameof(Index));
            }

            return View(size);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var size = await _unitOfWork.Sizes.FirstOrDefaultAsync(m => m.Id == id);
            if (size == null)
            {
                return NotFound();
            }

            return View(size);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var size = await _unitOfWork.Sizes.FirstOrDefaultAsync(e => e.Id == id);
            if (size == null)
            {
                return NotFound();
            }

            var shoeSizes = await _unitOfWork.ShoeSizes.GetAllAsync(e => e.SizeId == size.Id);
            if (shoeSizes.Count == 0)
            {
                _unitOfWork.Sizes.Remove(size);
                await _unitOfWork.SaveChangesAsync();
                TempData["SuccessMessage"] = "Kích thước được xóa thành công";
            }
            else
            {
                TempData["ErrorMessage"] = "Một số Giày thuộc về Kích thước này. Không thể xóa nó!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}