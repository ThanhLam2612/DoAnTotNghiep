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
            // 1. LẤY SẢN PHẨM MỚI (Đã thêm ThenInclude để lấy thuộc tính cấu hình)
            ViewBag.NewProducts = await _context.Products
                                .Include(p => p.Reviews)
                                .Include(p => p.ProductVariants)
                                    .ThenInclude(v => v.AttributeValues)
                                        .ThenInclude(av => av.PredefinedAttributeValue)
                                            .ThenInclude(pa => pa.ProductAttribute)
                                .OrderByDescending(p => p.CreatedDate)
                                .Take(8)
                                .ToListAsync();

            // 2. LẤY SẢN PHẨM BÁN CHẠY (Đã thêm ThenInclude để lấy thuộc tính cấu hình)
            ViewBag.BestSellers = await _context.OrderDetails
                                    .Include(od => od.Order)
                                    .Include(od => od.Product)
                                          .ThenInclude(p => p.Reviews)
                                    .Include(od => od.Product)
                                          .ThenInclude(p => p.ProductVariants)
                                              .ThenInclude(v => v.AttributeValues)
                                                  .ThenInclude(av => av.PredefinedAttributeValue)
                                                      .ThenInclude(pa => pa.ProductAttribute)
                                    .Where(od => od.Order.Status == 2)
                                    .GroupBy(od => od.Product)
                                    .Select(g => new
                                    {
                                        ProductItem = g.Key,
                                        TotalSold = g.Sum(od => od.Quantity)
                                    })
                                    .OrderByDescending(x => x.TotalSold)
                                    .Take(8)
                                    .Select(x => x.ProductItem)
                                    .ToListAsync();

            // 3. LẤY TIN TỨC MỚI
            ViewBag.RecentNews = await _context.News
                                       .OrderByDescending(n => n.NewsId)
                                       .Take(8)
                                       .ToListAsync();

            // ====================================================================
            // 4. LẤY SẢN PHẨM KHUYẾN MÃI (LOGIC MỚI SỬ DỤNG PROMOTION_VARIANT)
            // ====================================================================
            var now = DateTime.Now;
            var activePromotions = await _context.PromotionVariants
                .Include(pv => pv.ProductVariant)
                .Include(pv => pv.Promotion)
                .Where(pv => pv.Promotion.IsActive == true
                          && pv.Promotion.StartDate <= now
                          && pv.Promotion.EndDate >= now)
                .ToListAsync();

            // SỬA Ở ĐÂY: Gom nhóm theo BIẾN THỂ (VariantId) thay vì ProductId
            var promoDict = activePromotions
                .GroupBy(pv => pv.VariantId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Max(pv => pv.DiscountPercent)
                );

            ViewBag.PromoDict = promoDict;

            // Lấy danh sách ProductId để truyền ra View
            var saleProductIds = activePromotions.Select(pv => pv.ProductVariant.ProductId).Distinct().ToList();

            ViewBag.SaleProducts = await _context.Products
                .Include(p => p.Reviews)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.AttributeValues)
                        .ThenInclude(av => av.PredefinedAttributeValue)
                            .ThenInclude(pa => pa.ProductAttribute)
                .Where(p => saleProductIds.Contains(p.ProductId))
                .ToListAsync();
            // ====================================================================

            // 5. LẤY DANH SÁCH SẢN PHẨM ĐÃ YÊU THÍCH CỦA USER ĐANG ĐĂNG NHẬP
            List<int> favoritedProductIds = new List<int>();
            if (User.Identity.IsAuthenticated)
            {
                var currentUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
                if (currentUser != null)
                {
                    favoritedProductIds = await _context.Favorites
                        .Where(f => f.UserId == currentUser.UserId)
                        .Select(f => f.ProductId)
                        .ToListAsync();
                }
            }
            ViewBag.FavoritedIds = favoritedProductIds;

            // 6. THÊM MỚI: LẤY DANH SÁCH SLIDER ĐỂ HIỂN THỊ RA TRANG CHỦ
            var sliders = await _context.Sliders
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            return View(sliders);
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