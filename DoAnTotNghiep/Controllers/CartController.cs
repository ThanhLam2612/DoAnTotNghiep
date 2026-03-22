using DoAnTotNghiep.Extension;
using DoAnTotNghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        public CartController(AppDbContext context) { _context = context; }
        public List<CartItem> Cart => HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
        public IActionResult Index()
        {
            return View(Cart);
        }
        public IActionResult AddToCart(int id, int quantity = 1)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == id);
            if (product == null) return NotFound();
            var cart = Cart; 
            var item = cart.FirstOrDefault(c => c.ProductId == id);
            if (item != null)
            {
                item.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Price = product.BasePrice,
                    ThumbnailUrl = product.ThumbnailUrl,
                    Quantity = quantity
                });
            }
            HttpContext.Session.SetJson("Cart", cart);
            return RedirectToAction("Index");
        }
        public IActionResult RemoveFromCart(int id)
        {
            var cart = Cart;
            var item = cart.FirstOrDefault(c => c.ProductId == id);
            if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.SetJson("Cart", cart);
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            var cart = Cart;
            var item = cart.FirstOrDefault(c => c.ProductId == id);
            if (item != null)
            {
                item.Quantity = quantity > 0 ? quantity : 1;
                HttpContext.Session.SetJson("Cart", cart);
                return Json(new
                {
                    success = true,
                    itemTotal = item.TotalAmount.ToString("#,##0") + " đ",
                    cartTotal = cart.Sum(x => x.TotalAmount).ToString("#,##0") + " đ"
                });
            }
            return Json(new { success = false });
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var cart = Cart;
            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart"); 
            }
            if (User.Identity.IsAuthenticated)
            {
                var username = User.Identity.Name;
                var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null)
                {
                    ViewBag.FullName = user.FullName;
                    ViewBag.Phone = user.Phone;
                }
            }
            return View(cart); 
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order)
        {
            var cart = Cart;
            if (cart.Count == 0) return RedirectToAction("Index", "Cart");
            if (ModelState.IsValid)
            {
                order.OrderDate = DateTime.Now;
                order.Status = 0; 
                order.TotalAmount = cart.Sum(x => x.TotalAmount); 
                order.Username = User.Identity?.Name;
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                foreach (var item in cart)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,       
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price             
                    };
                    _context.OrderDetails.Add(orderDetail);
                }
                await _context.SaveChangesAsync();
                HttpContext.Session.Remove("Cart");
                return RedirectToAction("CheckoutSuccess", new { id = order.OrderId });
            }
            return View(cart);
        }
        public IActionResult CheckoutSuccess(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }
        [HttpPost]
        public IActionResult BuyNow(int id, int quantity = 1)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == id);
            if (product == null) return NotFound();
            var cart = Cart;
            var item = cart.FirstOrDefault(c => c.ProductId == id);
            if (item != null)
            {
                item.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Price = product.BasePrice,
                    ThumbnailUrl = product.ThumbnailUrl,
                    Quantity = quantity
                });
            }
            HttpContext.Session.SetJson("Cart", cart);
            return RedirectToAction("Checkout");
        }
    }
}
