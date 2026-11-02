using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using ShopProject.Data;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace ShopProject.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterSuccessModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public RegisterSuccessModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public string Email { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToPage("/Index");
            }

            Email = email;
            return Page();
        }

        public async Task<IActionResult> OnPostResendAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return RedirectToPage("/Index");
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["Success"] = "Email уже подтверждён.";
                return RedirectToPage("/Account/Login");
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = code },
                protocol: Request.Scheme);

            // ===== КРАСИВОЕ ПИСЬМО (КАК ПРИ РЕГИСТРАЦИИ) =====
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

        <div class='title'>Подтверждение email</div>

        <p class='text'>
            Мы получили запрос на повторную отправку ссылки для подтверждения email.
            Нажмите кнопку ниже, чтобы подтвердить свой адрес:
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
            Если вы не запрашивали повторную отправку, просто проигнорируйте это письмо.
        </p>

        <div class='footer'>
            TAIGI — одежда для тех, кто ценит свободу.<br />
            Это автоматическое письмо, отвечать на него не нужно.
        </div>
    </div>
</body>
</html>
";

            await _emailSender.SendEmailAsync(email, "Подтверждение регистрации — TAIGI", htmlMessage);

            TempData["Success"] = "Письмо отправлено повторно.";
            return RedirectToPage(new { email = email });
        }
    }
}