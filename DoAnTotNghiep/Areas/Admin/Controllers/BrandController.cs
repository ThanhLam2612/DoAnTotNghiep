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

        public async Task<IActionResult> Index()
        {
            return View(await _context.Brands.ToListAsync());
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand brand, IFormFile logoImage)
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
                                System.IO.File.Delete(oldFilePath);
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
            var brand = await _context.Brands.FindAsync(id);
            if (brand != null)
            {
                if (!string.IsNullOrEmpty(brand.LogoUrl))
                {
                    string filePath = Path.Combine(_webHostEnvironment.WebRootPath, brand.LogoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.BrandId == id);
        }
    }
}
