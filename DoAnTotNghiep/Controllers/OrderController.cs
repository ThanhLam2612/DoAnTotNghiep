using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        public OrderController(AppDbContext context) { _context = context; }
        public async Task<IActionResult> Index()
        {
            var username = User.Identity.Name;
            var orders = await _context.Orders
                                       .Where(o => o.Username == username)
                                       .OrderByDescending(o => o.OrderDate)
                                       .ToListAsync();
            return View(orders);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var username = User.Identity.Name;
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.Username == username);
            if (order == null) return NotFound(); 
            return View(order);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var username = User.Identity.Name;
            var order = await _context.Orders
                                      .FirstOrDefaultAsync(o => o.OrderId == id && o.Username == username);
            if (order == null) return NotFound();
            if (order.Status == 0)
            {
                order.Status = 3; 
                await _context.SaveChangesAsync();
                TempData["Success"] = "Hủy đơn hàng thành công!";
            }
            else
            {
                TempData["Error"] = "Không thể hủy! Đơn hàng này đang được giao hoặc đã hoàn thành.";
            }
            return RedirectToAction(nameof(Details), new { id = order.OrderId });
        }
    }
}
