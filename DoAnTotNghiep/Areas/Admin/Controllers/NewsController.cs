using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Drawing2D;
namespace DoAnTotNghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class NewsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public NewsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index()
        {
            // Sắp xếp bài mới nhất lên đầu
            var newsList = await _context.News.OrderByDescending(n => n.CreatedDate).ToListAsync();
            return View(newsList);
        }

        // 2. GIAO DIỆN THÊM MỚI
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // 3. XỬ LÝ LƯU TIN TỨC
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(News news, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null)
                {
                    string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/news");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    news.ImageUrl = "/images/news/" + uniqueFileName;
                }
                news.CreatedDate = DateTime.Now;
                news.CreatedBy = User.Identity?.Name;
                news.CreatedDate = DateTime.Now; 
                _context.Add(news);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(news);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();

            return View(news);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, News news, IFormFile? imageFile)
        {
            if (id != news.NewsId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var oldNew = await _context.News.AsNoTracking().FirstOrDefaultAsync(b => b.NewsId == id);
                    if (oldNew != null)
                    {
                        news.CreatedBy = oldNew.CreatedBy;
                        news.CreatedDate = oldNew.CreatedDate;
                    }
                    news.UpdatedBy = User.Identity?.Name;
                    news.UpdatedDate = DateTime.Now;
                    if (imageFile != null)
                    {
                        string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/news");
                        if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        string filePath = Path.Combine(uploadFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }
                        if (!string.IsNullOrEmpty(news.ImageUrl))
                        {
                            string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, news.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                        news.ImageUrl = "/images/news/" + uniqueFileName;
                    }
                    _context.Update(news);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NewsExists(news.NewsId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(news);
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var news = await _context.News.FirstOrDefaultAsync(m => m.NewsId == id);
            if (news == null) return NotFound();

            return View(news);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news != null)
            {
                if (!string.IsNullOrEmpty(news.ImageUrl))
                {
                    string filePath = Path.Combine(_webHostEnvironment.WebRootPath, news.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                _context.News.Remove(news);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        private bool NewsExists(int id)
        {
            return _context.News.Any(e => e.NewsId == id);
        }
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile upload)
        {
            if (upload != null && upload.Length > 0)
            {
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/news");
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                string fileName = Guid.NewGuid().ToString() + "_" + upload.FileName;
                string filePath = Path.Combine(uploadFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await upload.CopyToAsync(stream);
                }
                string url = "/images/news/" + fileName;
                return Json(new { uploaded = true, url = url });
            }
            return Json(new { uploaded = false, error = new { message = "Không thể tải lên hình ảnh" } });
        }
    }
}
