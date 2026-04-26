using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PromotionController : Controller
    {
        private readonly AppDbContext _context;

        public PromotionController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, string status, int page = 1)
        {
            // Sửa Include từ PromotionProducts thành PromotionVariants
            var query = _context.Promotions
                                .Include(p => p.PromotionVariants)
                                .AsQueryable();
            var now = DateTime.Now;

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "ongoing") query = query.Where(p => p.IsActive == true && p.StartDate <= now && p.EndDate >= now);
                else if (status == "upcoming") query = query.Where(p => p.IsActive == true && p.StartDate > now);
                else if (status == "ended") query = query.Where(p => p.IsActive == true && p.EndDate < now);
                else if (status == "disabled") query = query.Where(p => p.IsActive == false);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(p => p.PromotionName.Contains(searchString));
            }

            int pageSize = 5;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var promotions = await query.OrderByDescending(p => p.PromotionId)
                                        .Skip((page - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(promotions);
        }

        // =========================================================================
        // HÀM MỚI (AJAX): Lấy danh sách Biến thể của 1 Sản phẩm để hiển thị ra View
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> GetVariantsByProduct(int productId)
        {
            var variants = await _context.ProductVariants
                .Include(v => v.AttributeValues)
                    .ThenInclude(av => av.PredefinedAttributeValue)
                .Where(v => v.ProductId == productId)
                .Select(v => new {
                    variantId = v.VariantId,
                    price = v.Price,
                    // Nối các thuộc tính lại thành tên (VD: 12GB / Xanh dương)
                    variantName = string.Join(" - ", v.AttributeValues.Select(a => a.PredefinedAttributeValue.Value))
                })
                .ToListAsync();

            return Json(variants);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Products = await _context.Products.OrderByDescending(p => p.ProductId).ToListAsync();
            var defaultPromo = new Promotion
            {
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7)
            };
            return View(defaultPromo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // LƯU Ý: Đổi 'selectedProducts' thành 'selectedVariants'
        public async Task<IActionResult> Create(Promotion promotion, int[] selectedVariants, int[] discounts)
        {
            ModelState.Remove("PromotionVariants");
            if (string.IsNullOrEmpty(promotion.Description)) ModelState.Remove("Description");

            if (ModelState.IsValid)
            {
                if (selectedVariants == null || selectedVariants.Length == 0)
                {
                    ModelState.AddModelError("", "Vui lòng cấu hình giảm giá cho ít nhất 1 biến thể sản phẩm!");
                    ViewBag.Products = await _context.Products.ToListAsync();
                    return View(promotion);
                }

                if (promotion.EndDate <= promotion.StartDate)
                {
                    ModelState.AddModelError("EndDate", "Ngày kết thúc phải lớn hơn ngày bắt đầu!");
                    ViewBag.Products = await _context.Products.ToListAsync();
                    return View(promotion);
                }

                _context.Promotions.Add(promotion);
                await _context.SaveChangesAsync();

                // Lưu các Biến thể được chọn
                for (int i = 0; i < selectedVariants.Length; i++)
                {
                    // Chỉ lưu những biến thể có % giảm giá > 0
                    if (discounts != null && discounts.Length > i && discounts[i] > 0)
                    {
                        var promoVariant = new PromotionVariant
                        {
                            PromotionId = promotion.PromotionId,
                            VariantId = selectedVariants[i], // Đã sửa thành VariantId
                            DiscountPercent = discounts[i]
                        };
                        _context.PromotionVariants.Add(promoVariant);
                    }
                }
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã tạo thành công chiến dịch: {promotion.PromotionName}";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Products = await _context.Products.ToListAsync();
            return View(promotion);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var promotion = await _context.Promotions
                .Include(p => p.PromotionVariants)
                    .ThenInclude(pv => pv.ProductVariant)
                        .ThenInclude(v => v.Product) // Lấy tên Sản phẩm gốc
                .Include(p => p.PromotionVariants)
                    .ThenInclude(pv => pv.ProductVariant)
                        .ThenInclude(v => v.AttributeValues)
                            .ThenInclude(av => av.PredefinedAttributeValue) // Lấy tên Cấu hình (RAM/Màu)
                .FirstOrDefaultAsync(p => p.PromotionId == id);

            if (promotion == null) return NotFound();

            ViewBag.Products = await _context.Products.OrderByDescending(p => p.ProductId).ToListAsync();

            return View(promotion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Promotion promotion, int[] selectedVariants, int[] discounts)
        {
            if (id != promotion.PromotionId) return NotFound();
            ModelState.Remove("PromotionVariants");
            if (string.IsNullOrEmpty(promotion.Description)) ModelState.Remove("Description");

            if (ModelState.IsValid)
            {
                if (selectedVariants == null || selectedVariants.Length == 0)
                {
                    ModelState.AddModelError("", "Vui lòng cấu hình giảm giá cho ít nhất 1 biến thể sản phẩm!");
                    ViewBag.Products = await _context.Products.ToListAsync();
                    return View(promotion);
                }
                if (promotion.EndDate <= promotion.StartDate)
                {
                    ModelState.AddModelError("EndDate", "Ngày kết thúc phải lớn hơn ngày bắt đầu!");
                    ViewBag.Products = await _context.Products.ToListAsync();
                    return View(promotion);
                }

                try
                {
                    var existingPromo = await _context.Promotions
                        .Include(p => p.PromotionVariants)
                        .FirstOrDefaultAsync(p => p.PromotionId == id);

                    if (existingPromo == null) return NotFound();

                    existingPromo.PromotionName = promotion.PromotionName;
                    existingPromo.Description = promotion.Description;
                    existingPromo.StartDate = promotion.StartDate;
                    existingPromo.EndDate = promotion.EndDate;
                    existingPromo.IsActive = promotion.IsActive;

                    // Xóa các liên kết biến thể cũ
                    _context.PromotionVariants.RemoveRange(existingPromo.PromotionVariants);

                    // Thêm lại cấu hình biến thể mới
                    for (int i = 0; i < selectedVariants.Length; i++)
                    {
                        if (discounts != null && discounts.Length > i && discounts[i] > 0)
                        {
                            _context.PromotionVariants.Add(new PromotionVariant
                            {
                                PromotionId = promotion.PromotionId,
                                VariantId = selectedVariants[i],
                                DiscountPercent = discounts[i]
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã cập nhật thành công chiến dịch: {promotion.PromotionName}";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Promotions.Any(e => e.PromotionId == promotion.PromotionId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Products = await _context.Products.ToListAsync();
            return View(promotion);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var promotion = await _context.Promotions
                .Include(p => p.PromotionVariants)
                .FirstOrDefaultAsync(p => p.PromotionId == id);

            if (promotion == null) return NotFound();
            try
            {
                _context.PromotionVariants.RemoveRange(promotion.PromotionVariants);
                _context.Promotions.Remove(promotion);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa thành công chiến dịch: {promotion.PromotionName}";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Lỗi: Không thể xóa chiến dịch này!";
            }
            return RedirectToAction(nameof(Index));
        }
        // TÍNH NĂNG TẮT KHẨN CẤP KHUYẾN MÃI TỪ BÊN NGOÀI DANH SÁCH (SOFT DELETE)
        public async Task<IActionResult> Disable(int? id)
        {
            if (id == null) return NotFound();

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null) return NotFound();

            // Thực hiện Soft Delete: Tắt trạng thái hiển thị
            promotion.IsActive = false;

            _context.Update(promotion);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã tắt thành công chiến dịch '{promotion.PromotionName}'.";
            return RedirectToAction(nameof(Index));
        }
    }
}