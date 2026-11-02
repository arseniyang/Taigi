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
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        public AccountController(
      UserManager<ApplicationUser> userManager,
      SignInManager<ApplicationUser> signInManager,
      ApplicationDbContext context,
      IEmailSender emailSender) // <-- ДОБАВИТЬ
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailSender = emailSender;
        }

        // GET: /Account/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            // ===== УСТАНАВЛИВАЕМ ЗНАЧЕНИЕ ДЛЯ ПРЕДСТАВЛЕНИЯ =====
            ViewBag.IsEmailConfirmed = user.EmailConfirmed;

            var model = new ProfileViewModel
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth
            };

            return View(model);
        }

        // POST: /Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            // 1. Сначала получаем пользователя
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            // 2. Если форма заполнена с ошибками (например, возраст < 13)
            if (!ModelState.IsValid)
            {
                // ВАЖНО: возвращаем статус Email в ViewBag, чтобы страница не упала!
                ViewBag.IsEmailConfirmed = user.EmailConfirmed;
                return View(model);
            }

            // 3. Обновляем данные
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.DateOfBirth = model.DateOfBirth;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Профиль обновлён.";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // 4. Если упала база данных при сохранении — тоже возвращаем ViewBag
            ViewBag.IsEmailConfirmed = user.EmailConfirmed;
            return View(model);
        }

        // GET: /Account/ChangePassword
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "✔ Пароль изменён!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: /Account/Orders
        public async Task<IActionResult> Orders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }



        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmailConfirmation()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["Info"] = "Email уже подтверждён.";
                return RedirectToAction(nameof(Profile));
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = code },
                protocol: Request.Scheme);

            var htmlMessage = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Подтверждение email</title>
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
        <div class='title'>Подтверждение email</div>
        <p class='text'>
            Для подтверждения email адреса нажмите кнопку ниже:
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
            Если вы не запрашивали подтверждение, просто проигнорируйте это письмо.
        </p>
        <div class='footer'>
            TAIGI — одежда для тех, кто ценит свободу.<br />
            Это автоматическое письмо, отвечать на него не нужно.
        </div>
    </div>
</body>
</html>";

            await _emailSender.SendEmailAsync(user.Email, "Подтверждение email — TAIGI", htmlMessage);

            TempData["Info"] = "Письмо с подтверждением отправлено повторно.";
            return RedirectToAction(nameof(Profile));
        }
    }
}