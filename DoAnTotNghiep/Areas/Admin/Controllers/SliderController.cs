using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace DoAnTotNghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SliderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SliderController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // 1. DANH SÁCH SLIDE
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách sắp xếp theo thứ tự
            var sliders = await _context.Sliders.OrderBy(s => s.DisplayOrder).ToListAsync();

            // THUẬT TOÁN TỰ ĐỘNG LÀM PHẲNG (AUTO-HEAL)
            // Nếu phát hiện số bị nhảy cóc (VD: 1, 2, 3, 6), tự động đánh số lại thành 1, 2, 3, 4
            bool isChanged = false;
            for (int i = 0; i < sliders.Count; i++)
            {
                if (sliders[i].DisplayOrder != i + 1)
                {
                    sliders[i].DisplayOrder = i + 1;
                    _context.Update(sliders[i]);
                    isChanged = true;
                }
            }

            // Nếu có sắp xếp lại thì lưu vào Database
            if (isChanged)
            {
                await _context.SaveChangesAsync();
            }

            return View(sliders);
        }

        // 2. TẠO MỚI SLIDE (GET)
        public IActionResult Create()
        {
            return View();
        }

        // 3. TẠO MỚI SLIDE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Slider slider, IFormFile? imageUpload)
        {
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                // Xử lý upload file ảnh
                if (imageUpload != null && imageUpload.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "sliders");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageUpload.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageUpload.CopyToAsync(fileStream);
                    }

                    slider.ImageUrl = "/images/sliders/" + uniqueFileName;
                }
                else
                {
                    ModelState.AddModelError("ImageUrl", "Vui lòng chọn một hình ảnh cho Slide!");
                    return View(slider);
                }

                // =======================================================
                // TỰ ĐỘNG CẤP SỐ THỨ TỰ (Max + 1) ĐỂ ĐẨY XUỐNG CUỐI
                // =======================================================
                var maxOrder = await _context.Sliders.MaxAsync(s => (int?)s.DisplayOrder) ?? 0;
                slider.DisplayOrder = maxOrder + 1;

                // LƯU VẾT THỜI GIAN VÀ NGƯỜI TẠO
                slider.CreatedBy = User.Identity?.Name ?? "Admin";
                slider.CreatedAt = DateTime.Now;

                _context.Sliders.Add(slider);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm Slide mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(slider);
        }

        // 4. SỬA SLIDE (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var slider = await _context.Sliders.FindAsync(id);
            if (slider == null) return NotFound();

            return View(slider);
        }

        // 5. SỬA SLIDE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Slider slider, IFormFile? imageUpload)
        {
            if (id != slider.SliderId) return NotFound();
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy ra bản ghi gốc trong DB để cập nhật nhằm giữ lại Ngày Tạo, Người tạo
                    var existingSlider = await _context.Sliders.FindAsync(id);
                    if (existingSlider == null) return NotFound();

                    // Xử lý Upload ảnh 
                    if (imageUpload != null && imageUpload.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(existingSlider.ImageUrl))
                        {
                            string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingSlider.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "sliders");
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageUpload.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageUpload.CopyToAsync(fileStream);
                        }

                        existingSlider.ImageUrl = "/images/sliders/" + uniqueFileName;
                    }

                    // Cập nhật các trường được phép sửa
                    existingSlider.Title = slider.Title;
                    existingSlider.Description = slider.Description;
                    existingSlider.IsActive = slider.IsActive;

                    // LƯU VẾT THỜI GIAN VÀ NGƯỜI SỬA
                    existingSlider.UpdatedBy = User.Identity?.Name ?? "Admin";
                    existingSlider.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã cập nhật thông tin Slide thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Sliders.Any(e => e.SliderId == slider.SliderId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(slider);
        }

        // 6. XÓA SLIDE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var slider = await _context.Sliders.FindAsync(id);
            if (slider == null) return NotFound();

            try
            {
                // 1. LƯU LẠI VỊ TRÍ CỦA SLIDE SẮP BỊ XÓA
                int deletedOrder = slider.DisplayOrder;

                // 2. Xóa file ảnh vật lý
                if (!string.IsNullOrEmpty(slider.ImageUrl))
                {
                    string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, slider.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // 3. Xóa data trong DB
                _context.Sliders.Remove(slider);

                // 4. THUẬT TOÁN DỒN HÀNG (LẤP KHOẢNG TRỐNG)
                var slidesToShift = await _context.Sliders
                    .Where(s => s.DisplayOrder > deletedOrder && s.SliderId != slider.SliderId)
                    .ToListAsync();

                if (slidesToShift.Any())
                {
                    foreach (var item in slidesToShift)
                    {
                        item.DisplayOrder -= 1;
                    }
                    _context.Sliders.UpdateRange(slidesToShift);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa Slide và tự động dồn lại thứ tự thành công!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Lỗi: Không thể xóa Slide này!";
            }

            return RedirectToAction(nameof(Index));
        }

        // 7. ĐỔI CHỖ 2 SLIDE CHO NHAU
        [HttpPost]
        public async Task<IActionResult> SwapOrder(int currentId, int targetOrder)
        {
            var slider1 = await _context.Sliders.FindAsync(currentId);
            var slider2 = await _context.Sliders.FirstOrDefaultAsync(s => s.DisplayOrder == targetOrder);

            if (slider1 != null && slider2 != null && slider1.SliderId != slider2.SliderId)
            {
                // Tráo đổi giá trị DisplayOrder của 2 slide cho nhau
                int tempOrder = slider1.DisplayOrder;
                slider1.DisplayOrder = slider2.DisplayOrder;
                slider2.DisplayOrder = tempOrder;

                // Cập nhật người sửa
                slider1.UpdatedBy = User.Identity?.Name ?? "Admin";
                slider1.UpdatedAt = DateTime.Now;
                slider2.UpdatedBy = User.Identity?.Name ?? "Admin";
                slider2.UpdatedAt = DateTime.Now;

                _context.Update(slider1);
                _context.Update(slider2);

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã hoán đổi vị trí thành công!";
            }
            else if (slider2 == null)
            {
                TempData["ErrorMessage"] = $"Lỗi: Không tìm thấy Slide nào đang ở vị trí số {targetOrder}.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}