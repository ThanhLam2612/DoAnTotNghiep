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
        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            var query = _context.News.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(n => n.Title.Contains(searchString));
            }
            int pageSize = 5; 
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var newsList = await query.OrderByDescending(n => n.CreatedDate)
                                      .Skip((page - 1) * pageSize)
                                      .Take(pageSize)
                                      .ToListAsync();
            ViewBag.SearchString = searchString;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View(newsList);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
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
