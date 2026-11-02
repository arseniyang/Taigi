using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services; // <-- ВАЖНО! ИСПОЛЬЗУЕМ Identity-интерфейс
using Microsoft.Extensions.Options;
using MimeKit;
using ShopProject.Models;

namespace ShopProject.Services
{
    public class EmailSender : IEmailSender // <-- Теперь реализует Identity-интерфейс
    {
        private readonly EmailSettings _emailSettings;

        public EmailSender(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                Console.WriteLine($"📧 Пытаюсь отправить письмо на {email}");
                Console.WriteLine($"📧 SMTP: {_emailSettings.SmtpServer}:{_emailSettings.Port}");
                Console.WriteLine($"📧 From: {_emailSettings.FromEmail}");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlMessage };

                using var client = new SmtpClient();
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, SecureSocketOptions.SslOnConnect);
                await client.AuthenticateAsync(_emailSettings.FromEmail, _emailSettings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"✅ Письмо успешно отправлено на {email}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ОШИБКА ПРИ ОТПРАВКЕ: {ex.Message}");
                Console.WriteLine($"❌ ПОДРОБНОСТИ: {ex.InnerException?.Message ?? "Нет дополнительной информации"}");
                throw;
            }
        }
    }
}