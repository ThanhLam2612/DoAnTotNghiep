using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] 
    public class UserController : Controller
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string searchString, string role, bool? isActive, int page = 1)
        {
            var query = _context.AppUsers.AsQueryable();
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
            int pageSize = 5; 
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var users = await query.OrderByDescending(u => u.UserId)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();
            ViewBag.SearchString = searchString;
            ViewBag.Role = role;
            ViewBag.IsActive = isActive;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(users);
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

                TempData["SuccessMessage"] = $"Đã thêm tài khoản {user.Username} thành công!";
                return RedirectToAction(nameof(Index));
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

            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");

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

                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Đã cập nhật thông tin tài khoản {existingUser.Username} thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.AppUsers.Any(e => e.UserId == user.UserId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
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
                return RedirectToAction(nameof(Index));
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
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user != null)
            {
                user.Password = "123456";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã đặt lại mật khẩu cho tài khoản {user.Username} thành 123456 thành công!";
            }
            return RedirectToAction("Edit", new { id = id });
        }
    }
}