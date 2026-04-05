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
            var query = _context.Promotions
                                .Include(p => p.PromotionProducts)
                                .AsQueryable();
            var now = DateTime.Now;
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "ongoing") 
                {
                    query = query.Where(p => p.IsActive == true && p.StartDate <= now && p.EndDate >= now);
                }
                else if (status == "upcoming")
                {
                    query = query.Where(p => p.IsActive == true && p.StartDate > now);
                }
                else if (status == "ended") 
                {
                    query = query.Where(p => p.IsActive == true && p.EndDate < now);
                }
                else if (status == "disabled")
                {
                    query = query.Where(p => p.IsActive == false);
                }
            }
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(p => p.PromotionName.Contains(searchString));
            }
            int pageSize = 10;
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
        public async Task<IActionResult> Create(Promotion promotion, int[] selectedProducts)
        {
            ModelState.Remove("PromotionProducts");
            if (string.IsNullOrEmpty(promotion.Description))
            {
                ModelState.Remove("Description");
            }
            if (ModelState.IsValid)
            {
                if (selectedProducts == null || selectedProducts.Length == 0)
                {
                    ModelState.AddModelError("", "⚠️ Vui lòng tick chọn ít nhất 1 sản phẩm tham gia khuyến mãi!");
                    ViewBag.Products = await _context.Products.ToListAsync();
                    ViewBag.SelectedProducts = selectedProducts.ToList();
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
                if (selectedProducts != null && selectedProducts.Length > 0)
                {
                    foreach (var productId in selectedProducts)
                    {
                        var promoProduct = new PromotionProduct
                        {
                            PromotionId = promotion.PromotionId,
                            ProductId = productId
                        };
                        _context.PromotionProducts.Add(promoProduct);
                    }
                    await _context.SaveChangesAsync();
                }
                TempData["SuccessMessage"] = $"🎉 Đã tạo thành công chiến dịch: {promotion.PromotionName}";
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
                .Include(p => p.PromotionProducts)
                .FirstOrDefaultAsync(p => p.PromotionId == id);
            if (promotion == null) return NotFound();
            ViewBag.Products = await _context.Products.OrderByDescending(p => p.ProductId).ToListAsync();
            ViewBag.SelectedProducts = promotion.PromotionProducts.Select(pp => pp.ProductId).ToList();
            return View(promotion);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Promotion promotion, int[] selectedProducts)
        {
            if (id != promotion.PromotionId) return NotFound();
            ModelState.Remove("PromotionProducts");
            if (string.IsNullOrEmpty(promotion.Description)) ModelState.Remove("Description");
            if (ModelState.IsValid)
            {
                if (selectedProducts == null || selectedProducts.Length == 0)
                {
                    ModelState.AddModelError("", "⚠️ Vui lòng tick chọn ít nhất 1 sản phẩm tham gia khuyến mãi!");
                    ViewBag.Products = await _context.Products.ToListAsync();
                    ViewBag.SelectedProducts = selectedProducts.ToList();
                    return View(promotion);
                }
                if (promotion.EndDate <= promotion.StartDate)
                {
                    ModelState.AddModelError("EndDate", "Ngày kết thúc phải lớn hơn ngày bắt đầu!");
                    ViewBag.Products = await _context.Products.ToListAsync();
                    ViewBag.SelectedProducts = selectedProducts.ToList();
                    return View(promotion);
                }
                try
                {
                    var existingPromo = await _context.Promotions
                        .Include(p => p.PromotionProducts)
                        .FirstOrDefaultAsync(p => p.PromotionId == id);
                    if (existingPromo == null) return NotFound();
                    existingPromo.PromotionName = promotion.PromotionName;
                    existingPromo.Description = promotion.Description;
                    existingPromo.DiscountPercent = promotion.DiscountPercent;
                    existingPromo.StartDate = promotion.StartDate;
                    existingPromo.EndDate = promotion.EndDate;
                    existingPromo.IsActive = promotion.IsActive;
                    _context.PromotionProducts.RemoveRange(existingPromo.PromotionProducts);
                    foreach (var productId in selectedProducts)
                    {
                        _context.PromotionProducts.Add(new PromotionProduct
                        {
                            PromotionId = promotion.PromotionId,
                            ProductId = productId
                        });
                    }
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"✏️ Đã cập nhật thành công chiến dịch: {promotion.PromotionName}";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Promotions.Any(e => e.PromotionId == promotion.PromotionId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Products = await _context.Products.ToListAsync();
            ViewBag.SelectedProducts = selectedProducts?.ToList() ?? new List<int>();
            return View(promotion);
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var promotion = await _context.Promotions
                .Include(p => p.PromotionProducts)
                .FirstOrDefaultAsync(p => p.PromotionId == id);
            if (promotion == null) return NotFound();
            try
            {
                _context.PromotionProducts.RemoveRange(promotion.PromotionProducts);
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
    }
}
