using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DoAnTotNghiep.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.NewProducts = await _context.Products
                                                .OrderByDescending(p => p.ProductId)
                                                .Take(8)
                                                .ToListAsync();
            ViewBag.BestSellers = await _context.Products
                                                .OrderBy(p => p.BasePrice)
                                                .Take(8)
                                                .ToListAsync();
            ViewBag.RecentNews = await _context.News
                                               .OrderByDescending(n => n.NewsId) 
                                               .Take(8)
                                               .ToListAsync();
            var now = DateTime.Now;
            var activePromotions = await _context.PromotionProducts
                .Include(pp => pp.Product)     
                .Include(pp => pp.Promotion)   
                .Where(pp => pp.Promotion.IsActive == true
                          && pp.Promotion.StartDate <= now
                          && pp.Promotion.EndDate >= now) 
                .OrderByDescending(pp => pp.Promotion.DiscountPercent)
                .ToListAsync();
            ViewBag.SaleProducts = activePromotions;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
