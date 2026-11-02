using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopProject.Data;
using ShopProject.Models;
using ShopProject.Services; // <-- ДОБАВЛЕНО
using System.Security.Claims;
using System.Text.Json;

namespace ShopProject.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStockService _stockService; // <-- ДОБАВЛЕНО

        public CartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IStockService stockService) // <-- ДОБАВЛЕН ПАРАМЕТР
        {
            _context = context;
            _userManager = userManager;
            _stockService = stockService;
        }

        // ===== ПОЛУЧЕНИЕ ГОСТЕВОЙ КОРЗИНЫ ИЗ СЕССИИ =====
        private List<GuestCartItem> GetGuestCart()
        {
            var cartJson = HttpContext.Session.GetString("GuestCart");
            if (string.IsNullOrEmpty(cartJson))
                return new List<GuestCartItem>();

            return JsonSerializer.Deserialize<List<GuestCartItem>>(cartJson) ?? new List<GuestCartItem>();
        }

        // ===== СОХРАНЕНИЕ ГОСТЕВОЙ КОРЗИНЫ В СЕССИЮ =====
        private void SaveGuestCart(List<GuestCartItem> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("GuestCart", cartJson);
            HttpContext.Session.SetInt32("CartCount", cart.Sum(c => c.Quantity));
        }

        // ===== ПЕРЕНОС ГОСТЕВОЙ КОРЗИНЫ В БД =====
        // ===== ПЕРЕНОС ГОСТЕВОЙ КОРЗИНЫ В БД С ПРОВЕРКОЙ ОСТАТКОВ =====
        private async Task MergeGuestCartToUser(string userId)
        {
            var guestCart = GetGuestCart();
            if (!guestCart.Any()) return;

            var userCart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (userCart == null)
            {
                userCart = new Cart { UserId = userId };
                _context.Carts.Add(userCart);
                await _context.SaveChangesAsync();
            }

            // Список товаров, которые не удалось перенести из-за нехватки остатков
            var errors = new List<string>();

            foreach (var guestItem in guestCart)
            {
                var size = guestItem.Size ?? "OneSize";

                // Проверяем остатки для этого товара
                var stockAvailable = await _stockService.IsStockAvailableAsync(
                    guestItem.ProductId,
                    size,
                    guestItem.Quantity
                );

                if (!stockAvailable)
                {
                    var currentStock = await _stockService.GetStockQuantityAsync(guestItem.ProductId, size);
                    var product = await _context.Products.FindAsync(guestItem.ProductId);
                    var productName = product?.Name ?? "Товар #" + guestItem.ProductId;

                    errors.Add($"{productName} (размер {size}) — в наличии {currentStock} шт., вы пытаетесь добавить {guestItem.Quantity} шт.");
                    continue; // Пропускаем этот товар
                }

                // Если остатков достаточно — переносим
                var existingItem = userCart.CartItems
                    .FirstOrDefault(ci => ci.ProductId == guestItem.ProductId && ci.Size == size);

                if (existingItem == null)
                {
                    userCart.CartItems.Add(new CartItem
                    {
                        ProductId = guestItem.ProductId,
                        Quantity = guestItem.Quantity,
                        Size = size,
                        CartId = userCart.Id
                    });
                }
                else
                {
                    // Проверяем, не превысит ли общее количество остатки
                    var totalQuantity = existingItem.Quantity + guestItem.Quantity;
                    var totalStockAvailable = await _stockService.IsStockAvailableAsync(
                        guestItem.ProductId,
                        size,
                        totalQuantity
                    );

                    if (!totalStockAvailable)
                    {
                        var currentStock = await _stockService.GetStockQuantityAsync(guestItem.ProductId, size);
                        var product = await _context.Products.FindAsync(guestItem.ProductId);
                        var productName = product?.Name ?? "Товар #" + guestItem.ProductId;

                        errors.Add($"{productName} (размер {size}) — в наличии {currentStock} шт., у вас уже {existingItem.Quantity} шт., пытаетесь добавить ещё {guestItem.Quantity} шт.");

                        // Добавляем только то количество, которое помещается
                        var maxCanAdd = Math.Max(0, currentStock - existingItem.Quantity);
                        if (maxCanAdd > 0)
                        {
                            existingItem.Quantity += maxCanAdd;
                            errors.Add($"⚠️ {productName} (размер {size}) — добавлено только {maxCanAdd} шт. из {guestItem.Quantity} (ограничение остатков)");
                        }
                        continue;
                    }

                    existingItem.Quantity += guestItem.Quantity;
                }
            }

            await _context.SaveChangesAsync();

            // Очищаем гостевую корзину
            HttpContext.Session.Remove("GuestCart");
            HttpContext.Session.SetInt32("CartCount", userCart.CartItems.Sum(c => c.Quantity));

            // Если были ошибки — сохраняем их в TempData для отображения
            if (errors.Any())
            {
                TempData["Warning"] = string.Join("<br/>", errors);
                TempData["WarningTitle"] = "Не все товары из гостевой корзины были перенесены:";
            }
        }

        // ===== GET: /Cart/Index =====
        public async Task<IActionResult> Index()
        {
            // ===== АВТОРИЗОВАННЫЙ ПОЛЬЗОВАТЕЛЬ =====
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Index", "Home");

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.Images)
                    .Include(c => c.CartItems)                     // ← ДОБАВЛЕНО
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.Sizes)              // ← ДОБАВЛЕНО
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart { UserId = userId };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                // Проверяем доступность (необязательно, можно в представлении)
                bool hasUnavailable = false;
                foreach (var item in cart.CartItems)
                {
                    var product = item.Product;
                    if (!product.IsActive) { hasUnavailable = true; continue; }
                    var size = product.Sizes?.FirstOrDefault(s => s.Size == item.Size);
                    if (size == null || !size.IsAvailable || size.StockQuantity <= 0)
                        hasUnavailable = true;
                }
                ViewBag.HasUnavailableItems = hasUnavailable;

                if (!cart.CartItems.Any())
                {
                    HttpContext.Session.Remove("PromoCode");
                    HttpContext.Session.Remove("PromoDiscount");
                    TempData.Remove("PromoSuccess");
                    TempData.Remove("PromoError");
                }

                HttpContext.Session.SetInt32("CartCount", cart.CartItems.Sum(c => c.Quantity));
                return View(cart);
            }

            // ===== ГОСТЕВАЯ КОРЗИНА =====
            var guestCart = GetGuestCart();
            var guestCartItems = new List<CartItem>();

            foreach (var item in guestCart)
            {
                var product = await _context.Products
                    .Include(p => p.Images)
                    .Include(p => p.Sizes)   // ← ДОБАВЛЕНО
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (product != null)
                {
                    guestCartItems.Add(new CartItem
                    {
                        ProductId = product.Id,
                        Product = product,
                        Quantity = item.Quantity,
                        Size = item.Size ?? "OneSize"
                    });
                }
            }

            // Проверяем доступность для гостя
            bool guestHasUnavailable = false;
            foreach (var item in guestCartItems)
            {
                var product = item.Product;
                if (product == null) continue;
                if (!product.IsActive) { guestHasUnavailable = true; continue; }
                var size = product.Sizes?.FirstOrDefault(s => s.Size == item.Size);
                if (size == null || !size.IsAvailable || size.StockQuantity <= 0)
                    guestHasUnavailable = true;
            }
            ViewBag.HasUnavailableItems = guestHasUnavailable;

            if (!guestCartItems.Any())
            {
                HttpContext.Session.Remove("PromoCode");
                HttpContext.Session.Remove("PromoDiscount");
                TempData.Remove("PromoSuccess");
                TempData.Remove("PromoError");
            }

            var cartModel = new Cart { CartItems = guestCartItems };
            HttpContext.Session.SetInt32("CartCount", guestCartItems.Sum(c => c.Quantity));

            return View(cartModel);
        }


        // ===== POST: /Cart/Add =====
        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var product = await _context.Products
                .Include(p => p.Sizes)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                TempData["Error"] = "Товар не найден.";
                return RedirectToAction(nameof(Index));
            }

            if (!product.IsActive)
            {
                TempData["Error"] = "Этот товар временно недоступен.";
                return RedirectToAction(nameof(Index));
            }

            var size = Request.Form["size"].ToString();
            if (string.IsNullOrEmpty(size))
            {
                size = "OneSize";
            }

            // ===== ПРОВЕРКА ОСТАТКОВ (НОВАЯ ЛОГИКА) =====
            var stockAvailable = await _stockService.IsStockAvailableAsync(productId, size, quantity);
            if (!stockAvailable)
            {
                var currentStock = await _stockService.GetStockQuantityAsync(productId, size);
                TempData["Error"] = $"Недостаточно товара. В наличии: {currentStock} шт.";
                return RedirectToAction(nameof(Index));
            }

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart { UserId = userId };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId && ci.Size == size);
                if (cartItem == null)
                {
                    cartItem = new CartItem
                    {
                        ProductId = productId,
                        Quantity = quantity,
                        Size = size,
                        CartId = cart.Id
                    };
                    _context.CartItems.Add(cartItem);
                }
                else
                {
                    // Проверяем, не превысит ли общее количество остаток
                    var totalQuantity = cartItem.Quantity + quantity;
                    var totalStockAvailable = await _stockService.IsStockAvailableAsync(productId, size, totalQuantity);
                    if (!totalStockAvailable)
                    {
                        var currentStock = await _stockService.GetStockQuantityAsync(productId, size);
                        TempData["Error"] = $"Нельзя добавить больше товара. В наличии: {currentStock} шт. У вас уже {cartItem.Quantity} шт.";
                        return RedirectToAction(nameof(Index));
                    }
                    cartItem.Quantity += quantity;
                }

                await _context.SaveChangesAsync();
                HttpContext.Session.SetInt32("CartCount", cart.CartItems.Sum(c => c.Quantity));

                TempData["Success"] = "Товар добавлен в корзину!";
                return RedirectToAction(nameof(Index));
            }

            // Гостевая корзина
            var guestCart = GetGuestCart();
            var existingItem = guestCart.FirstOrDefault(ci => ci.ProductId == productId && ci.Size == size);

            if (existingItem == null)
            {
                guestCart.Add(new GuestCartItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    Size = size
                });
            }
            else
            {
                // Проверяем, не превысит ли общее количество остаток
                var totalQuantity = existingItem.Quantity + quantity;
                var totalStockAvailable = await _stockService.IsStockAvailableAsync(productId, size, totalQuantity);
                if (!totalStockAvailable)
                {
                    var currentStock = await _stockService.GetStockQuantityAsync(productId, size);
                    TempData["Error"] = $"Нельзя добавить больше товара. В наличии: {currentStock} шт. У вас уже {existingItem.Quantity} шт.";
                    return RedirectToAction(nameof(Index));
                }
                existingItem.Quantity += quantity;
            }

            SaveGuestCart(guestCart);
            TempData["Success"] = "Товар добавлен в корзину! (войдите, чтобы оформить заказ)";
            return RedirectToAction(nameof(Index));
        }

        // ===== POST: /Cart/Update =====
        [HttpPost]
        public async Task<IActionResult> Update(int cartItemId, int quantity)
        {
            if (User.Identity.IsAuthenticated)
            {
                var cartItem = await _context.CartItems
                    .Include(ci => ci.Product)
                    .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

                if (cartItem != null)
                {
                    if (quantity <= 0)
                    {
                        _context.CartItems.Remove(cartItem);
                    }
                    else
                    {
                        // Проверяем остатки при обновлении количества
                        var size = cartItem.Size ?? "OneSize";
                        var stockAvailable = await _stockService.IsStockAvailableAsync(cartItem.ProductId, size, quantity);
                        if (!stockAvailable)
                        {
                            var currentStock = await _stockService.GetStockQuantityAsync(cartItem.ProductId, size);
                            TempData["Error"] = $"Недостаточно товара. В наличии: {currentStock} шт.";
                            return RedirectToAction(nameof(Index));
                        }
                        cartItem.Quantity = quantity;
                    }

                    await _context.SaveChangesAsync();

                    var cart = await _context.Carts
                        .Include(c => c.CartItems)
                        .FirstOrDefaultAsync(c => c.Id == cartItem.CartId);
                    if (cart != null)
                        HttpContext.Session.SetInt32("CartCount", cart.CartItems.Sum(c => c.Quantity));
                }
                return RedirectToAction(nameof(Index));
            }

            var guestCart = GetGuestCart();
            var item = guestCart.FirstOrDefault(ci => ci.ProductId == cartItemId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    guestCart.Remove(item);
                }
                else
                {
                    // Для гостевой корзины проверяем остатки
                    var stockAvailable = await _stockService.IsStockAvailableAsync(item.ProductId, item.Size ?? "OneSize", quantity);
                    if (!stockAvailable)
                    {
                        var currentStock = await _stockService.GetStockQuantityAsync(item.ProductId, item.Size ?? "OneSize");
                        TempData["Error"] = $"Недостаточно товара. В наличии: {currentStock} шт.";
                        return RedirectToAction(nameof(Index));
                    }
                    item.Quantity = quantity;
                }

                SaveGuestCart(guestCart);
            }
            return RedirectToAction(nameof(Index));
        }

        // ===== POST: /Cart/Remove =====
        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            if (User.Identity.IsAuthenticated)
            {
                var cartItem = await _context.CartItems.FindAsync(cartItemId);
                if (cartItem != null)
                {
                    var cartId = cartItem.CartId;
                    _context.CartItems.Remove(cartItem);
                    await _context.SaveChangesAsync();

                    var cart = await _context.Carts
                        .Include(c => c.CartItems)
                        .FirstOrDefaultAsync(c => c.Id == cartId);
                    if (cart != null)
                        HttpContext.Session.SetInt32("CartCount", cart.CartItems.Sum(c => c.Quantity));
                    else
                        HttpContext.Session.SetInt32("CartCount", 0);
                }
                return RedirectToAction(nameof(Index));
            }

            var guestCart = GetGuestCart();
            var item = guestCart.FirstOrDefault(ci => ci.ProductId == cartItemId);
            if (item != null)
            {
                guestCart.Remove(item);
                SaveGuestCart(guestCart);
            }
            return RedirectToAction(nameof(Index));
        }

        // ===== POST: /Cart/Increase =====
        [HttpPost]
        public async Task<IActionResult> Increase(int productId, string size = "OneSize")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // ===== ГОСТЬ =====
            if (string.IsNullOrEmpty(userId))
            {
                var cart = GetGuestCart();
                var item = cart.FirstOrDefault(i => i.ProductId == productId && i.Size == size);
                if (item == null)
                    return Json(new { success = false, message = "Товар не найден в корзине" });

                var incNewQty = item.Quantity + 1;
                var incStockOk = await _stockService.IsStockAvailableAsync(productId, size, incNewQty);
                if (!incStockOk)
                {
                    var incCurrentStock = await _stockService.GetStockQuantityAsync(productId, size);
                    return Json(new { success = false, message = $"Недостаточно товара. В наличии: {incCurrentStock} шт." });
                }

                item.Quantity = incNewQty;
                SaveGuestCart(cart);

                var incTotal = 0m;
                var incProduct = await _context.Products.FindAsync(productId); // ← ОДИН РАЗ!

                foreach (var ci in cart)
                {
                    var incProductInCart = await _context.Products.FindAsync(ci.ProductId);
                    if (incProductInCart != null)
                        incTotal += incProductInCart.Price * ci.Quantity;
                }

                var incItemTotal = incProduct != null ? incProduct.Price * item.Quantity : 0;

                return Json(new
                {
                    success = true,
                    itemTotal = incItemTotal,
                    total = incTotal,
                    count = cart.Sum(i => i.Quantity),
                    newQuantity = item.Quantity
                });
            }

            // ===== АВТОРИЗОВАННЫЙ =====
            var cartFromDb = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cartFromDb == null)
                return Json(new { success = false, message = "Корзина не найдена" });

            var cartItem = cartFromDb.CartItems
                .FirstOrDefault(i => i.ProductId == productId && i.Size == size);

            if (cartItem == null)
                return Json(new { success = false, message = "Товар не найден в корзине" });

            if (!cartItem.Product.IsActive)
                return Json(new { success = false, message = "Этот товар временно недоступен." });

            var incNewQtyDb = cartItem.Quantity + 1;
            var incStockOkDb = await _stockService.IsStockAvailableAsync(productId, size, incNewQtyDb);
            if (!incStockOkDb)
            {
                var incCurrentStockDb = await _stockService.GetStockQuantityAsync(productId, size);
                return Json(new { success = false, message = $"Недостаточно товара. В наличии: {incCurrentStockDb} шт." });
            }

            cartItem.Quantity = incNewQtyDb;
            await _context.SaveChangesAsync();

            var incTotalDb = cartFromDb.CartItems.Sum(i => i.Product.Price * i.Quantity);
            var incCountDb = cartFromDb.CartItems.Sum(i => i.Quantity);
            var incItemTotalDb = cartItem.Product.Price * cartItem.Quantity;

            HttpContext.Session.SetInt32("CartCount", incCountDb);


            //
            var promoCode = HttpContext.Session.GetString("PromoCode");
            var discountPercent = HttpContext.Session.GetInt32("PromoDiscount") ?? 0;

            var finalTotal = incTotalDb;
            var discountAmount = 0m;

            if (!string.IsNullOrEmpty(promoCode) && discountPercent > 0)
            {
                discountAmount = incTotalDb * discountPercent / 100;
                finalTotal = incTotalDb - discountAmount;
            }

            return Json(new
            {
                success = true,
                itemTotal = incItemTotalDb,
                total = incTotalDb,
                finalTotal = finalTotal,
                discountAmount = discountAmount,
                discountPercent = discountPercent,
                count = incCountDb,
                newQuantity = cartItem.Quantity
            });
          
        }

        // ===== POST: /Cart/Decrease =====
        [HttpPost]
        public async Task<IActionResult> Decrease(int productId, string size = "OneSize")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // ===== ГОСТЬ =====
            if (string.IsNullOrEmpty(userId))
            {
                var cart = GetGuestCart();
                var item = cart.FirstOrDefault(i => i.ProductId == productId && i.Size == size);
                if (item == null)
                    return Json(new { success = false, message = "Товар не найден в корзине" });

                bool decRemoved = false;
                if (item.Quantity <= 1)
                {
                    cart.Remove(item);
                    decRemoved = true;
                }
                else
                {
                    item.Quantity--;
                }

                SaveGuestCart(cart);

                var decTotal = 0m;
                var decProduct = await _context.Products.FindAsync(productId); // ← ОДИН РАЗ!

                foreach (var ci in cart)
                {
                    var decProductInCart = await _context.Products.FindAsync(ci.ProductId);
                    if (decProductInCart != null)
                        decTotal += decProductInCart.Price * ci.Quantity;
                }

                var decItemTotal = decProduct != null && !decRemoved ? decProduct.Price * item.Quantity : 0;

                return Json(new
                {
                    success = true,
                    itemTotal = decItemTotal,
                    total = decTotal,
                    count = cart.Sum(i => i.Quantity),
                    newQuantity = !decRemoved ? item.Quantity : 0,
                    removed = decRemoved
                });
            }

            // ===== АВТОРИЗОВАННЫЙ =====
            var cartFromDb = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cartFromDb == null)
                return Json(new { success = false, message = "Корзина не найдена" });

            var cartItem = cartFromDb.CartItems
                .FirstOrDefault(i => i.ProductId == productId && i.Size == size);

            if (cartItem == null)
                return Json(new { success = false, message = "Товар не найден в корзине" });

            if (!cartItem.Product.IsActive)
                return Json(new { success = false, message = "Этот товар временно недоступен." });

            bool decRemovedDb = false;
            if (cartItem.Quantity <= 1)
            {
                _context.CartItems.Remove(cartItem);
                decRemovedDb = true;
            }
            else
            {
                cartItem.Quantity--;
            }

            await _context.SaveChangesAsync();

            var decTotalDb = cartFromDb.CartItems.Sum(i => i.Product.Price * i.Quantity);
            var decCountDb = cartFromDb.CartItems.Sum(i => i.Quantity);
            var decItemTotalDb = !decRemovedDb ? cartItem.Product.Price * cartItem.Quantity : 0;

            HttpContext.Session.SetInt32("CartCount", decCountDb);

            // ===== В КОНЦЕ МЕТОДА Decrease (перед return) =====
            var promoCode = HttpContext.Session.GetString("PromoCode");
            var discountPercent = HttpContext.Session.GetInt32("PromoDiscount") ?? 0;

            var finalTotal = decTotalDb;
            var discountAmount = 0m;

            if (!string.IsNullOrEmpty(promoCode) && discountPercent > 0)
            {
                discountAmount = decTotalDb * discountPercent / 100;
                finalTotal = decTotalDb - discountAmount;
            }

            return Json(new
            {
                success = true,
                itemTotal = decItemTotalDb,
                total = decTotalDb,
                finalTotal = finalTotal,
                discountAmount = discountAmount,
                discountPercent = discountPercent,
                count = decCountDb,
                newQuantity = !decRemovedDb ? cartItem.Quantity : 0,
                removed = decRemovedDb
            });
        }

        // ===== POST: /Cart/ApplyPromoCode (AJAX) =====
        [HttpPost]
        public async Task<IActionResult> ApplyPromoCode(string promoCode)
        {
            if (string.IsNullOrWhiteSpace(promoCode))
            {
                return Json(new { success = false, message = "Введите код промокода." });
            }

            var code = promoCode.ToUpperInvariant().Trim();
            var promo = await _context.PromoCodes
                .FirstOrDefaultAsync(p => p.Code == code && p.IsActive);

            if (promo == null)
            {
                return Json(new { success = false, message = "Промокод не найден или неактивен." });
            }

            if (promo.ExpiryDate.HasValue && promo.ExpiryDate.Value < DateTime.Now)
            {
                return Json(new { success = false, message = "Срок действия промокода истёк." });
            }

            if (promo.UsageLimit.HasValue && promo.UsedCount >= promo.UsageLimit.Value)
            {
                return Json(new { success = false, message = "Лимит использования промокода исчерпан." });
            }

            HttpContext.Session.SetString("PromoCode", promo.Code);
            HttpContext.Session.SetInt32("PromoDiscount", promo.DiscountPercent);

            // Считаем общую сумму корзины для обновления
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            decimal total = 0;

            if (!string.IsNullOrEmpty(userId))
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);
                if (cart != null)
                {
                    total = cart.CartItems.Sum(i => i.Product.Price * i.Quantity);
                }
            }
            else
            {
                var guestCart = GetGuestCart();
                foreach (var item in guestCart)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                        total += product.Price * item.Quantity;
                }
            }

            var discountAmount = total * promo.DiscountPercent / 100;
            var finalTotal = total - discountAmount;

            return Json(new
            {
                success = true,
                message = $"Промокод {promo.Code} применён! Скидка {promo.DiscountPercent}%",
                discountPercent = promo.DiscountPercent,
                discountAmount = discountAmount,
                total = total,
                finalTotal = finalTotal,
                promoCode = promo.Code
            });
        }

        // ===== POST: /Cart/RemovePromoCode (AJAX) =====
        [HttpPost]
        public async Task<IActionResult> RemovePromoCode()
        {
            HttpContext.Session.Remove("PromoCode");
            HttpContext.Session.Remove("PromoDiscount");

            // Считаем общую сумму корзины для обновления
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            decimal total = 0;

            if (!string.IsNullOrEmpty(userId))
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);
                if (cart != null)
                {
                    total = cart.CartItems.Sum(i => i.Product.Price * i.Quantity);
                }
            }
            else
            {
                var guestCart = GetGuestCart();
                foreach (var item in guestCart)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                        total += product.Price * item.Quantity;
                }
            }

            return Json(new
            {
                success = true,
                message = "Промокод удалён.",
                total = total,
                finalTotal = total
            });
        }
        // ===== POST: /Cart/RemoveFromCart (AJAX) =====
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId, string size = "OneSize")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // ===== ГОСТЬ =====
            if (string.IsNullOrEmpty(userId))
            {
                var cart = GetGuestCart();
                var item = cart.FirstOrDefault(i => i.ProductId == productId && i.Size == size);
                if (item == null)
                    return Json(new { success = false, message = "Товар не найден в корзине" });

                cart.Remove(item);
                SaveGuestCart(cart);

                var totalRemove = 0m;
                foreach (var ci in cart)
                {
                    var product = await _context.Products.FindAsync(ci.ProductId);
                    if (product != null)
                        totalRemove += product.Price * ci.Quantity;
                }

                // ===== РАСЧЁТ СКИДКИ ДЛЯ ГОСТЯ =====
                var promoCode = HttpContext.Session.GetString("PromoCode");
                var discountPercent = HttpContext.Session.GetInt32("PromoDiscount") ?? 0;
                var discountAmount = 0m;
                var finalTotal = totalRemove;
                if (!string.IsNullOrEmpty(promoCode) && discountPercent > 0)
                {
                    discountAmount = totalRemove * discountPercent / 100;
                    finalTotal = totalRemove - discountAmount;
                }

                return Json(new
                {
                    success = true,
                    total = totalRemove,
                    finalTotal = finalTotal,
                    discountAmount = discountAmount,
                    discountPercent = discountPercent,
                    count = cart.Sum(i => i.Quantity)
                });
            }

            // ===== АВТОРИЗОВАННЫЙ =====
            var cartFromDb = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cartFromDb == null)
                return Json(new { success = false, message = "Корзина не найдена" });

            var cartItem = cartFromDb.CartItems
                .FirstOrDefault(i => i.ProductId == productId && i.Size == size);

            if (cartItem == null)
                return Json(new { success = false, message = "Товар не найден в корзине" });

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            var total = cartFromDb.CartItems.Sum(i => i.Product.Price * i.Quantity);
            var count = cartFromDb.CartItems.Sum(i => i.Quantity);

            // ===== РАСЧЁТ СКИДКИ =====
            var promoCodeDb = HttpContext.Session.GetString("PromoCode");
            var discountPercentDb = HttpContext.Session.GetInt32("PromoDiscount") ?? 0;
            var discountAmountDb = 0m;
            var finalTotalDb = total;
            if (!string.IsNullOrEmpty(promoCodeDb) && discountPercentDb > 0)
            {
                discountAmountDb = total * discountPercentDb / 100;
                finalTotalDb = total - discountAmountDb;
            }

            HttpContext.Session.SetInt32("CartCount", count);

            return Json(new
            {
                success = true,
                total = total,
                finalTotal = finalTotalDb,
                discountAmount = discountAmountDb,
                discountPercent = discountPercentDb,
                count = count
            });
        }

        // ===== ПЕРЕНОС КОРЗИНЫ ПОСЛЕ РЕГИСТРАЦИИ/ВХОДА =====

        public async Task<IActionResult> MergeCartAfterLogin(string returnUrl = null)
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction(nameof(Index));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index", "Home");
            }

            var guestCart = GetGuestCart();

            if (guestCart.Any())
            {
                await MergeGuestCartToUser(userId);

                // Если были предупреждения — они уже в TempData["Warning"]
                if (TempData["Warning"] == null)
                {
                    TempData["Success"] = "Товары из гостевой корзины успешно перенесены!";
                }
                return RedirectToAction(nameof(Index));
            }
            else
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }
        }
    }
}