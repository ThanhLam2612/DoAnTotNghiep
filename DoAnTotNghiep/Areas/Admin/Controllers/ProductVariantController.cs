using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

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

        public async Task<IActionResult> Index(int productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null) return NotFound();
            ViewBag.Product = product;

            var variants = await _context.ProductVariants
                .Include(v => v.ProductImages) // THÊM DÒNG NÀY ĐỂ LẤY GALLERY ẢNH
                .Include(v => v.AttributeValues)
                    .ThenInclude(av => av.PredefinedAttributeValue)
                        .ThenInclude(p => p.ProductAttribute)
                .Where(v => v.ProductId == productId)
                .ToListAsync();
            return View(variants);
        }

        // =========================================================================
        // HÀM CREATE MỚI: HIỂN THỊ GIAO DIỆN TẠO MA TRẬN BIẾN THỂ
        // =========================================================================
        public async Task<IActionResult> Create(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            ViewBag.Product = product;

            // Lấy các Tùy chọn và Kho từ điển (Dùng chung hoặc của Danh mục này)
            ViewBag.Attributes = await _context.ProductAttributes
                .Include(a => a.PredefinedValues)
                .Where(a => a.CategoryId == null || a.CategoryId == product.CategoryId)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productId, decimal price, int stockQuantity, IFormFile? imageFile, IFormFileCollection? galleryFiles, IFormCollection form)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            // 1. Lấy danh sách ID thuộc tính mà Admin đã chọn từ các thẻ Select
            var allAttributes = await _context.ProductAttributes
                .Where(a => a.CategoryId == null || a.CategoryId == product.CategoryId)
                .ToListAsync();

            var selectedPredefinedIds = new List<int>();
            foreach (var attr in allAttributes)
            {
                string selectedValue = form[$"attribute_{attr.AttributeId}"];
                if (int.TryParse(selectedValue, out int predefinedId))
                {
                    selectedPredefinedIds.Add(predefinedId);
                }
            }

            if (!selectedPredefinedIds.Any())
            {
                TempData["ErrorMessage"] = "Bạn phải chọn ít nhất 1 thông số kỹ thuật!";
                return RedirectToAction(nameof(Create), new { productId });
            }

            // 2. Kiểm tra trùng lặp: Xem tổ hợp này đã tồn tại chưa?
            var existingVariants = await _context.ProductVariants
                .Include(v => v.AttributeValues)
                .Where(v => v.ProductId == productId)
                .ToListAsync();

            bool isDuplicate = false;
            foreach (var ev in existingVariants)
            {
                var evIds = ev.AttributeValues.Select(a => a.PredefinedValueId).ToList();
                if (selectedPredefinedIds.Count == evIds.Count && !selectedPredefinedIds.Except(evIds).Any())
                {
                    isDuplicate = true;
                    break;
                }
            }

            if (isDuplicate)
            {
                TempData["ErrorMessage"] = "Lỗi: Tổ hợp cấu hình này đã tồn tại trong sản phẩm! Vui lòng chọn tổ hợp khác.";
                return RedirectToAction(nameof(Create), new { productId });
            }

            // Đường dẫn chung để lưu ảnh biến thể
            string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "variants");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            // 3. Xử lý tải lên "Ảnh đại diện" duy nhất
            string imageUrl = null;
            if (imageFile != null && imageFile.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                using (var fileStream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                imageUrl = "/images/variants/" + fileName;
            }

            // 4. Khởi tạo và lưu biến thể mới (Lấy Id)
            var newVariant = new ProductVariant
            {
                ProductId = productId,
                Price = price,
                StockQuantity = stockQuantity,
                ImageUrl = imageUrl,
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? "Hệ thống"
            };

            _context.ProductVariants.Add(newVariant);
            await _context.SaveChangesAsync(); // Cần SaveChanges trước để lấy newVariant.VariantId

            // 5. Xử lý tải lên nhiều ảnh cho Gallery (Lưu vào bảng ProductImage)
            if (galleryFiles != null && galleryFiles.Any())
            {
                foreach (var file in galleryFiles)
                {
                    if (file.Length > 0)
                    {
                        string galleryFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        using (var fileStream = new FileStream(Path.Combine(uploadPath, galleryFileName), FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        string galleryImageUrl = "/images/variants/" + galleryFileName;

                        // Tạo đối tượng ProductImage mới
                        var productImage = new ProductImage
                        {
                            ProductId = productId,
                            VariantId = newVariant.VariantId, // Liên kết với biến thể mới tạo
                            ImageUrl = galleryImageUrl, // Đảm bảo tên thuộc tính khớp với Model ProductImage.cs của bạn (vd: ImageUrl, Url, Path, ImagePath)
                            
                        };
                        _context.ProductImages.Add(productImage);
                    }
                }
            }

            // 6. Lưu các liên kết thuộc tính
            foreach (var valId in selectedPredefinedIds)
            {
                _context.VariantAttributeValues.Add(new VariantAttributeValue
                {
                    VariantId = newVariant.VariantId,
                    PredefinedValueId = valId
                });
            }

            // Cuối cùng, lưu tất cả ảnh gallery và liên kết thuộc tính vào database
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã tạo cấu hình mới và thư viện ảnh thành công!";
            return RedirectToAction(nameof(Index), new { productId });
        }


        // =========================================================================
        // HÀM MỚI: DÀNH CHO AJAX GỌI TỪ TRANG TẠO BIẾN THỂ (ADD-ON-THE-FLY)
        // =========================================================================
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CreateQuick([FromForm] int attributeId, [FromForm] string value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return Json(new { success = false, message = "Giá trị không được để trống!" });
                }

                bool exists = await _context.PredefinedAttributeValues.AnyAsync(v =>
                    v.AttributeId == attributeId && v.Value.ToLower() == value.Trim().ToLower());

                if (exists)
                {
                    return Json(new { success = false, message = $"Giá trị '{value}' đã tồn tại trong danh sách!" });
                }

                var newValue = new PredefinedAttributeValue
                {
                    AttributeId = attributeId,
                    Value = value.Trim(),
                    ColorHex = ""
                };

                _context.PredefinedAttributeValues.Add(newValue);
                await _context.SaveChangesAsync();

                return Json(new { success = true, id = newValue.PredefinedValueId, value = newValue.Value });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // =========================================================================
        // HÀM MỚI: HIỂN THỊ GIAO DIỆN SỬA HÀNG LOẠT (BULK EDIT)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> EditBulk(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();
            ViewBag.Product = product;

            var variants = await _context.ProductVariants
                .Include(v => v.AttributeValues)
                    .ThenInclude(av => av.PredefinedAttributeValue)
                        .ThenInclude(p => p.ProductAttribute)
                .Where(v => v.ProductId == productId)
                .ToListAsync();

            return View(variants);
        }

        // =========================================================================
        // HÀM MỚI: XỬ LÝ LƯU SỬA HÀNG LOẠT
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBulk(int productId, List<VariantUpdateVM> updates)
        {
            if (updates == null || !updates.Any()) return RedirectToAction(nameof(Index), new { productId });

            int successCount = 0;
            foreach (var item in updates)
            {
                var variant = await _context.ProductVariants.FindAsync(item.VariantId);
                if (variant != null)
                {
                    // Cập nhật Giá và Tồn kho
                    variant.Price = item.Price;
                    variant.StockQuantity = item.StockQuantity;

                    // LƯU LỊCH SỬ LÚC SỬA
                    variant.UpdatedAt = DateTime.Now;
                    variant.UpdatedBy = User.Identity?.Name ?? "Hệ thống";

                    // Cập nhật Ảnh nếu có tải lên ảnh mới
                    if (item.ImageFile != null && item.ImageFile.Length > 0)
                    {
                        // Xóa ảnh cũ vật lý
                        if (!string.IsNullOrEmpty(variant.ImageUrl))
                        {
                            string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", variant.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        }

                        // Lưu ảnh mới
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(item.ImageFile.FileName);
                        string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "variants");
                        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                        using (var fileStream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                        {
                            await item.ImageFile.CopyToAsync(fileStream);
                        }
                        variant.ImageUrl = "/images/variants/" + fileName;
                    }

                    _context.Update(variant);
                    successCount++;
                }
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã lưu thay đổi cho {successCount} cấu hình!";
            return RedirectToAction(nameof(Index), new { productId });
        }

        // =========================================================================
        // HÀM HIỂN THỊ GIAO DIỆN SỬA 1 BIẾN THỂ
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.AttributeValues)
                .Include(v => v.ProductImages) // Lấy cả gallery cũ lên
                .FirstOrDefaultAsync(v => v.VariantId == id);

            if (variant == null) return NotFound();

            var product = await _context.Products.FindAsync(variant.ProductId);
            ViewBag.Product = product;

            // Lấy toàn bộ thuộc tính của danh mục để hiển thị Dropdown
            ViewBag.Attributes = await _context.ProductAttributes
                .Include(a => a.PredefinedValues)
                .Where(a => a.CategoryId == null || a.CategoryId == product.CategoryId)
                .ToListAsync();

            return View(variant);
        }

        // =========================================================================
        // HÀM XỬ LÝ LƯU GIAO DIỆN SỬA
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, decimal price, int stockQuantity, IFormFile? imageFile, IFormFileCollection? galleryFiles, List<int>? deletedImageIds, IFormCollection form)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .Include(v => v.AttributeValues)
                .Include(v => v.ProductImages)
                .FirstOrDefaultAsync(v => v.VariantId == id);

            if (variant == null) return NotFound();

            // 1. Lấy thông số kỹ thuật mới từ Form
            var allAttributes = await _context.ProductAttributes.Where(a => a.CategoryId == null || a.CategoryId == variant.Product.CategoryId).ToListAsync();
            var selectedPredefinedIds = new List<int>();
            foreach (var attr in allAttributes)
            {
                if (int.TryParse(form[$"attribute_{attr.AttributeId}"], out int predefinedId))
                {
                    selectedPredefinedIds.Add(predefinedId);
                }
            }

            if (!selectedPredefinedIds.Any())
            {
                TempData["ErrorMessage"] = "Bạn phải chọn ít nhất 1 thông số kỹ thuật!";
                return RedirectToAction(nameof(Edit), new { id = variant.VariantId });
            }

            // 2. Kiểm tra trùng lặp (Loại trừ chính nó ra)
            var existingVariants = await _context.ProductVariants
                .Include(v => v.AttributeValues)
                .Where(v => v.ProductId == variant.ProductId && v.VariantId != id)
                .ToListAsync();

            bool isDuplicate = false;
            foreach (var ev in existingVariants)
            {
                var evIds = ev.AttributeValues.Select(a => a.PredefinedValueId).ToList();
                if (selectedPredefinedIds.Count == evIds.Count && !selectedPredefinedIds.Except(evIds).Any())
                {
                    isDuplicate = true; break;
                }
            }

            if (isDuplicate)
            {
                TempData["ErrorMessage"] = "Tổ hợp cấu hình này đã bị trùng với một biến thể khác của sản phẩm!";
                return RedirectToAction(nameof(Edit), new { id = variant.VariantId });
            }

            // 3. Cập nhật thông tin cơ bản & Audit Logs
            variant.Price = price;
            variant.StockQuantity = stockQuantity;
            variant.UpdatedAt = DateTime.Now;
            variant.UpdatedBy = User.Identity?.Name ?? "Hệ thống";

            string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "variants");

            // 4. Xử lý Ảnh đại diện (Nếu up ảnh mới thì xóa ảnh cũ)
            if (imageFile != null && imageFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(variant.ImageUrl))
                {
                    string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", variant.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                using (var fileStream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                variant.ImageUrl = "/images/variants/" + fileName;
            }

            // 5. XÓA các ảnh Gallery cũ (Nếu Admin bấm nút Xóa trên giao diện)
            if (deletedImageIds != null && deletedImageIds.Any())
            {
                var imagesToDelete = variant.ProductImages.Where(img => deletedImageIds.Contains(img.ImageId)).ToList();
                foreach (var img in imagesToDelete)
                {
                    if (!string.IsNullOrEmpty(img.ImageUrl))
                    {
                        string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", img.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }
                    _context.ProductImages.Remove(img);
                }
            }

            // 6. THÊM các ảnh Gallery mới
            if (galleryFiles != null && galleryFiles.Any())
            {
                foreach (var file in galleryFiles)
                {
                    if (file.Length > 0)
                    {
                        string galleryFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        using (var fileStream = new FileStream(Path.Combine(uploadPath, galleryFileName), FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        _context.ProductImages.Add(new ProductImage
                        {
                            ProductId = variant.ProductId, // Quan trọng: Khóa ngoại
                            VariantId = variant.VariantId,
                            ImageUrl = "/images/variants/" + galleryFileName
                        });
                    }
                }
            }

            // 7. Cập nhật Thông số (Xóa hết cái cũ, gán cái mới)
            _context.VariantAttributeValues.RemoveRange(variant.AttributeValues);
            foreach (var valId in selectedPredefinedIds)
            {
                _context.VariantAttributeValues.Add(new VariantAttributeValue { VariantId = variant.VariantId, PredefinedValueId = valId });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật biến thể thành công!";
            return RedirectToAction(nameof(Index), new { productId = variant.ProductId });
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Lấy biến thể cùng toàn bộ ảnh và thông số của nó
            var variant = await _context.ProductVariants
                .Include(v => v.AttributeValues)
                .Include(v => v.ProductImages)
                .FirstOrDefaultAsync(v => v.VariantId == id);

            if (variant == null)
            {
                return NotFound();
            }

            int productId = variant.ProductId; // Lưu lại ID sản phẩm gốc để lát chuyển trang

            // 1. KIỂM TRA RÀNG BUỘC (Rất quan trọng):
            // Xem biến thể này có nằm trong đơn hàng nào chưa?
            bool isOrdered = await _context.OrderDetails.AnyAsync(od => od.VariantId == id);
            if (isOrdered)
            {
                TempData["ErrorMessage"] = "Lỗi: Không thể xóa! Biến thể này đã phát sinh đơn hàng. Bạn chỉ nên vào phần 'Sửa' và đổi 'Tồn kho' về 0.";
                return RedirectToAction(nameof(Index), new { productId = productId });
            }

            // 2. DỌN DẸP Ổ CỨNG: Xóa ảnh đại diện vật lý (nếu có)
            if (!string.IsNullOrEmpty(variant.ImageUrl))
            {
                string oldMainPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", variant.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldMainPath)) System.IO.File.Delete(oldMainPath);
            }

            // 3. DỌN DẸP Ổ CỨNG: Xóa các ảnh Gallery vật lý (nếu có)
            if (variant.ProductImages != null && variant.ProductImages.Any())
            {
                foreach (var img in variant.ProductImages)
                {
                    if (!string.IsNullOrEmpty(img.ImageUrl))
                    {
                        string oldGalleryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", img.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldGalleryPath)) System.IO.File.Delete(oldGalleryPath);
                    }
                }
                // Xóa danh sách ảnh Gallery trong Database
                _context.ProductImages.RemoveRange(variant.ProductImages);
            }

            // 4. Xóa liên kết thông số (RAM, Màu...) trong Database
            if (variant.AttributeValues != null)
            {
                _context.VariantAttributeValues.RemoveRange(variant.AttributeValues);
            }

            // 5. Cuối cùng, xóa biến thể
            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa biến thể thành công!";
            return RedirectToAction(nameof(Index), new { productId = productId });
        }
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteAjax([FromForm] int variantId)
        {
            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant == null) return Json(new { success = false, message = "Không tìm thấy biến thể." });

            bool isOrdered = await _context.OrderDetails.AnyAsync(od => od.VariantId == variantId);
            if (isOrdered) return Json(new { success = false, message = "Biến thể này đã có đơn hàng, không thể xóa." });

            var oldAttributes = _context.VariantAttributeValues.Where(v => v.VariantId == variantId);
            _context.VariantAttributeValues.RemoveRange(oldAttributes);
            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // =========================================================================
        // SỬA NHANH GIÁ TRỊ THUỘC TÍNH BẰNG AJAX
        // =========================================================================
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> EditQuick([FromForm] int id, [FromForm] string newValue)
        {
            if (string.IsNullOrWhiteSpace(newValue))
                return Json(new { success = false, message = "Giá trị không được để trống!" });

            var attrValue = await _context.PredefinedAttributeValues.FindAsync(id);
            if (attrValue == null) return Json(new { success = false, message = "Không tìm thấy giá trị này." });

            // Kiểm tra trùng tên trong cùng một nhóm Thuộc tính (vd: trùng 2 chữ "Đỏ" trong nhóm "Màu")
            bool exists = await _context.PredefinedAttributeValues.AnyAsync(v =>
                v.AttributeId == attrValue.AttributeId &&
                v.PredefinedValueId != id &&
                v.Value.ToLower() == newValue.Trim().ToLower());

            if (exists) return Json(new { success = false, message = "Giá trị này đã tồn tại!" });

            attrValue.Value = newValue.Trim();
            _context.Update(attrValue);
            await _context.SaveChangesAsync();

            return Json(new { success = true, value = attrValue.Value });
        }

        // =========================================================================
        // XÓA NHANH GIÁ TRỊ THUỘC TÍNH BẰNG AJAX (CÓ KIỂM TRA RÀNG BUỘC)
        // =========================================================================
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteQuick([FromForm] int id)
        {
            var attrValue = await _context.PredefinedAttributeValues.FindAsync(id);
            if (attrValue == null) return Json(new { success = false, message = "Không tìm thấy giá trị này." });

            // RẤT QUAN TRỌNG: Kiểm tra xem chữ "Đỏ" này có đang được gắn cho biến thể của sản phẩm nào không
            bool isUsed = await _context.VariantAttributeValues.AnyAsync(v => v.PredefinedValueId == id);
            if (isUsed)
            {
                return Json(new { success = false, message = "Không thể xóa! Giá trị này đang được sử dụng bởi một hoặc nhiều sản phẩm." });
            }

            _context.PredefinedAttributeValues.Remove(attrValue);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        private bool ProductVariantExists(int id)
        {
            return _context.ProductVariants.Any(e => e.VariantId == id);
        }
    }


    public class VariantVM
    {
        public int VariantId { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public List<int> AttributeValueIds { get; set; }
        public IFormFile? ImageFile { get; set; }
    }

    // DTO cho lúc Sửa Hàng Loạt
    public class VariantUpdateVM
    {
        public int VariantId { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public IFormFile? ImageFile { get; set; }
    }
}