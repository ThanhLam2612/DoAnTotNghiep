using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class ProductAttributeController : Controller
    {
        private readonly AppDbContext _context;

        public ProductAttributeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, int? pageNumber)
        {
            // 1. Giữ lại từ khóa tìm kiếm trên ô input khi load lại trang
            ViewData["CurrentFilter"] = searchString;

            // 2. Tạo câu truy vấn gốc (Chưa thực thi ngay nhờ AsQueryable)
            var query = _context.ProductAttributes
                                .Include(a => a.Category)
                                .AsQueryable();

            // 3. XỬ LÝ TÌM KIẾM
            if (!string.IsNullOrEmpty(searchString))
            {
                // Tìm theo Tên thuộc tính HOẶC Tên danh mục
                query = query.Where(a => a.AttributeName.Contains(searchString) ||
                                        (a.Category != null && a.Category.CategoryName.Contains(searchString)));
            }

            // 4. Sắp xếp mặc định (Mới nhất lên đầu)
            query = query.OrderByDescending(a => a.AttributeId);

            // 5. XỬ LÝ PHÂN TRANG
            int pageSize = 5; // Số dòng trên 1 trang (bạn có thể đổi thành 5, 20 tùy ý)
            int pageIndex = pageNumber ?? 1; // Nếu không truyền trang nào thì mặc định là trang 1

            int totalItems = await query.CountAsync(); // Tổng số dòng thỏa mãn điều kiện
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize); // Tính tổng số trang

            // Lấy data của trang hiện tại (Dùng Skip và Take)
            var attributes = await query.Skip((pageIndex - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync();

            // Truyền thông tin phân trang sang View
            ViewBag.CurrentPage = pageIndex;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(attributes);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // TRUYỀN DANH SÁCH DANH MỤC SANG VIEW ĐỂ LÀM COMBOBOX
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // BỔ SUNG THÊM CategoryId VÀO THUỘC TÍNH [Bind]
        public async Task<IActionResult> Create([Bind("AttributeId,AttributeName,CategoryId")] ProductAttribute attribute)
        {
            ModelState.Remove("Category");
            if (ModelState.IsValid)
            {
                // LOGIC MỚI: Chỉ báo trùng khi trùng Tên thuộc tính VÀ trùng cả Danh mục
                bool exists = await _context.ProductAttributes.AnyAsync(a =>
                    a.AttributeName.ToLower() == attribute.AttributeName.ToLower() &&
                    a.CategoryId == attribute.CategoryId);

                if (exists)
                {
                    ModelState.AddModelError("AttributeName", "Thuộc tính này đã tồn tại trong danh mục tương ứng!");
                    ViewBag.Categories = await _context.Categories.ToListAsync(); // Load lại data nếu bị lỗi
                    return View(attribute);
                }

                _context.Add(attribute);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm thuộc tính thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(attribute);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var attribute = await _context.ProductAttributes.FindAsync(id);
            if (attribute != null)
            {
                // LOGIC MỚI 1: Kiểm tra xem Thuộc tính này có đang được gán cho Biến thể nào không (Thông qua Kho từ điển)
                bool isUsedInVariants = await _context.VariantAttributeValues
                    .AnyAsync(v => v.PredefinedAttributeValue != null && v.PredefinedAttributeValue.AttributeId == id);

                if (isUsedInVariants)
                {
                    TempData["ErrorMessage"] = "Không thể xóa! Đang có sản phẩm sử dụng thuộc tính này.";
                    return RedirectToAction(nameof(Index));
                }

                // LOGIC MỚI 2: Nếu chưa gán cho SP nào, nhưng trong Kho từ điển đang có chứa giá trị (VD: đã tạo 128GB, 256GB...)
                bool hasPredefinedValues = await _context.PredefinedAttributeValues.AnyAsync(p => p.AttributeId == id);
                if (hasPredefinedValues)
                {
                    TempData["ErrorMessage"] = "Không thể xóa! Vui lòng xóa hết các giá trị định sẵn (trong Kho từ điển) của thuộc tính này trước.";
                    return RedirectToAction(nameof(Index));
                }

                _context.ProductAttributes.Remove(attribute);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa thuộc tính thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var attribute = await _context.ProductAttributes.FindAsync(id);
            if (attribute == null) return NotFound();

            // TRUYỀN DANH SÁCH DANH MỤC SANG VIEW
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(attribute);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // BỔ SUNG THÊM CategoryId VÀO THUỘC TÍNH [Bind]
        public async Task<IActionResult> Edit(int id, [Bind("AttributeId,AttributeName,CategoryId")] ProductAttribute attribute)
        {
            if (id != attribute.AttributeId) return NotFound();
            ModelState.Remove("Category");
            if (ModelState.IsValid)
            {
                try
                {
                    // LOGIC MỚI: Check trùng (Loại trừ ID hiện tại)
                    bool exists = await _context.ProductAttributes.AnyAsync(a =>
                        a.AttributeName.ToLower() == attribute.AttributeName.ToLower() &&
                        a.CategoryId == attribute.CategoryId &&
                        a.AttributeId != id);

                    if (exists)
                    {
                        ModelState.AddModelError("AttributeName", "Thuộc tính này đã tồn tại trong danh mục tương ứng!");
                        ViewBag.Categories = await _context.Categories.ToListAsync();
                        return View(attribute);
                    }

                    _context.Update(attribute);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thuộc tính thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductAttributeExists(attribute.AttributeId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(attribute);
        }

        private bool ProductAttributeExists(int id)
        {
            return _context.ProductAttributes.Any(e => e.AttributeId == id);
        }
    }
}
