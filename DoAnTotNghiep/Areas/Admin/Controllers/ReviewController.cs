using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class ReviewController : Controller
    {
        private readonly AppDbContext _context;

        public ReviewController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, bool? isApproved, int? rating, int page = 1)
        {
            // Bổ sung Include ReviewLikes để lấy số lượng người đã bấm Hữu ích
            var query = _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.ReviewLikes)
                .AsQueryable();

            if (isApproved.HasValue)
            {
                query = query.Where(r => r.IsApproved == isApproved.Value);
            }
            if (rating.HasValue)
            {
                query = query.Where(r => r.Rating == rating.Value);
            }
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(r => r.UserName.Contains(searchString)
                                      || r.Product.ProductName.Contains(searchString)
                                      || r.Comment.Contains(searchString));
            }

            int pageSize = 5;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var reviews = await query.OrderByDescending(r => r.CreatedDate)
                                     .Skip((page - 1) * pageSize)
                                     .Take(pageSize)
                                     .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.IsApproved = isApproved;
            ViewBag.Rating = rating;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(reviews);
        }

        public async Task<IActionResult> ToggleStatus(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                review.IsApproved = !review.IsApproved;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = review.IsApproved ? "Đã duyệt và hiển thị bình luận!" : "Đã ẩn bình luận thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa vĩnh viễn đánh giá!";
            }
            return RedirectToAction(nameof(Index));
        }

        // ========================================================
        // TÍNH NĂNG MỚI 1: ADMIN PHẢN HỒI ĐÁNH GIÁ CỦA KHÁCH
        // ========================================================
        [HttpPost]
        public async Task<IActionResult> Reply(int reviewId, string adminReply)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review != null)
            {
                // Nếu nội dung trống thì coi như là xóa phản hồi
                if (string.IsNullOrWhiteSpace(adminReply))
                {
                    review.AdminReply = null;
                    review.RepliedAt = null;
                    TempData["SuccessMessage"] = "Đã thu hồi câu trả lời!";
                }
                else
                {
                    review.AdminReply = adminReply.Trim();
                    review.RepliedAt = DateTime.Now;
                    TempData["SuccessMessage"] = "Đã gửi phản hồi cho khách hàng!";
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá này!";
            }
            return RedirectToAction(nameof(Index));
        }

        // ========================================================
        // TÍNH NĂNG MỚI 2: XEM DANH SÁCH NHỮNG NGƯỜI ĐÃ BẤM LIKE
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> GetLikes(int reviewId)
        {
            var likes = await _context.ReviewLikes
                .Where(l => l.ReviewId == reviewId)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new {
                    userName = string.IsNullOrEmpty(l.UserName) ? "Khách ẩn danh" : l.UserName,
                    createdAt = l.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            return Json(likes);
        }
    }
}