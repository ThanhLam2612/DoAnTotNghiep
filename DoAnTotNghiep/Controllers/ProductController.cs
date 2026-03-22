using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Controllers
{

    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string? searchString, int? categoryId)
        {
            var products = _context.Products.Include(p => p.Category).AsQueryable();
            if (categoryId != null)
            {
                products = products.Where(p => p.CategoryId == categoryId);
                var category = await _context.Categories.FindAsync(categoryId);
                if (category != null)
                {
                    ViewBag.CategoryName = category.CategoryName;
                }
            }
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.ProductName.Contains(searchString));
                ViewBag.SearchKeyword = searchString;
            }
            var result = await products.OrderByDescending(p => p.CreatedDate).ToListAsync();
            return View(result);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }
    }
}
