using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")] 
    public class ContactController : Controller
    {
        private readonly AppDbContext _context;
        public ContactController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string searchString, int? status, int page = 1)
        {
            var query = _context.Contacts.AsQueryable();
            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(c => (c.FullName != null && c.FullName.Contains(searchString))
                                      || (c.Email != null && c.Email.Contains(searchString))
                                      || (c.Phone != null && c.Phone.Contains(searchString))
                                      || (c.Message != null && c.Message.Contains(searchString)));
            }
            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;
            var contacts = await query.OrderBy(c => c.Status)
                                      .ThenByDescending(c => c.CreatedDate)
                                      .Skip((page - 1) * pageSize)
                                      .Take(pageSize)
                                      .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.Status = status; 
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(contacts);
        }
        [HttpGet]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact != null)
            {
                contact.Status = 1; 
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã đánh dấu xử lý tin nhắn thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa tin nhắn thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
