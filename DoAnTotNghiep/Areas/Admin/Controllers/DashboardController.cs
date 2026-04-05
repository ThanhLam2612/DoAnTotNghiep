using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DoAnTotNghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        public DashboardController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalCustomers = await _context.AppUsers.CountAsync(u => u.Role == "Customer");
            ViewBag.TotalRevenue = await _context.Orders
                                                 .Where(o => o.Status == 2) 
                                                 .SumAsync(o => o.TotalAmount);
            var currentYear = DateTime.Now.Year;
            ViewBag.CurrentYear = currentYear;
            var monthlyRevenue = await _context.Orders
                .Where(o => o.OrderDate.Year == currentYear && o.Status == 2)
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(o => o.TotalAmount) })
                .ToListAsync();

            decimal[] revenueData = new decimal[12];
            foreach (var item in monthlyRevenue)
            {
                revenueData[item.Month - 1] = item.Total;
            }
            ViewBag.RevenueData = string.Join(",", revenueData);
            var topSellingProducts = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                .Where(od => od.Order.Status == 2) 
                .GroupBy(od => new { od.ProductId, od.Product.ProductName, od.Product.ThumbnailUrl })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    ImageUrl = g.Key.ThumbnailUrl,
                    TotalSold = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.Quantity * od.Price)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToListAsync();

            ViewBag.TopSellingProducts = topSellingProducts;
            return View();
        }
    }
}
