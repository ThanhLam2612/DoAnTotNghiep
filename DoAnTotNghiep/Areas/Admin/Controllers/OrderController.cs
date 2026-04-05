using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        public OrderController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string searchString, int? status, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            var query = _context.Orders.AsQueryable();
            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }
            if (fromDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.OrderDate <= endOfDay);
            }
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(o => o.OrderId.ToString().Contains(searchString)
                                      || (o.CustomerName != null && o.CustomerName.Contains(searchString))
                                      || (o.Phone != null && o.Phone.Contains(searchString)));
            }
            int pageSize = 10;
            int totalItems = await query.CountAsync(); 
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize); 
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;
            var orders = await query.OrderByDescending(o => o.OrderDate)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();
            ViewBag.SearchString = searchString;
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View(orders);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product) 
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null) return NotFound();

            return View(order);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, int status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật trạng thái thành công!";
            }
            return RedirectToAction(nameof(Details), new { id = orderId });
        }
    }
}
