using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class ProductVariantController : Controller
    {
        private readonly AppDbContext _context;

        public ProductVariantController(AppDbContext context)
        {
            _context = context;
        }

        // ==================================================
        // 1. DANH SÁCH BIẾN THỂ (Của 1 sản phẩm cụ thể)
        // ==================================================
        public async Task<IActionResult> Index(int productId)
        {
            // Kiểm tra xem sản phẩm gốc có tồn tại không
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            // Truyền tên sản phẩm ra ngoài View để hiển thị tiêu đề
            ViewBag.ProductName = product.ProductName;
            ViewBag.ProductId = productId;

            // Lấy ra tất cả các biến thể thuộc về Sản phẩm này
            var variants = await _context.ProductVariants
                .Where(v => v.ProductId == productId)
                .ToListAsync();

            return View(variants);
        }

        // ==================================================
        // 2. TẠO MỚI BIẾN THỂ (Giao diện)
        // ==================================================
        [HttpGet]
        public IActionResult Create(int productId)
        {
            var product = _context.Products.Find(productId);
            if (product == null) return NotFound();

            // Truyền ProductId sang Form thông qua ViewBag để gán khóa ngoại
            ViewBag.ProductId = productId;
            ViewBag.ProductName = product.ProductName;
            ViewBag.BasePrice = product.BasePrice; // Có thể truyền giá gốc ra để gợi ý

            return View();
        }

        // ==================================================
        // 3. XỬ LÝ LƯU BIẾN THỂ (POST)
        // ==================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductVariant variant)
        {
            if (ModelState.IsValid)
            {
                _context.Add(variant);
                await _context.SaveChangesAsync();

                // Lưu xong, quay trở về trang Danh sách biến thể của sản phẩm đó
                return RedirectToAction(nameof(Index), new { productId = variant.ProductId });
            }

            // Nếu lỗi, phải nạp lại dữ liệu
            ViewBag.ProductId = variant.ProductId;
            return View(variant);
        }
        // ==========================================
        // 4. GIAO DIỆN SỬA (GET)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Include Product để lấy tên sản phẩm hiển thị ra View
            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.VariantId == id);

            if (variant == null) return NotFound();

            ViewBag.ProductName = variant.Product?.ProductName;
            return View(variant);
        }

        // ==========================================
        // 5. XỬ LÝ SỬA DỮ LIỆU (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductVariant variant)
        {
            if (id != variant.VariantId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(variant);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VariantExists(variant.VariantId)) return NotFound();
                    else throw;
                }
                // Quan trọng: Phải truyền productId về hàm Index để nó load đúng danh sách
                return RedirectToAction(nameof(Index), new { productId = variant.ProductId });
            }
            return View(variant);
        }

        // ==========================================
        // 6. GIAO DIỆN XÁC NHẬN XÓA (GET)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(m => m.VariantId == id);

            if (variant == null) return NotFound();

            return View(variant);
        }

        // ==========================================
        // 7. XỬ LÝ XÓA DỮ LIỆU (POST)
        // ==========================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var variant = await _context.ProductVariants.FindAsync(id);
            int productId = 0; // Biến tạm để lưu ProductId trước khi xóa

            if (variant != null)
            {
                productId = variant.ProductId;
                _context.ProductVariants.Remove(variant);
                await _context.SaveChangesAsync();
            }

            // Xóa xong quay về danh sách biến thể của sản phẩm gốc
            return RedirectToAction(nameof(Index), new { productId = productId });
        }

        private bool VariantExists(int id)
        {
            return _context.ProductVariants.Any(e => e.VariantId == id);
        }
    }
}
