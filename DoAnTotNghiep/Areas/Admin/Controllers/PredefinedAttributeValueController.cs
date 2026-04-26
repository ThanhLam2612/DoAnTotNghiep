using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class PredefinedAttributeValueController : Controller
    {
        private readonly AppDbContext _context;

        public PredefinedAttributeValueController(AppDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách các giá trị của 1 Thuộc tính (VD: Các màu sắc của thuộc tính "Màu sắc")
        public async Task<IActionResult> Index(int attributeId)
        {
            var attribute = await _context.ProductAttributes.FindAsync(attributeId);
            if (attribute == null) return NotFound();

            ViewBag.Attribute = attribute;

            var values = await _context.PredefinedAttributeValues
                .Where(v => v.AttributeId == attributeId)
                .OrderBy(v => v.Value)
                .ToListAsync();

            return View(values);
        }

        // Xử lý Thêm mới Giá trị (Nằm chung trên trang Index bằng Modal/Form nhanh)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PredefinedAttributeValue model)
        {
            ModelState.Remove("ProductAttribute");
            ModelState.Remove("VariantAttributeValues");

            if (ModelState.IsValid)
            {
                // Kiểm tra trùng lặp chữ hoa/chữ thường (VD: đã có "128GB" thì không cho thêm "128gb")
                bool exists = await _context.PredefinedAttributeValues.AnyAsync(v =>
                    v.AttributeId == model.AttributeId && v.Value.ToLower() == model.Value.ToLower());

                if (exists)
                {
                    TempData["ErrorMessage"] = "Giá trị này đã tồn tại trong tùy chọn!";
                    return RedirectToAction(nameof(Index), new { attributeId = model.AttributeId });
                }

                _context.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm giá trị mới thành công!";
            }
            return RedirectToAction(nameof(Index), new { attributeId = model.AttributeId });
        }// =========================================================================
        // HÀM MỚI: DÀNH CHO AJAX GỌI TỪ TRANG TẠO BIẾN THỂ (ADD-ON-THE-FLY)
        // =========================================================================
        [HttpPost]
        [IgnoreAntiforgeryToken] // BẮT BUỘC THÊM DÒNG NÀY ĐỂ KHÔNG BỊ CHẶN AJAX
        public async Task<IActionResult> CreateQuick([FromForm] int attributeId, [FromForm] string value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return Json(new { success = false, message = "Giá trị không được để trống!" });
                }

                // 1. Kiểm tra trùng lặp
                bool exists = await _context.PredefinedAttributeValues.AnyAsync(v =>
                    v.AttributeId == attributeId && v.Value.ToLower() == value.Trim().ToLower());

                if (exists)
                {
                    return Json(new { success = false, message = $"Giá trị '{value}' đã tồn tại trong danh sách!" });
                }

                // 2. Tạo mới
                var newValue = new PredefinedAttributeValue
                {
                    AttributeId = attributeId,
                    Value = value.Trim(),
                    ColorHex = ""
                };

                _context.PredefinedAttributeValues.Add(newValue);
                await _context.SaveChangesAsync();

                // 3. Trả về ID
                return Json(new { success = true, id = newValue.PredefinedValueId, value = newValue.Value });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // Xóa Giá trị khỏi từ điển
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var value = await _context.PredefinedAttributeValues.FindAsync(id);
            if (value != null)
            {
                int attrId = value.AttributeId;

                // KIỂM TRA RÀNG BUỘC: Nếu đã có biến thể dùng giá trị này thì KHÔNG cho xóa
                bool isUsed = await _context.VariantAttributeValues.AnyAsync(v => v.PredefinedValueId == id);
                if (isUsed)
                {
                    TempData["ErrorMessage"] = "Không thể xóa! Giá trị này đang được sử dụng cho một số biến thể sản phẩm.";
                    return RedirectToAction(nameof(Index), new { attributeId = attrId });
                }

                _context.PredefinedAttributeValues.Remove(value);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa giá trị thành công!";
                return RedirectToAction(nameof(Index), new { attributeId = attrId });
            }
            return RedirectToAction("Index", "ProductAttribute");
        }
    }
}