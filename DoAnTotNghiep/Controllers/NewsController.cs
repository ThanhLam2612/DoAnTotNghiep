using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Controllers
{
    public class NewsController : Controller
    {
        private readonly AppDbContext _context;
        public NewsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var posts = await _context.News
                                      .OrderByDescending(p => p.CreatedDate)
                                      .ToListAsync();
            return View(posts);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var post = await _context.News.FirstOrDefaultAsync(m => m.NewsId == id);

            if (post == null) return NotFound();

            return View(post);
        }
    }
}
