using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DoAnTotNghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;
        public CategoryController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            var query = _context.Categories.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(c => c.CategoryName.Contains(searchString));
            }
            int pageSize = 5; 
            int totalItems = await query.CountAsync(); 
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;
            var categories = await query.OrderByDescending(c => c.CategoryId)
                                        .Skip((page - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync();
            ViewBag.SearchString = searchString;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View(categories);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                category.CreatedDate = DateTime.Now;
                category.CreatedBy = User.Identity?.Name ?? "Hệ thống";
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index)); 
            }
            return View(category); 
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category); 
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.CategoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var oldCategory = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == id);
                    if (oldCategory != null)
                    {
                        category.CreatedBy = oldCategory.CreatedBy;
                        category.CreatedDate = oldCategory.CreatedDate;
                    }
                    category.UpdatedBy = User.Identity.Name; 
                    category.UpdatedDate = DateTime.Now;     
                    _context.Update(category); 
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index)); 
            }
            return View(category);
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FirstOrDefaultAsync(m => m.CategoryId == id);
            if (category == null) return NotFound();

            return View(category);
        }
        [HttpPost, ActionName("Delete")] 
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category); 
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }
    }
}
