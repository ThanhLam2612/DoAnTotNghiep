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
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, int? selectedYear)
        {
            // =======================================================
            // 1. XỬ LÝ DỮ LIỆU ĐẦU VÀO & GIỮ TRẠNG THÁI LỌC
            // =======================================================
            DateTime start = startDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime end = endDate ?? DateTime.Now.Date.AddDays(1).AddTicks(-1);
            int yearForMonthChart = selectedYear ?? DateTime.Now.Year;

            ViewBag.StartDate = start.ToString("yyyy-MM-dd");
            ViewBag.EndDate = end.ToString("yyyy-MM-dd");
            ViewBag.SelectedYear = yearForMonthChart;

            // Lấy danh sách các năm có phát sinh đơn hàng để đổ vào Combobox
            var availableYears = await _context.Orders.Select(o => o.OrderDate.Year).Distinct().OrderByDescending(y => y).ToListAsync();
            if (!availableYears.Contains(DateTime.Now.Year)) availableYears.Insert(0, DateTime.Now.Year);
            ViewBag.AvailableYears = availableYears;

            // =======================================================
            // 2. TRUY VẤN DỮ LIỆU CHUNG & BIỂU ĐỒ NGÀY
            // =======================================================
            var orderQuery = _context.Orders.Where(o => o.OrderDate >= start && o.OrderDate <= end);

            // ĐÃ SỬA: Lấy tổng toàn bộ cửa hàng (Dùng _context.Orders thay vì orderQuery)
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalRevenue = await _context.Orders.Where(o => o.Status == 2).SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.PendingOrders = await orderQuery.CountAsync(o => o.Status == 0);
            ViewBag.SuccessOrders = await orderQuery.CountAsync(o => o.Status == 2);
            ViewBag.CanceledOrders = await orderQuery.CountAsync(o => o.Status == 3);

            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalCustomers = await _context.AppUsers.CountAsync(u => u.Role == "Customer");

            var dailyRevenue = await orderQuery
                .Where(o => o.Status == 2)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalAmount) })
                .OrderBy(x => x.Date)
                .ToListAsync();

            ViewBag.ChartLabels = string.Join(",", dailyRevenue.Select(x => x.Date.ToString("dd/MM/yyyy")));
            ViewBag.ChartData = string.Join(",", dailyRevenue.Select(x => x.Total));

            var topSellingProducts = await _context.OrderDetails
                .Include(od => od.Order).Include(od => od.Product)
                .Where(od => od.Order.Status == 2 && od.Order.OrderDate >= start && od.Order.OrderDate <= end)
                .GroupBy(od => new { od.ProductId, od.Product.ProductName, od.Product.ThumbnailUrl })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    ImageUrl = g.Key.ThumbnailUrl,
                    TotalSold = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.Quantity * od.Price)
                })
                .OrderByDescending(x => x.TotalSold).Take(5).ToListAsync();
            ViewBag.TopSellingProducts = topSellingProducts;

            // =======================================================
            // 3. BIỂU ĐỒ DOANH THU THEO THÁNG (Theo Năm được chọn)
            // =======================================================
            var monthlyRevenue = await _context.Orders
                .Where(o => o.Status == 2 && o.OrderDate.Year == yearForMonthChart)
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(o => o.TotalAmount) })
                .OrderBy(x => x.Month)
                .ToListAsync();

            ViewBag.MonthlyLabels = string.Join(",", monthlyRevenue.Select(x => "Tháng " + x.Month));
            ViewBag.MonthlyData = string.Join(",", monthlyRevenue.Select(x => x.Total));

            return View();
        }

    }
}
