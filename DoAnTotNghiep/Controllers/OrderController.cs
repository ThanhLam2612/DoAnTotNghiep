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

            // Lấy tên user đang đăng nhập để bảo mật, tránh việc khách này xem trộm đơn khách khác
            var username = User.Identity.Name;

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(v => v.AttributeValues)
                            // LOGIC MỚI: Bắt buộc phải đi qua Kho từ điển (PredefinedAttributeValue)
                            .ThenInclude(av => av.PredefinedAttributeValue)
                                .ThenInclude(p => p.ProductAttribute)
                .FirstOrDefaultAsync(m => m.OrderId == id && m.Username == username); // Đảm bảo đơn hàng này của đúng user đang đăng nhập

            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails) // Bắt buộc phải có dòng này để kéo chi tiết lên
                .FirstOrDefaultAsync(o => o.OrderId == id && o.Username == User.Identity.Name);

            if (order == null) return NotFound();

            // Chỉ cho phép hủy khi đơn hàng đang ở trạng thái Chờ duyệt (0)
            if (order.Status == 0)
            {
                order.Status = 3; // 3 là trạng thái Đã Hủy
                _context.Update(order);

                // ==========================================
                // VÒNG LẶP HOÀN TRẢ LẠI KHO 
                // ==========================================
                // Kiểm tra order.OrderDetails khác null trước khi lặp để chống crash
                if (order.OrderDetails != null && order.OrderDetails.Any())
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        if (detail.VariantId.HasValue)
                        {
                            var variant = await _context.ProductVariants.FindAsync(detail.VariantId.Value);
                            if (variant != null)
                            {
                                variant.StockQuantity += detail.Quantity; // Cộng trả lại số lượng vào kho
                                _context.Update(variant);
                            }
                        }
                    }
                }
                // ==========================================

                await _context.SaveChangesAsync();
                TempData["Success"] = "Hủy đơn hàng thành công! Số lượng sản phẩm đã được hoàn lại kho.";
            }
            else
            {
                TempData["Error"] = "Không thể hủy đơn hàng này vì đang trong quá trình giao hoặc đã hoàn thành.";
            }

            return RedirectToAction(nameof(Details), new { id = order.OrderId });
        }
    }
}