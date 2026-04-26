using DoAnTotNghiep.Models;
using DoAnTotNghiep.Helpers; // Dùng để gọi thư viện VnPayLibrary
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnTotNghiep.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config; // Bổ sung IConfiguration để đọc appsettings.json

        public CartController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ======================= HÀM HỖ TRỢ LẤY THÔNG TIN =======================
        private async Task<int> GetCurrentUserId()
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            return user!.UserId;
        }

        // HÀM LẤY GIỎ HÀNG VÀ BƠM THÊM GIÁ, TÊN, ẢNH TỪ DATABASE SẢN PHẨM
        private async Task<List<CartItem>> GetPopulatedCartAsync()
        {
            int userId = await GetCurrentUserId();

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.AttributeValues)
                        .ThenInclude(av => av.PredefinedAttributeValue)
                            .ThenInclude(p => p.ProductAttribute)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var now = DateTime.Now;

            foreach (var item in cartItems)
            {
                item.ProductName = item.Product?.ProductName ?? "";
                item.ThumbnailUrl = item.Product?.ThumbnailUrl ?? "";
                item.Price = 0;

                if (item.VariantId.HasValue && item.ProductVariant != null)
                {
                    item.Price = item.ProductVariant.Price;

                    var attributes = item.ProductVariant.AttributeValues
                        .Where(a => a.PredefinedAttributeValue != null && a.PredefinedAttributeValue.ProductAttribute != null)
                        .Select(a => $"{a.PredefinedAttributeValue.ProductAttribute.AttributeName}: {a.PredefinedAttributeValue.Value}");
                    item.VariantName = string.Join(" | ", attributes);

                    // Lấy Khuyến mãi
                    var activePromo = await _context.PromotionVariants
                        .Include(pv => pv.Promotion)
                        .Where(pv => pv.VariantId == item.VariantId.Value
                                  && pv.Promotion.IsActive == true
                                  && pv.Promotion.StartDate <= now
                                  && pv.Promotion.EndDate >= now)
                        .OrderByDescending(pv => pv.DiscountPercent)
                        .FirstOrDefaultAsync();

                    if (activePromo != null && item.Price > 0)
                    {
                        item.Price = item.Price - (item.Price * activePromo.DiscountPercent / 100m);
                    }

                    // Lấy hình ảnh
                    if (!string.IsNullOrEmpty(item.ProductVariant.ImageUrl))
                    {
                        item.ThumbnailUrl = item.ProductVariant.ImageUrl;
                    }
                    else
                    {
                        var colorAttr = item.ProductVariant.AttributeValues.FirstOrDefault(a =>
                            a.PredefinedAttributeValue != null &&
                            a.PredefinedAttributeValue.ProductAttribute != null &&
                            (a.PredefinedAttributeValue.ProductAttribute.AttributeName.ToLower().Contains("màu") ||
                             a.PredefinedAttributeValue.ProductAttribute.AttributeName.ToLower().Contains("color")));

                        if (colorAttr != null)
                        {
                            var sibling = await _context.ProductVariants
                                .Include(v => v.AttributeValues)
                                .FirstOrDefaultAsync(v => v.ProductId == item.ProductId
                                                       && v.VariantId != item.VariantId.Value
                                                       && !string.IsNullOrEmpty(v.ImageUrl)
                                                       && v.AttributeValues.Any(a => a.PredefinedValueId == colorAttr.PredefinedValueId));

                            if (sibling != null) item.ThumbnailUrl = sibling.ImageUrl;
                        }
                    }
                }
            }
            return cartItems;
        }

        // ======================= CÁC CHỨC NĂNG CHÍNH =======================
        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem giỏ hàng của bạn!";
                return RedirectToAction("Login", "Account");
            }
            var cart = await GetPopulatedCartAsync();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int? variantId, int quantity = 1)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng!";
                return RedirectToAction("Login", "Account");
            }

            int userId = await GetCurrentUserId();
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null) return NotFound();

            int maxStock = 999;
            if (variantId.HasValue)
            {
                var variant = await _context.ProductVariants.FindAsync(variantId.Value);
                if (variant != null) maxStock = variant.StockQuantity;
            }

            var existingItem = await _context.CartItems.FirstOrDefaultAsync(c =>
                c.ProductId == productId && c.VariantId == variantId && c.UserId == userId);

            int currentInCart = existingItem != null ? existingItem.Quantity : 0;
            if (currentInCart + quantity > maxStock)
            {
                TempData["ErrorMessage"] = $"Bạn đã có {currentInCart} sản phẩm này trong giỏ. Không thể thêm {quantity} cái vì vượt quá tồn kho ({maxStock} cái).";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                _context.CartItems.Update(existingItem);
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    VariantId = variantId,
                    Quantity = quantity,
                    CreatedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> BuyNow(int productId, int? variantId, int quantity = 1)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để mua hàng!";
                return RedirectToAction("Login", "Account");
            }
            await AddToCart(productId, variantId, quantity);
            return RedirectToAction("Checkout");
        }

        public async Task<IActionResult> RemoveFromCart(int productId, int? variantId)
        {
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Login", "Account");

            int userId = await GetCurrentUserId();
            var item = await _context.CartItems.FirstOrDefaultAsync(c =>
                c.ProductId == productId && c.VariantId == variantId && c.UserId == userId);

            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int? variantId, int quantity)
        {
            if (!User.Identity.IsAuthenticated) return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            int userId = await GetCurrentUserId();
            var item = await _context.CartItems.FirstOrDefaultAsync(c =>
                c.ProductId == productId && c.VariantId == variantId && c.UserId == userId);

            if (item != null)
            {
                int maxStock = 999;
                if (variantId.HasValue)
                {
                    var variant = await _context.ProductVariants.FindAsync(variantId.Value);
                    if (variant != null) maxStock = variant.StockQuantity;
                }
                if (quantity > maxStock)
                {
                    return Json(new { success = false, message = $"Rất tiếc, sản phẩm này chỉ còn {maxStock} cái trong kho!" });
                }

                item.Quantity = quantity > 0 ? quantity : 1;
                await _context.SaveChangesAsync();

                var populatedCart = await GetPopulatedCartAsync();
                var updatedItem = populatedCart.FirstOrDefault(c => c.CartItemId == item.CartItemId);

                return Json(new
                {
                    success = true,
                    itemTotal = updatedItem?.TotalAmount.ToString("#,##0") + " đ",
                    cartTotal = populatedCart.Sum(x => x.TotalAmount).ToString("#,##0") + " đ"
                });
            }
            return Json(new { success = false });
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tiến hành thanh toán!";
                return RedirectToAction("Login", "Account");
            }

            var cart = await GetPopulatedCartAsync();
            if (cart.Count == 0) return RedirectToAction("Index", "Cart");

            foreach (var item in cart)
            {
                int currentStock = 999;
                if (item.VariantId.HasValue)
                {
                    var variant = await _context.ProductVariants.FindAsync(item.VariantId.Value);
                    if (variant != null) currentStock = variant.StockQuantity;
                }

                if (item.Quantity > currentStock)
                {
                    TempData["ErrorMessage"] = $"Sản phẩm '{item.ProductName}' {item.VariantName} hiện chỉ còn {currentStock} cái. Vui lòng điều chỉnh lại số lượng!";
                    return RedirectToAction("Index", "Cart");
                }
            }

            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user != null)
            {
                ViewBag.FullName = user.FullName;
                ViewBag.Phone = user.Phone;
            }
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // BỔ SUNG: Nhận thêm tham số paymentMethod từ form Checkout
        public async Task<IActionResult> Checkout(Order order, string paymentMethod = "COD")
        {
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Login", "Account");

            var cart = await GetPopulatedCartAsync();
            if (cart.Count == 0) return RedirectToAction("Index", "Cart");

            bool hasError = false;
            foreach (var item in cart)
            {
                int currentStock = 999;
                if (item.VariantId.HasValue)
                {
                    var variant = await _context.ProductVariants.FindAsync(item.VariantId.Value);
                    if (variant != null) currentStock = variant.StockQuantity;
                }

                if (item.Quantity > currentStock)
                {
                    ModelState.AddModelError("", $"Rất tiếc, '{item.ProductName}' {item.VariantName} vừa bị người khác mua mất, chỉ còn {currentStock} cái. Vui lòng quay lại giỏ hàng!");
                    hasError = true;
                }
            }

            if (hasError) return View(cart);

            if (ModelState.IsValid)
            {
                // 1. LƯU ĐƠN HÀNG VÀO DATABASE
                order.OrderDate = DateTime.Now;
                order.Status = 0; // 0 = Chờ xử lý / Chưa thanh toán
                order.TotalAmount = cart.Sum(x => x.TotalAmount);
                order.Username = User.Identity?.Name;
                order.PaymentMethod = paymentMethod;
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in cart)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        VariantId = item.VariantId,
                        Quantity = item.Quantity,
                        Price = item.Price
                    };
                    _context.OrderDetails.Add(orderDetail);

                    if (item.VariantId.HasValue)
                    {
                        var variant = await _context.ProductVariants.FindAsync(item.VariantId.Value);
                        if (variant != null)
                        {
                            variant.StockQuantity -= item.Quantity;
                            _context.Update(variant);
                        }
                    }
                }

                // 2. XÓA GIỎ HÀNG 
                int userId = await GetCurrentUserId();
                var itemsToDelete = await _context.CartItems.Where(c => c.UserId == userId).ToListAsync();
                _context.CartItems.RemoveRange(itemsToDelete);
                await _context.SaveChangesAsync();

                // ==========================================================
                // 3. XỬ LÝ THANH TOÁN VNPAY NẾU KHÁCH CHỌN
                // ==========================================================
                if (paymentMethod == "VNPAY")
                {
                    var vnpay = new VnPayLibrary();

                    vnpay.AddRequestData("vnp_Version", _config["Vnpay:Version"]);
                    vnpay.AddRequestData("vnp_Command", _config["Vnpay:Command"]);
                    vnpay.AddRequestData("vnp_TmnCode", _config["Vnpay:TmnCode"]);
                    // Số tiền phải nhân 100 theo chuẩn VNPAY
                    vnpay.AddRequestData("vnp_Amount", ((long)(order.TotalAmount * 100)).ToString());
                    vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                    vnpay.AddRequestData("vnp_CurrCode", _config["Vnpay:CurrCode"]);
                    vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(HttpContext));
                    vnpay.AddRequestData("vnp_Locale", _config["Vnpay:Locale"]);
                    vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang: " + order.OrderId);
                    vnpay.AddRequestData("vnp_OrderType", "other");
                    vnpay.AddRequestData("vnp_ReturnUrl", _config["Vnpay:ReturnUrl"]);
                    vnpay.AddRequestData("vnp_TxnRef", order.OrderId.ToString());

                    string paymentUrl = vnpay.CreateRequestUrl(_config["Vnpay:BaseUrl"], _config["Vnpay:HashSecret"]);

                    // Chuyển hướng sang trang thanh toán của VNPAY
                    return Redirect(paymentUrl);
                }

                // Nếu là COD thì chuyển thẳng sang trang Success
                return RedirectToAction("CheckoutSuccess", new { id = order.OrderId });
            }

            return View(cart);
        }

        // ==========================================================
        // 4. HÀM ĐÓN KHÁCH HÀNG TỪ VNPAY TRẢ VỀ
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> PaymentCallback()
        {
            if (Request.Query.Count > 0)
            {
                string vnp_HashSecret = _config["Vnpay:HashSecret"];
                var vnpayData = Request.Query;
                VnPayLibrary vnpay = new VnPayLibrary();

                // Lấy toàn bộ dữ liệu VNPAY trả về
                foreach (var s in vnpayData)
                {
                    if (!string.IsNullOrEmpty(s.Key) && s.Key.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(s.Key, s.Value);
                    }
                }

                string orderIdStr = vnpay.GetResponseData("vnp_TxnRef");
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_SecureHash = Request.Query["vnp_SecureHash"];

                // Xác thực chữ ký
                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                if (checkSignature)
                {
                    int orderId = int.Parse(orderIdStr);
                    var order = await _context.Orders.FindAsync(orderId);

                    if (vnp_ResponseCode == "00" && order != null)
                    {
                        // 00 là thanh toán thành công
                        order.Status = 1; // 1 = Đã thanh toán (Hoặc trạng thái tương ứng trong hệ thống của bạn)
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Thanh toán qua VNPAY thành công!";
                        return RedirectToAction("CheckoutSuccess", new { id = orderId });
                    }
                    else
                    {
                        // Lỗi thẻ hoặc khách bấm Hủy
                        TempData["ErrorMessage"] = "Giao dịch thanh toán bị hủy hoặc không thành công.";
                        return RedirectToAction("Index", "Cart");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Lỗi xác thực dữ liệu thanh toán (Sai chữ ký).";
                    return RedirectToAction("Index", "Cart");
                }
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult CheckoutSuccess(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }
    }
}