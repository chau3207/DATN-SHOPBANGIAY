using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShoeStore.Ultitity;

namespace ShoeStore.Controllers
{
    [Authorize(Roles = SD.Role_Admin)]
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _manager;

        public RolesController(RoleManager<IdentityRole> roleManager)
        {
            _manager = roleManager;
        }
        public IActionResult Index()
        {
            var roles = _manager.Roles;
            return View(roles);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(IdentityRole role) 
        {
            if (string.IsNullOrWhiteSpace(role.Name))
            {
                ModelState.AddModelError("", "Yêu cầu nhập tên vai trò.");
                return View(role);
            }

            if (!_manager.RoleExistsAsync(role.Name).GetAwaiter().GetResult()) 
            {
                _manager.CreateAsync(new IdentityRole(role.Name)).GetAwaiter().GetResult();
            }
            else
            {
                ModelState.AddModelError("", "Vai trò đã tồn tại");
                return View(role);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var role = await _manager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }
            return View(role);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(IdentityRole role)
        {
            var existingRole = await _manager.FindByIdAsync(role.Id);
            if (existingRole == null)
            {
                return NotFound();
            }

            existingRole.Name = role.Name;
            await _manager.UpdateAsync(existingRole);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _manager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }
            return View(role);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var role = await _manager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            await _manager.DeleteAsync(role);
            return RedirectToAction("Index");
        }
    }
}
