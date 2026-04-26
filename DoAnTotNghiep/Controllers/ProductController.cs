using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
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

        // BỔ SUNG: Thêm int? categoryId và int? brandId vào tham số truyền vào
        public async Task<IActionResult> Index(string? searchString, decimal? minPrice, decimal? maxPrice, string? sortOrder, int? categoryId, int? brandId, int page = 1)
        {
            var now = DateTime.Now;

            var query = _context.Products
                                .Include(p => p.Reviews)
                                .Include(p => p.Category)
                                .Include(p => p.Brand)
                                // BỔ SUNG INCLUDE THUỘC TÍNH ĐỂ VIEW CHIA CẤU HÌNH ĐƯỢC
                                .Include(p => p.ProductVariants)
                                    .ThenInclude(v => v.AttributeValues)
                                        .ThenInclude(av => av.PredefinedAttributeValue)
                                            .ThenInclude(pa => pa.ProductAttribute)
                                // INCLUDE TIẾP PROMOTION
                                .Include(p => p.ProductVariants)
                                    .ThenInclude(v => v.PromotionVariants)
                                        .ThenInclude(pv => pv.Promotion)
                                .AsQueryable();

            // 1. TÌM KIẾM THEO TÊN
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(p => p.ProductName.Contains(searchString));
                ViewBag.SearchKeyword = searchString;
            }

            // ========================================================
            // 2. LOGIC MỚI: LỌC THEO DANH MỤC VÀ THƯƠNG HIỆU TỪ MENU
            // ========================================================
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            var productList = await query.ToListAsync();

            // 3. LỌC GIÁ KHI KHÁCH HÀNG KÉO THANH TRƯỢT
            if (minPrice.HasValue && minPrice.Value > 0)
            {
                productList = productList.Where(p => p.ProductVariants.Any() &&
                    p.ProductVariants.Min(v => v.Price * (1 - (v.PromotionVariants
                        .Where(pv => pv.Promotion.IsActive == true && pv.Promotion.StartDate <= now && pv.Promotion.EndDate >= now)
                        .Select(pv => pv.DiscountPercent)
                        .DefaultIfEmpty(0).Max()) / 100m)) >= minPrice.Value).ToList();
            }
            if (maxPrice.HasValue && maxPrice.Value < 100000000)
            {
                productList = productList.Where(p => p.ProductVariants.Any() &&
                    p.ProductVariants.Min(v => v.Price * (1 - (v.PromotionVariants
                        .Where(pv => pv.Promotion.IsActive == true && pv.Promotion.StartDate <= now && pv.Promotion.EndDate >= now)
                        .Select(pv => pv.DiscountPercent)
                        .DefaultIfEmpty(0).Max()) / 100m)) <= maxPrice.Value).ToList();
            }

            // 4. SẮP XẾP
            switch (sortOrder)
            {
                case "price_asc":
                    productList = productList.OrderBy(p => p.ProductVariants.Any() ?
                        p.ProductVariants.Min(v => v.Price * (1 - (v.PromotionVariants
                            .Where(pv => pv.Promotion.IsActive == true && pv.Promotion.StartDate <= now && pv.Promotion.EndDate >= now)
                            .Select(pv => pv.DiscountPercent)
                            .DefaultIfEmpty(0).Max()) / 100m)) : 0).ToList();
                    break;
                case "price_desc":
                    productList = productList.OrderByDescending(p => p.ProductVariants.Any() ?
                        p.ProductVariants.Min(v => v.Price * (1 - (v.PromotionVariants
                            .Where(pv => pv.Promotion.IsActive == true && pv.Promotion.StartDate <= now && pv.Promotion.EndDate >= now)
                            .Select(pv => pv.DiscountPercent)
                            .DefaultIfEmpty(0).Max()) / 100m)) : 0).ToList();
                    break;
                case "name_asc":
                    productList = productList.OrderBy(p => p.ProductName).ToList();
                    break;
                default:
                    productList = productList.OrderByDescending(p => p.CreatedDate).ToList();
                    break;
            }

            // 5. PHÂN TRANG
            int pageSize = 12;
            int totalItems = productList.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var result = productList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // 6. LẤY KHUYẾN MÃI
            var displayedProductIds = result.Select(r => r.ProductId).ToList();
            var activePromotions = await _context.PromotionVariants
                .Include(pv => pv.Promotion)
                .Include(pv => pv.ProductVariant)
                .Where(pv => displayedProductIds.Contains(pv.ProductVariant.ProductId)
                          && pv.Promotion.IsActive == true
                          && pv.Promotion.StartDate <= now
                          && pv.Promotion.EndDate >= now)
                .ToListAsync();

            // ĐÃ SỬA: Gom nhóm theo VariantId để lấy đúng % giảm giá của từng cấu hình
            var promoDict = activePromotions
                .GroupBy(pv => pv.VariantId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Max(pv => pv.DiscountPercent)
                );

            ViewBag.PromoDict = promoDict;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortOrder = sortOrder;

            // BỔ SUNG: Truyền ID danh mục và thương hiệu ra View để không bị mất khi phân trang
            ViewBag.CategoryId = categoryId;
            ViewBag.BrandId = brandId;

            // 7. LẤY SẢN PHẨM YÊU THÍCH
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

            return View(result);
        }

        public async Task<IActionResult> Details(int? id, int? variantId)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.ReviewLikes)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.AttributeValues)
                        .ThenInclude(av => av.PredefinedAttributeValue)
                            .ThenInclude(p => p.ProductAttribute)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();
            ViewBag.SelectedVariantId = variantId;
            var now = DateTime.Now;

            var activePromotion = await _context.PromotionVariants
                .Include(pv => pv.Promotion)
                .Include(pv => pv.ProductVariant)
                .Where(pv => pv.ProductVariant.ProductId == id
                          && pv.Promotion.IsActive == true
                          && pv.Promotion.StartDate <= now
                          && pv.Promotion.EndDate >= now)
                .OrderByDescending(pv => pv.DiscountPercent)
                .Select(pv => pv.Promotion)
                .FirstOrDefaultAsync();

            ViewBag.ActivePromotion = activePromotion;

            var relatedProducts = await _context.Products
                .Include(p => p.ProductVariants)
                .Where(p => p.CategoryId == product.CategoryId && p.BrandId == product.BrandId && p.ProductId != product.ProductId)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            var relatedProductIds = relatedProducts.Select(p => p.ProductId).ToList();

            var relatedPromotions = await _context.PromotionVariants
                .Include(pv => pv.Promotion)
                .Include(pv => pv.ProductVariant)
                .Where(pv => relatedProductIds.Contains(pv.ProductVariant.ProductId)
                          && pv.Promotion.IsActive == true
                          && pv.Promotion.StartDate <= now
                          && pv.Promotion.EndDate >= now)
                .ToListAsync();

            var promoDict = relatedPromotions
                .GroupBy(pv => pv.ProductVariant.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Max(pv => pv.DiscountPercent)
                );

            ViewBag.PromoDict = promoDict;
            List<int> favoritedIds = new List<int>();
            if (User.Identity.IsAuthenticated)
            {
                var currentUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
                if (currentUser != null)
                {
                    // Lưu ý: Đổi _context.Favorites thành đúng tên bảng Yêu thích của bạn (nếu khác)
                    favoritedIds = await _context.Favorites
                        .Where(f => f.UserId == currentUser.UserId)
                        .Select(f => f.ProductId)
                        .ToListAsync();
                }
            }
            ViewBag.FavoritedIds = favoritedIds;
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReview(int productId, int rating, string comment, int? variantId)
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
                VariantId = variantId,
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

        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int productId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "require_login" });
            }

            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null) return Json(new { success = false });

            var existingFav = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == user.UserId && f.ProductId == productId);

            if (existingFav != null)
            {
                _context.Favorites.Remove(existingFav);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = false });
            }
            else
            {
                var newFav = new Favorite
                {
                    UserId = user.UserId,
                    ProductId = productId,
                    CreatedDate = DateTime.Now
                };
                _context.Favorites.Add(newFav);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = true });
            }
        }

        [Authorize]
        public async Task<IActionResult> Favorites()
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null) return RedirectToAction("Login", "Account");

            var favorites = await _context.Favorites
                .Include(f => f.Product)
                    .ThenInclude(p => p.Reviews)
                .Include(f => f.Product)
                    .ThenInclude(p => p.ProductVariants)
                .Where(f => f.UserId == user.UserId)
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();

            var favProductIds = favorites.Select(f => f.ProductId).ToList();
            var now = DateTime.Now;

            var activePromotions = await _context.PromotionVariants
                .Include(pv => pv.Promotion)
                .Include(pv => pv.ProductVariant)
                .Where(pv => favProductIds.Contains(pv.ProductVariant.ProductId)
                          && pv.Promotion.IsActive == true
                          && pv.Promotion.StartDate <= now
                          && pv.Promotion.EndDate >= now)
                .ToListAsync();

            var promoDict = activePromotions
                .GroupBy(pv => pv.ProductVariant.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Max(pv => pv.DiscountPercent)
                );

            ViewBag.PromoDict = promoDict;

            return View(favorites);
        }
        [HttpPost]
        public async Task<IActionResult> ToggleReviewLike(int reviewId)
        {
            // 1. Phải đăng nhập mới được Like
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "require_login" });
            }

            // Lấy thông tin user hiện tại (Dùng chung cho cả Identity hoặc Session tuỳ bạn thiết kế)
            var currentUserName = User.Identity.Name;
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == currentUserName);

            // Lấy ID người dùng, nếu không có AppUser thì tạm lấy UserName
            string userId = user != null ? user.UserId.ToString() : currentUserName;

            // 2. Kiểm tra xem người này đã Like đánh giá này chưa
            var existingLike = await _context.ReviewLikes
                .FirstOrDefaultAsync(l => l.ReviewId == reviewId && (l.UserId == userId || l.UserName == currentUserName));

            bool isLiked;

            if (existingLike != null)
            {
                // Nếu đã Like rồi -> Bấm lần nữa là Hủy Like (Unlike)
                _context.ReviewLikes.Remove(existingLike);
                isLiked = false;
            }
            else
            {
                // Nếu chưa Like -> Thêm mới
                var newLike = new ReviewLike
                {
                    ReviewId = reviewId,
                    UserId = userId,
                    UserName = currentUserName,
                    CreatedAt = DateTime.Now
                };
                _context.ReviewLikes.Add(newLike);
                isLiked = true;
            }

            // Lưu thay đổi vào DB
            await _context.SaveChangesAsync();

            // 3. Đếm lại tổng số Like mới nhất của đánh giá này
            int newCount = await _context.ReviewLikes.CountAsync(l => l.ReviewId == reviewId);

            // 4. Trả về cho Javascript để cập nhật giao diện mượt mà
            return Json(new
            {
                success = true,
                isLiked = isLiked,
                newCount = newCount
            });
        }
        [HttpPost]
        public async Task<IActionResult> EditReview(int reviewId, int rating, string comment)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thực hiện thao tác này!";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            var review = await _context.Reviews.FindAsync(reviewId);

            // Kiểm tra xem đánh giá có tồn tại và đúng là của User đang đăng nhập không
            if (review == null || review.UserName != User.Identity.Name)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá hoặc bạn không có quyền chỉnh sửa!";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(comment))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn số sao và nhập nội dung đánh giá!";
                return RedirectToAction("Details", new { id = review.ProductId });
            }

            // Cập nhật nội dung mới
            review.Rating = rating;
            review.Comment = comment.Trim();
            review.IsEdited = true;
            review.UpdatedDate = DateTime.Now;

            // LƯU Ý: Khách đổi ý (Sửa sao/Sửa chữ) thì câu trả lời cũ của Admin không còn hợp lý nữa, nên xóa đi để Admin trả lời lại
            review.AdminReply = null;
            review.RepliedAt = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã cập nhật đánh giá thành công!";
            return RedirectToAction("Details", new { id = review.ProductId });
        }
    }
}