using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Mvc;

namespace DoAnTotNghiep.Controllers
{
    public class ContactController : Controller
    {
        private readonly AppDbContext _context;
        public ContactController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitContact(string name, string phone, string email, string message)
        {
            var contact = new Contact
            {
                FullName = name,
                Phone = phone,
                Email = email,
                Message = message,
                CreatedDate = DateTime.Now,
                Status = 0 
            };
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi trong thời gian sớm nhất.";
            return RedirectToAction("Index");
        }
    }
}
