using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using ShopProject.Data;
using ShopProject.Models;
using ShopProject.Services;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace ShopProject.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStockService _stockService;
        private readonly IEmailSender _emailSender;

        public OrdersController(
     ApplicationDbContext context,
     UserManager<ApplicationUser> userManager,
     IStockService stockService,
     IEmailSender emailSender) // <-- ДОБАВИТЬ
        {
            _context = context;
            _userManager = userManager;
            _stockService = stockService;
            _emailSender = emailSender;
        }

        // ===== GET: /Orders/Index =====
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Images)
                .Include(o => o.PickupPoint)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ===== GET: /Orders/Checkout =====
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            if (user == null)
                return NotFound();

            // ===== ПРОВЕРКА: ПОДТВЕРЖДЁН ЛИ EMAIL =====
            if (!user.EmailConfirmed)
            {
                var callbackUrl = Url.Page(
        "/Account/ConfirmEmail",
        pageHandler: null,
        values: new { area = "Identity", userId = user.Id, code = code },
        protocol: Request.Scheme);

                // ===== КРАСИВОЕ HTML-ПИСЬМО =====
                var htmlMessage = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Подтверждение email для заказа</title>
    <style>
        body {{ font-family: Arial, sans-serif; background: #f5f4f0; margin: 0; padding: 40px 0; }}
        .container {{ max-width: 560px; margin: 0 auto; background: #ffffff; padding: 40px; border: 1px solid #e8e7e4; }}
        .logo {{ font-size: 26px; font-weight: 700; color: #1a1a1a; letter-spacing: 2px; text-align: center; border-bottom: 1px solid #e8e7e4; padding-bottom: 20px; margin-bottom: 24px; }}
        .title {{ font-size: 22px; font-weight: 600; color: #1a1a1a; margin-bottom: 12px; }}
        .text {{ font-size: 15px; color: #555; line-height: 1.6; margin-bottom: 24px; }}
        .btn {{ display: inline-block; padding: 14px 36px; background: #1a1a1a; color: #ffffff !important; text-decoration: none; font-weight: 600; font-size: 15px; letter-spacing: 0.5px; text-align: center; }}
        .btn:hover {{ background: #333; }}
        .link-fallback {{ font-size: 13px; color: #999; margin-top: 16px; word-break: break-all; background: #f5f4f0; padding: 12px; }}
        .footer {{ margin-top: 30px; font-size: 12px; color: #999; border-top: 1px solid #e8e7e4; padding-top: 20px; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='logo'>TAIGI</div>
        <div class='title'>Подтверждение email для заказа</div>
        <p class='text'>
            Для оформления заказа в магазине TAIGI необходимо подтвердить ваш email.
            Нажмите кнопку ниже, чтобы подтвердить:
        </p>
        <div style='text-align: center; margin: 24px 0;'>
            <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' class='btn'>Подтвердить email</a>
        </div>
        <p class='text' style='font-size:14px;color:#999;'>
            Если кнопка не работает, скопируйте ссылку в браузер:
        </p>
        <div class='link-fallback'>
            {HtmlEncoder.Default.Encode(callbackUrl)}
        </div>
        <p class='text' style='font-size:14px;color:#999;margin-top:12px;'>
            Если вы не пытались оформить заказ, просто проигнорируйте это письмо.
        </p>
        <div class='footer'>
            TAIGI — одежда для тех, кто ценит свободу.<br />
            Это автоматическое письмо, отвечать на него не нужно.
        </div>
    </div>
</body>
</html>";

                await _emailSender.SendEmailAsync(user.Email, "Подтверждение email для заказа — TAIGI", htmlMessage);

                TempData["Info"] = "Для оформления заказа подтвердите email. Ссылка отправлена на вашу почту.";
                return RedirectToAction("Index", "Cart");
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // Проверяем остатки перед показом формы
            var stockErrors = await _stockService.ValidateCartItemsAsync(cart.CartItems.ToList());
            if (stockErrors.Any())
            {
                var errorMessages = string.Join("<br/>", stockErrors.Select(e => e.Message));
                TempData["Error"] = $"Некоторые товары закончились:<br/>{errorMessages}";
                return RedirectToAction("Index", "Cart");
            }

            var pickupPoints = await _context.PickupPoints
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            var promoCode = HttpContext.Session.GetString("PromoCode");
            var discount = HttpContext.Session.GetInt32("PromoDiscount") ?? 0;

            var model = new CheckoutViewModel
            {
                Cart = cart,
                PickupPoints = pickupPoints,
                PromoCode = promoCode,
                PromoDiscount = discount,
                DiscountAmount = discount > 0 ? cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity) * discount / 100m : 0,
                ReceiverName = $"{user?.FirstName ?? ""} {user?.LastName ?? ""}".Trim(),
                ReceiverPhone = user?.PhoneNumber ?? "",
                Email = user?.Email ?? "",
                IsPhoneFromProfile = !string.IsNullOrEmpty(user?.PhoneNumber),
                UserFirstName = user?.FirstName,
                UserLastName = user?.LastName
            };

            return View(model);
        }
        // ===== POST: /Orders/Checkout =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            // ===== ПРОВЕРКА: ПОДТВЕРЖДЁН ЛИ EMAIL =====
            if (!user.EmailConfirmed)
            {
                TempData["Error"] = "Для оформления заказа необходимо подтвердить email.";
                return RedirectToAction("Index", "Cart");
            }

            // Вспомогательная функция для перезагрузки данных формы
            async Task ReloadFormData()
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.Images)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                model.Cart = cart;
                model.PickupPoints = await _context.PickupPoints
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.DisplayOrder)
                    .ToListAsync();
                model.PromoCode = HttpContext.Session.GetString("PromoCode");
                model.PromoDiscount = HttpContext.Session.GetInt32("PromoDiscount") ?? 0;

                if (model.PromoDiscount > 0 && cart != null)
                {
                    model.DiscountAmount = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity) * model.PromoDiscount / 100m;
                }
            }

            // ===== 1. ВАЛИДАЦИЯ =====
            if (!ModelState.IsValid)
            {
                await ReloadFormData();
                return View(model);
            }

            // ============================================================
            // ===== 1.5 ПРОВЕРКА СОГЛАСИЙ =====
            // ============================================================
            if (!model.AgreeToTerms || !model.AgreeToOrderProcessing)
            {
                ModelState.AddModelError(string.Empty, "Для оформления заказа необходимо принять все условия");
                await ReloadFormData();
                return View(model);
            }

            // ===== 2. ПРОВЕРКА ВЫБОРА СПОСОБА ДОСТАВКИ =====
            if (model.DeliveryType == "pickup" && !model.PickupPointId.HasValue)
            {
                ModelState.AddModelError("PickupPointId", "Выберите пункт самовывоза");
                await ReloadFormData();
                return View(model);
            }

            if (model.DeliveryType == "delivery")
            {
                if (string.IsNullOrWhiteSpace(model.City))
                    ModelState.AddModelError("City", "Укажите город");
                if (string.IsNullOrWhiteSpace(model.Street))
                    ModelState.AddModelError("Street", "Укажите улицу");
                if (string.IsNullOrWhiteSpace(model.House))
                    ModelState.AddModelError("House", "Укажите номер дома");

                if (!ModelState.IsValid)
                {
                    await ReloadFormData();
                    return View(model);
                }
            }

            // ===== 3. ЗАГРУЖАЕМ КОРЗИНУ =====
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Sizes)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                return RedirectToAction("Index", "Cart");

            // ===== 4. ПРОВЕРЯЕМ ОСТАТКИ =====
            var stockErrors = await _stockService.ValidateCartItemsAsync(cart.CartItems.ToList());
            if (stockErrors.Any())
            {
                var errorMessages = string.Join("<br/>", stockErrors.Select(e => e.Message));
                TempData["Error"] = $"Недостаточно товара:<br/>{errorMessages}";
                return RedirectToAction("Index", "Cart");
            }

            // ===== 5. ПРОВЕРЯЕМ ПРОМОКОД =====
            var promoCode = HttpContext.Session.GetString("PromoCode");
            var discount = HttpContext.Session.GetInt32("PromoDiscount") ?? 0;

            // ===== 6. ФОРМИРУЕМ АДРЕС =====
            string shippingAddress;
            if (model.DeliveryType == "pickup" && model.PickupPointId.HasValue)
            {
                var pickupPoint = await _context.PickupPoints.FindAsync(model.PickupPointId.Value);
                shippingAddress = $"Самовывоз: {pickupPoint?.Name ?? "Пункт #" + model.PickupPointId} - {pickupPoint?.Address ?? ""}";
            }
            else
            {
                shippingAddress = $"{model.City}, ул. {model.Street}, д. {model.House}" +
                    (!string.IsNullOrEmpty(model.Apartment) ? $", кв. {model.Apartment}" : "") +
                    (!string.IsNullOrEmpty(model.Entrance) ? $", под. {model.Entrance}" : "") +
                    (!string.IsNullOrEmpty(model.Floor) ? $", эт. {model.Floor}" : "") +
                    (!string.IsNullOrEmpty(model.IntercomCode) ? $", домофон {model.IntercomCode}" : "");
            }

            // ===== 7. СОЗДАЁМ ЗАКАЗ =====
            var totalAmount = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity);
            var discountAmount = discount > 0 ? totalAmount * discount / 100m : 0;

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = "Pending",
                ShippingAddress = shippingAddress,
                PhoneNumber = model.ReceiverPhone,
                Email = model.Email ?? user.Email,
                DeliveryMethod = model.DeliveryType == "pickup" ? "Самовывоз" : "Доставка курьером",
                PaymentMethod = "Оплата при получении",
                Comment = model.Comment,
                TotalAmount = totalAmount - discountAmount,
                DiscountAmount = discountAmount > 0 ? discountAmount : null,

                DeliveryType = model.DeliveryType,
                PickupPointId = model.PickupPointId,
                City = model.DeliveryType == "delivery" ? model.City : null,
                Street = model.DeliveryType == "delivery" ? model.Street : null,
                House = model.DeliveryType == "delivery" ? model.House : null,
                Apartment = model.DeliveryType == "delivery" ? model.Apartment : null,
                Entrance = model.DeliveryType == "delivery" ? model.Entrance : null,
                Floor = model.DeliveryType == "delivery" ? model.Floor : null,
                IntercomCode = model.DeliveryType == "delivery" ? model.IntercomCode : null,
                DeliveryDate = model.DeliveryDate,
                DeliveryTimeFrom = model.DeliveryTimeFrom,
                DeliveryTimeTo = model.DeliveryTimeTo,
                ReceiverName = model.ReceiverName,
                ReceiverPhone = model.ReceiverPhone,
                DeliveryComment = model.Comment,

                // ============================================================
                // ===== СОХРАНЯЕМ СОГЛАСИЯ В ЗАКАЗЕ =====
                // ============================================================
                AgreedToTerms = model.AgreeToTerms,
                AgreedToTermsDate = DateTime.UtcNow,
                AgreedToOrderProcessing = model.AgreeToOrderProcessing,
                AgreedToOrderProcessingDate = DateTime.UtcNow
            };

            // Применяем промокод
            if (!string.IsNullOrEmpty(promoCode) && discount > 0)
            {
                var promo = await _context.PromoCodes
                    .FirstOrDefaultAsync(p => p.Code == promoCode && p.IsActive);

                if (promo != null &&
                    (!promo.ExpiryDate.HasValue || promo.ExpiryDate.Value >= DateTime.Now) &&
                    (!promo.UsageLimit.HasValue || promo.UsedCount < promo.UsageLimit.Value))
                {
                    order.PromoCodeId = promo.Id;
                    promo.UsedCount++;
                    await _context.SaveChangesAsync();
                }
            }

            // Копируем позиции из корзины
            foreach (var cartItem in cart.CartItems)
            {
                var mainImage = cartItem.Product.Images?
                    .OrderBy(i => i.DisplayOrder)
                    .FirstOrDefault();

                order.OrderItems.Add(new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    PriceSnapshot = cartItem.Product.Price,
                    Quantity = cartItem.Quantity,
                    ProductImageUrl = mainImage?.ImageUrl,
                    Size = cartItem.Size ?? "OneSize"
                });
            }

            order.StatusHistory = new List<OrderStatusHistory>
    {
        new OrderStatusHistory
        {
            Status = "Pending",
            ChangedAt = DateTime.Now,
            ChangedByUserId = userId,
            Note = "Заказ создан"
        }
    };

            _context.Orders.Add(order);

            // ===== 8. СПИСЫВАЕМ ОСТАТКИ =====
            foreach (var cartItem in cart.CartItems)
            {
                var size = cartItem.Size ?? "OneSize";
                var success = await _stockService.ReserveStockAsync(cartItem.ProductId, size, cartItem.Quantity);

                if (!success)
                {
                    TempData["Error"] = $"Не удалось зарезервировать товар «{cartItem.Product.Name}». Попробуйте позже.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            // ===== 9. ОЧИЩАЕМ КОРЗИНУ =====
            _context.CartItems.RemoveRange(cart.CartItems);

            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("PromoCode");
            HttpContext.Session.Remove("PromoDiscount");

            TempData["Success"] = "✔ Заказ успешно оформлен!";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        // ===== GET: /Orders/Details/5 =====
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Images)
                .Include(o => o.PromoCode)
                .Include(o => o.PickupPoint)
                .Include(o => o.StatusHistory)
                    .ThenInclude(h => h.ChangedByUser)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // ===== POST: /Orders/Cancel/5 =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound();

            if (order.Status == "Shipped" || order.Status == "Delivered")
            {
                TempData["Error"] = "Заказ уже отправлен или доставлен. Отмена невозможна.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (order.Status == "Cancelled")
            {
                TempData["Error"] = "Заказ уже отменен.";
                return RedirectToAction(nameof(Details), new { id });
            }

            foreach (var item in order.OrderItems)
            {
                var size = item.Size ?? "OneSize";
                await _stockService.ReleaseStockAsync(item.ProductId, size, item.Quantity);
            }

            order.Status = "Cancelled";

            order.StatusHistory ??= new List<OrderStatusHistory>();
            order.StatusHistory.Add(new OrderStatusHistory
            {
                Status = "Cancelled",
                ChangedAt = DateTime.Now,
                ChangedByUserId = userId,
                Note = "Заказ отменен пользователем (товары возвращены на склад)"
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Заказ успешно отменен. Товары возвращены на склад.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}