using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DoAnTotNghiep.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        public AccountController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(AppUser user)
        {
            if (ModelState.IsValid)
            {
                var checkExistUsername = await _context.AppUsers.AnyAsync(u => u.Username == user.Username);
                if (checkExistUsername)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã có người sử dụng!");
                    return View(user);
                }
                var checkExistEmail = await _context.AppUsers.AnyAsync(u => u.Email == user.Email);
                if (checkExistEmail)
                {
                    ModelState.AddModelError("Email", "Email này đã được đăng ký cho tài khoản khác!");
                    return View(user);
                }
                user.Role = "Customer";
                _context.AppUsers.Add(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction(nameof(Login));
            }
            return View(user);
        }
        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin") || User.IsInRole("Employee"))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string returnUrl = "/")
        {
            ViewBag.Username = username;
            ViewBag.ReturnUrl = returnUrl;
            bool hasError = false;
            if (string.IsNullOrEmpty(username))
            {
                ViewBag.UsernameError = "Vui lòng nhập tên đăng nhập!";
                hasError = true;
            }
            if (string.IsNullOrEmpty(password))
            {
                ViewBag.PasswordError = "Vui lòng nhập mật khẩu!";
                hasError = true;
            }
            if (hasError)
            {
                return View();
            }
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

            if (user != null && user.IsActive == true)
            {
                string userRole = !string.IsNullOrEmpty(user.Role) ? user.Role.Trim() : "Customer";
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, userRole)
        };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                if (userRole == "Admin")
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }
                else if (userRole == "Employee")
                {
                    return RedirectToAction("Index", "Category", new { area = "Admin" });
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/")
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }
            ViewBag.Error = "Tài khoản hoặc mật khẩu không chính xác!";
            return View();
        }
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        [HttpGet]
        [Authorize] 
        public async Task<IActionResult> Profile()
        {
            var username = User.Identity.Name;
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();
            return View(user);
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var username = User.Identity.Name;
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();
            return View(user); 
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(AppUser model)
        {
            var username = User.Identity.Name;
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");
            ModelState.Remove("Username");
            if (ModelState.IsValid)
            {
                var checkEmail = await _context.AppUsers.AnyAsync(u => u.Email == model.Email && u.Username != username);
                if (checkEmail)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác!");
                    return View(model);
                }
                user.FullName = model.FullName;
                user.Phone = model.Phone;
                user.Email = model.Email;
                user.DateOfBirth = model.DateOfBirth;
                _context.AppUsers.Update(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật thông tin thành công!";
                return RedirectToAction(nameof(Profile));
            }
            return View(model);
        }
        [HttpGet]
        [Authorize] 
        public IActionResult ChangePassword()
        {
            return View();
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var username = User.Identity.Name;
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound();
            }
            if (user.Password != model.OldPassword)
            {
                ModelState.AddModelError("OldPassword", "Mật khẩu hiện tại không chính xác!");
                return View(model);
            }
            user.Password = model.NewPassword;
            _context.Update(user);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Vui lòng ghi nhớ mật khẩu mới.";
            return RedirectToAction("Index", "Home");
        }
    }
}
