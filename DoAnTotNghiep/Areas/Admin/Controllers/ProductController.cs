using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Drawing2D;

namespace DoAnTotNghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index(string searchString, int? categoryId,int? brandId, int page = 1)
        {
            var query = _context.Products
                                .Include(p => p.Category)
                                .Include(p => p.Brand)
                                .AsQueryable();
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }
            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(p => p.ProductName.Contains(searchString));
            }
            int pageSize = 5; 
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var products = await query.OrderByDescending(p => p.ProductId)
                                      .Skip((page - 1) * pageSize)
                                      .Take(pageSize)
                                      .ToListAsync();
            ViewBag.SearchString = searchString;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", categoryId);
            ViewData["BrandId"] = new SelectList(_context.Brands, "BrandId", "BrandName", brandId);
            return View(products);
        }
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            ViewBag.BrandId = new SelectList(_context.Brands, "BrandId", "BrandName");

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            ModelState.Remove("Category");
            ModelState.Remove("Brand");
            ModelState.Remove("PromotionProducts");
            ModelState.Remove("ProductVariants");
            ModelState.Remove("Reviews");
            if (ModelState.IsValid)
            {
                if (imageFile != null)
                {

                    string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/products");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    product.ThumbnailUrl = "/images/products/" + uniqueFileName;
                }
                product.CreatedDate = DateTime.Now;
                product.CreatedBy = User.Identity?.Name;
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
            ViewBag.BrandId = new SelectList(_context.Brands, "BrandId", "BrandName", product.BrandId);

            return View(product);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
            ViewBag.BrandId = new SelectList(_context.Brands, "BrandId", "BrandName", product.BrandId);
            return View(product);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile)
        {
            if (id != product.ProductId) return NotFound();
            ModelState.Remove("Category");
            ModelState.Remove("Brand");
            ModelState.Remove("PromotionProducts");
            if (ModelState.IsValid)
            {
                try
                {
                    var oldProducts = await _context.Products.AsNoTracking().FirstOrDefaultAsync(n => n.ProductId == id);
                    if (oldProducts != null)
                    {
                        product.CreatedBy = oldProducts.CreatedBy;
                        product.CreatedDate = oldProducts.CreatedDate;
                    }
                    product.UpdatedBy = User.Identity?.Name;
                    product.UpdatedDate = DateTime.Now;
                    if (imageFile != null)
                    {
                        string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/products");
                        if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        string filePath = Path.Combine(uploadFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }
                        if (!string.IsNullOrEmpty(product.ThumbnailUrl))
                        {
                            string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ThumbnailUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                        product.ThumbnailUrl = "/images/products/" + uniqueFileName;
                    }
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CategoryId = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
            ViewBag.BrandId = new SelectList(_context.Brands, "BrandId", "BrandName", product.BrandId);
            return View(product);
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null) return NotFound();
            return View(product);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                if (!string.IsNullOrEmpty(product.ThumbnailUrl))
                {
                    string filePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ThumbnailUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
