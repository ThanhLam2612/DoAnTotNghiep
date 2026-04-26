using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DoAnTotNghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class BrandController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public BrandController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            var query = _context.Brands.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(b => b.BrandName.Contains(searchString));
            }
            int pageSize = 5; 
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;
            var brands = await query.OrderByDescending(b => b.BrandId)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();
            ViewBag.SearchString = searchString;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View(brands);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand brand, IFormFile? logoImage)
        {
            if (ModelState.IsValid)
            {
                
                if (logoImage != null)
                {   
                    string folder = "images/brands/";
                    folder += Guid.NewGuid().ToString() + "_" + logoImage.FileName;
                    string serverFolder = Path.Combine(_webHostEnvironment.WebRootPath, folder);                    
                    await logoImage.CopyToAsync(new FileStream(serverFolder, FileMode.Create));
                    brand.LogoUrl = "/" + folder;
                }
                brand.CreatedDate = DateTime.Now;
                brand.CreatedBy = User.Identity?.Name;
                _context.Add(brand);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) return NotFound();
            return View(brand);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand brand, IFormFile? logoImage)
        {
            if (id != brand.BrandId) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    var oldBrand = await _context.Brands.AsNoTracking().FirstOrDefaultAsync(b => b.BrandId == id);
                    if (oldBrand != null)
                    {
                        brand.CreatedBy = oldBrand.CreatedBy;
                        brand.CreatedDate = oldBrand.CreatedDate;
                    }
                    brand.UpdatedBy = User.Identity?.Name;
                    brand.UpdatedDate = DateTime.Now;
                    
                    if (logoImage != null)
                    {
                        string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/brands");
                        if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + logoImage.FileName;
                        string filePath = Path.Combine(uploadFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await logoImage.CopyToAsync(fileStream);
                        }
                        if (!string.IsNullOrEmpty(brand.LogoUrl))
                        {
                            string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, brand.LogoUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                try
                                {
                                    System.IO.File.Delete(oldFilePath);
                                }
                                catch (IOException)
                                {
                                    
                                }
                            }
                        }
                        brand.LogoUrl = "/images/brands/" + uniqueFileName;
                    }
                    _context.Update(brand);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BrandExists(brand.BrandId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var brand = await _context.Brands.FirstOrDefaultAsync(m => m.BrandId == id);
            if (brand == null) return NotFound();

            return View(brand);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            bool hasProducts = await _context.Products.AnyAsync(p => p.BrandId == id);

            if (hasProducts)
            {
                TempData["ErrorMessage"] = "Không thể xóa! Thương hiệu này đang có sản phẩm.";
                return RedirectToAction(nameof(Index));
            }
            var brand = await _context.Brands.FindAsync(id);
            if (brand != null)
            {
                if (!string.IsNullOrEmpty(brand.LogoUrl))
                {
                    string filePath = Path.Combine(_webHostEnvironment.WebRootPath, brand.LogoUrl.TrimStart('/'));
                    try
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                    catch (IOException)
                    {
                        
                    }
                }
                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa thương hiệu thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.BrandId == id);
        }
    }
}
