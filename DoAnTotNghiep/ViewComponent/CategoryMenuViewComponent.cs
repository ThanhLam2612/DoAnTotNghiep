using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.ViewComponents
{
    public class CategoryMenuViewComponent : Microsoft.AspNetCore.Mvc.ViewComponent
    {
        private readonly AppDbContext _context;

        public CategoryMenuViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }
    }
}
