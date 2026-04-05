using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Controllers
{

    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string? searchString, int? categoryId, int page = 1)
        {
            var products = _context.Products.Include(p => p.Category).AsQueryable();

            if (categoryId != null)
            {
                products = products.Where(p => p.CategoryId == categoryId);
                var category = await _context.Categories.FindAsync(categoryId);
                if (category != null)
                {
                    ViewBag.CategoryName = category.CategoryName;
                }
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.ProductName.Contains(searchString));
                ViewBag.SearchKeyword = searchString;
            }
            int pageSize = 12; 
            int totalItems = await products.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;
            var result = await products.OrderByDescending(p => p.CreatedDate)
                                       .Skip((page - 1) * pageSize)
                                       .Take(pageSize)
                                       .ToListAsync();
            var now = DateTime.Now;
            var activePromotions = await _context.PromotionProducts
                .Include(pp => pp.Promotion)
                .Where(pp => pp.Promotion.IsActive == true
                          && pp.Promotion.StartDate <= now
                          && pp.Promotion.EndDate >= now)
                .ToListAsync();

            var promoDict = activePromotions
                .GroupBy(pp => pp.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Max(pp => pp.Promotion.DiscountPercent)
                );
            ViewBag.PromoDict = promoDict;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.CategoryId = categoryId; 
            ViewBag.TotalItems = totalItems;

            return View(result);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();
            var now = DateTime.Now;
            var activePromotion = await _context.PromotionProducts
                .Include(pp => pp.Promotion)
                .Where(pp => pp.ProductId == id
                          && pp.Promotion.IsActive == true
                          && pp.Promotion.StartDate <= now
                          && pp.Promotion.EndDate >= now)
                .OrderByDescending(pp => pp.Promotion.DiscountPercent) 
                .Select(pp => pp.Promotion) 
                .FirstOrDefaultAsync();

            ViewBag.ActivePromotion = activePromotion;
            return View(product);
        }
        [HttpPost]
        public async Task<IActionResult> SubmitReview(int productId, int rating, string comment)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để đánh giá sản phẩm!";
                return RedirectToAction("Details", new { id = productId });
            }
            if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(comment))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn số sao và nhập nội dung đánh giá!";
                return RedirectToAction("Details", new { id = productId });
            }
            var review = new Review
            {
                ProductId = productId,
                Rating = rating,
                Comment = comment.Trim(),
                UserName = User.Identity.Name, 
                CreatedDate = DateTime.Now,
                IsApproved = true
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
            return RedirectToAction("Details", new { id = productId });
        }
    }
}
