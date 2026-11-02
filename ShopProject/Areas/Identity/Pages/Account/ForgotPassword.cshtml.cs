// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using ShopProject.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace ShopProject.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Введите Email")]
            [EmailAddress(ErrorMessage = "Введите корректный Email")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                // ===== КРАСИВОЕ ПИСЬМО =====
                var htmlMessage = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Восстановление пароля</title>
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
        <div class='title'>Восстановление пароля</div>
        <p class='text'>
            Мы получили запрос на сброс пароля для вашего аккаунта.
            Нажмите кнопку ниже, чтобы установить новый пароль:
        </p>
        <div style='text-align: center; margin: 24px 0;'>
            <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' class='btn'>Сбросить пароль</a>
        </div>
        <p class='text' style='font-size:14px;color:#999;'>
            Если кнопка не работает, скопируйте ссылку в браузер:
        </p>
        <div class='link-fallback'>
            {HtmlEncoder.Default.Encode(callbackUrl)}
        </div>
        <p class='text' style='font-size:14px;color:#999;margin-top:12px;'>
            Если вы не запрашивали сброс пароля, просто проигнорируйте это письмо.
        </p>
        <div class='footer'>
            TAIGI — одежда для тех, кто ценит свободу.
        </div>
    </div>
</body>
</html>
";

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Восстановление пароля — TAIGI",
                    htmlMessage);

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}