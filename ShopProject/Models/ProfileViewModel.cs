using System.ComponentModel.DataAnnotations;

namespace ShopProject.Models
{
    public class ProfileViewModel
    {
        [Display(Name = "Имя")]
        public string? FirstName { get; set; }

        [Display(Name = "Фамилия")]
        public string? LastName { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Phone]
        [Display(Name = "Телефон")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Дата рождения")]
        [DataType(DataType.Date)]
        [CustomValidation(typeof(ProfileViewModel), nameof(ValidateDateOfBirth))]
        public DateTime? DateOfBirth { get; set; }

        // ================================================================
        //  КАСТОМНАЯ ВАЛИДАЦИЯ ДАТЫ РОЖДЕНИЯ
        // ================================================================
        public static ValidationResult? ValidateDateOfBirth(DateTime? dateOfBirth, ValidationContext context)
        {
            if (!dateOfBirth.HasValue)
                return ValidationResult.Success;

            var minDate = new DateTime(1900, 1, 1);
            var maxDate = DateTime.Today.AddYears(-13);

            if (dateOfBirth.Value < minDate)
            {
                return new ValidationResult("Дата рождения не может быть раньше 1900 года.");
            }

            if (dateOfBirth.Value > maxDate)
            {
                return new ValidationResult("Вы должны быть старше 13 лет.");
            }

            return ValidationResult.Success;
        }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Введите текущий пароль")]
        [DataType(DataType.Password)]
        [Display(Name = "Текущий пароль")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите новый пароль")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть не менее 6 символов")]
        [DataType(DataType.Password)]
        [Display(Name = "Новый пароль")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Подтвердите пароль")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
        [Display(Name = "Подтверждение пароля")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}