using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController : Controller
    {
        private readonly AppDbContext _context;
        public UserController(AppDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> StaffIndex()
        {
            var staffs = await _context.AppUsers
                                       .Where(u => u.Role == "Admin" || u.Role == "Employee")
                                       .ToListAsync();
            return View(staffs);
        }
        [Authorize(Roles = "Admin,Employee")] 
        public async Task<IActionResult> CustomerIndex()
        {
            var customers = await _context.AppUsers
                                          .Where(u => u.Role == "Customer")
                                          .ToListAsync();
            return View(customers);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppUser user)
        {
            if (ModelState.IsValid)
            {
                var checkExist = await _context.AppUsers
                    .FirstOrDefaultAsync(u => u.Username == user.Username || u.Email == user.Email);

                if (checkExist != null)
                {
                    if (checkExist.Username == user.Username)
                        ModelState.AddModelError("Username", "Tên đăng nhập này đã có người sử dụng!");
                    if (checkExist.Email == user.Email)
                        ModelState.AddModelError("Email", "Email này đã được đăng ký!");

                    return View(user);
                }

                _context.Add(user);
                await _context.SaveChangesAsync();
                if (user.Role == "Admin" || user.Role == "Employee")
                {
                    return RedirectToAction(nameof(StaffIndex));
                }
                return RedirectToAction(nameof(CustomerIndex));
            }
            return View(user);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null) return NotFound();
            user.ConfirmPassword = user.Password;
            return View(user);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AppUser user)
        {
            if (id != user.UserId) return NotFound();
            if (string.IsNullOrEmpty(user.Password))
            {
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
            }
            else
            {
                if (string.IsNullOrEmpty(user.ConfirmPassword))
                {
                    ModelState.AddModelError("ConfirmPassword", "Vui lòng nhập ô Xác nhận mật khẩu!");
                }
                else if (user.Password != user.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp!");
                }
            }
            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.AppUsers.FindAsync(id);
                    if (existingUser == null) return NotFound();
                    var checkEmail = await _context.AppUsers
                        .FirstOrDefaultAsync(u => u.Email == user.Email && u.UserId != id);
                    if (checkEmail != null)
                    {
                        ModelState.AddModelError("Email", "Email này đã bị trùng với tài khoản khác!");
                        return View(user);
                    }
                    existingUser.FullName = user.FullName;
                    existingUser.Email = user.Email;
                    existingUser.Phone = user.Phone;
                    existingUser.DateOfBirth = user.DateOfBirth;
                    existingUser.Role = user.Role;
                    if (!string.IsNullOrEmpty(user.Password))
                    {
                        existingUser.Password = user.Password;
                    }
                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.AppUsers.Any(e => e.UserId == user.UserId)) return NotFound();
                    else throw;
                }
                if (user.Role == "Admin" || user.Role == "Employee")
                    return RedirectToAction(nameof(StaffIndex));

                return RedirectToAction(nameof(CustomerIndex));
            }
            return View(user);
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null) return NotFound();
            if (User.Identity.Name == user.Username)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản của chính mình!";
                return RedirectToAction(user.Role == "Customer" ? nameof(CustomerIndex) : nameof(StaffIndex));
            }
            user.IsActive = !user.IsActive;
            _context.Update(user);
            await _context.SaveChangesAsync();
            if (user.IsActive)
            {
                TempData["SuccessMessage"] = $"Đã mở khóa tài khoản: {user.FullName}";
            }
            else
            {
                TempData["ErrorMessage"] = $"Đã khóa tài khoản: {user.FullName}";
            }
            return RedirectToAction(user.Role == "Customer" ? nameof(CustomerIndex) : nameof(StaffIndex));
        }
    }
}
