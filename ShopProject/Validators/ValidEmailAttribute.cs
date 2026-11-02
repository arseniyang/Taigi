using System.ComponentModel.DataAnnotations;

namespace ShopProject.Validators
{
    public class ValidEmailAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var email = value.ToString();
            if (string.IsNullOrWhiteSpace(email))
                return ValidationResult.Success;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);

                // ===== ПРОВЕРЯЕМ, ЧТО В ДОМЕНЕ ЕСТЬ ТОЧКА И МИНИМУМ 2 СИМВОЛА ПОСЛЕ НЕЁ =====
                var host = addr.Host;
                var lastDotIndex = host.LastIndexOf('.');

                if (addr.Address == email &&
                    lastDotIndex > 0 &&
                    lastDotIndex < host.Length - 2) // минимум 2 символа после точки (.com, .ru, .io)
                {
                    return ValidationResult.Success;
                }
            }
            catch
            {
                // Невалидный email
            }

            return new ValidationResult("Введите корректный email адрес (например, user@domain.com)");
        }
    }
}