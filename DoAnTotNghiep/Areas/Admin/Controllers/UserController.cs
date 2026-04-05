using Azure;
using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
        public async Task<IActionResult> StaffIndex(string searchString, string role, bool? isActive, int page=1)
        {
            var query = _context.AppUsers.Where(u => u.Role == "Admin" || u.Role == "Employee").AsQueryable();
            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role == role);
            }
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(u => u.Username.Contains(searchString)
                                      || (u.FullName != null && u.FullName.Contains(searchString))
                                      || (u.Email != null && u.Email.Contains(searchString))
                                      || (u.Phone != null && u.Phone.Contains(searchString)));
            }
            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;
            var staffs = await query.OrderByDescending(u => u.UserId)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();
            ViewBag.SearchString = searchString;
            ViewBag.Role = role;
            ViewBag.IsActive = isActive;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View(staffs);
        }
        [Authorize(Roles = "Admin,Employee")] 
        public async Task<IActionResult> CustomerIndex(string searchString, bool? isActive, int page = 1)
        {
            var query = _context.AppUsers.Where(u => u.Role == "Customer").AsQueryable();
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(u => u.Username.Contains(searchString)
                                      || (u.FullName != null && u.FullName.Contains(searchString))
                                      || (u.Email != null && u.Email.Contains(searchString))
                                      || (u.Phone != null && u.Phone.Contains(searchString)));
            }
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(u => u.Username.Contains(searchString)
                                      || (u.FullName != null && u.FullName.Contains(searchString))
                                      || (u.Email != null && u.Email.Contains(searchString))
                                      || (u.Phone != null && u.Phone.Contains(searchString)));
            }
            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;
            var customers = await query.OrderByDescending(u => u.UserId)
                                         .Skip((page - 1) * pageSize)
                                         .Take(pageSize)
                                         .ToListAsync();
            ViewBag.SearchString = searchString;
            ViewBag.IsActive = isActive;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
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
