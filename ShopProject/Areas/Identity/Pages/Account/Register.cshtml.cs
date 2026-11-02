// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable
using ShopProject.Validators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using ShopProject.Data;

namespace ShopProject.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Введите Email")] // <--- ВОТ ЭТО
            [ValidEmail(ErrorMessage = "Введите корректный Email")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Введите пароль")] // <--- ВОТ ЭТО
            [StringLength(100, ErrorMessage = "Пароль должен быть не менее {2} символов", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Подтвердите пароль")]
            [Compare("Password", ErrorMessage = "Пароли не совпадают.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            // ============================================================
            // ===== ТОЛЬКО ЭТИ 2 ПОЛЯ ДЛЯ ЧЕКБОКСОВ =====
            // ============================================================

            // ============================================================
            // ===== ТОЛЬКО ЭТИ 2 ПОЛЯ ДЛЯ ЧЕКБОКСОВ =====
            // ============================================================

            [Display(Name = "Я принимаю условия публичной оферты")]
            public bool AgreeToOffer { get; set; }

            [Display(Name = "Я даю согласие на обработку персональных данных")]
            public bool AgreeToPrivacy { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            // Если пользователь уже авторизован — перенаправляем на главную
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // ============================================================
            // ===== ДИАГНОСТИКА — ЛОГИРУЕМ ВСЕ ДАННЫЕ =====
            // ============================================================
            _logger.LogInformation("========== НАЧАЛО РЕГИСТРАЦИИ ==========");
            _logger.LogInformation("Email: {Email}", Input?.Email ?? "NULL");
            _logger.LogInformation("AgreeToOffer: {AgreeToOffer}", Input?.AgreeToOffer ?? false);
            _logger.LogInformation("AgreeToPrivacy: {AgreeToPrivacy}", Input?.AgreeToPrivacy ?? false);
            _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);

            // Логируем все ошибки валидации
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("=== ОШИБКИ ВАЛИДАЦИИ ===");
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    foreach (var error in errors)
                    {
                        _logger.LogWarning("Поле '{Key}': {Error}", key, error.ErrorMessage);
                    }
                }
            }

            if (ModelState.IsValid)
            {
                // ============================================================
                // ===== ПРОВЕРКА СОГЛАСИЙ =====
                // ============================================================
                if (!Input.AgreeToOffer || !Input.AgreeToPrivacy)
                {
                    ModelState.AddModelError(string.Empty, "Для регистрации необходимо принять все условия");
                    _logger.LogWarning("Согласия НЕ приняты! AgreeToOffer={AgreeToOffer}, AgreeToPrivacy={AgreeToPrivacy}",
                        Input.AgreeToOffer, Input.AgreeToPrivacy);
                    return Page();
                }

                _logger.LogInformation("Согласия приняты, создаём пользователя...");

                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                // ============================================================
                // ===== СОХРАНЯЕМ СОГЛАСИЯ В ПОЛЬЗОВАТЕЛЕ =====
                // ============================================================
                user.AgreedToOffer = Input.AgreeToOffer;
                user.AgreedToOfferDate = Input.AgreeToOffer ? DateTime.UtcNow : null;
                user.AgreedToPrivacy = Input.AgreeToPrivacy;
                user.AgreedToPrivacyDate = Input.AgreeToPrivacy ? DateTime.UtcNow : null;

                _logger.LogInformation("Сохраняем пользователя в БД...");

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    // ===== ЛОГ ДО ОТПРАВКИ =====
                    _logger.LogInformation("Пытаюсь отправить письмо на {Email}", Input.Email);

                    var htmlMessage = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Подтверждение email</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background: #f5f4f0;
            margin: 0;
            padding: 40px 0;
        }}
        .container {{
            max-width: 560px;
            margin: 0 auto;
            background: #ffffff;
            padding: 40px;
            border: 1px solid #e8e7e4;
        }}
        .logo {{
            font-size: 26px;
            font-weight: 700;
            color: #1a1a1a;
            letter-spacing: 2px;
            text-align: center;
            border-bottom: 1px solid #e8e7e4;
            padding-bottom: 20px;
            margin-bottom: 24px;
        }}
        .logo span {{
            color: #999;
        }}
        .title {{
            font-size: 22px;
            font-weight: 600;
            color: #1a1a1a;
            margin-bottom: 12px;
        }}
        .text {{
            font-size: 15px;
            color: #555;
            line-height: 1.6;
            margin-bottom: 24px;
        }}
        .btn {{
            display: inline-block;
            padding: 14px 36px;
            background: #1a1a1a;
            color: #ffffff !important;
            text-decoration: none;
            font-weight: 600;
            font-size: 15px;
            letter-spacing: 0.5px;
            border-radius: 0;
            text-align: center;
        }}
        .btn:hover {{
            background: #333;
        }}
        .link-fallback {{
            font-size: 13px;
            color: #999;
            margin-top: 16px;
            word-break: break-all;
            background: #f5f4f0;
            padding: 12px;
        }}
        .footer {{
            margin-top: 30px;
            font-size: 12px;
            color: #999;
            border-top: 1px solid #e8e7e4;
            padding-top: 20px;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='logo'>TAIGI</div>

        <div class='title'>Добро пожаловать!</div>

        <p class='text'>
            Для завершения регистрации и подтверждения email адреса нажмите кнопку ниже:
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

        <div class='footer'>
            TAIGI — одежда для тех, кто ценит свободу.<br />
            Это автоматическое письмо, отвечать на него не нужно.
        </div>
    </div>
</body>
</html>
";

                    await _emailSender.SendEmailAsync(Input.Email, "Подтверждение регистрации — TAIGI", htmlMessage);

                    // ===== ЛОГ ПОСЛЕ ОТПРАВКИ =====Passwords must have at least one non alphanumeric character
                    _logger.LogInformation("Письмо успешно отправлено на {Email}", Input.Email);

                    // ===== АВТОРИЗУЕМ ПОЛЬЗОВАТЕЛЯ =====
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // ===== ПОКАЗЫВАЕМ СТРАНИЦУ УСПЕШНОЙ РЕГИСТРАЦИИ =====
                    return RedirectToPage("./RegisterSuccess", new { email = Input.Email, returnUrl = returnUrl });
                }

                foreach (var error in result.Errors)
                {
                    _logger.LogError("Ошибка создания пользователя: {Error}", error.Description);
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            _logger.LogInformation("========== КОНЕЦ РЕГИСТРАЦИИ (С ОШИБКОЙ) ==========");
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}