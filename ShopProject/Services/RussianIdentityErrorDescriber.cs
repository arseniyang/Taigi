using Microsoft.AspNetCore.Identity;

namespace ShopProject.Services
{
    public class RussianIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError() => new IdentityError { Code = nameof(DefaultError), Description = "Произошла неизвестная ошибка." };
        public override IdentityError PasswordMismatch() => new IdentityError { Code = nameof(PasswordMismatch), Description = "Неверный пароль." };
        public override IdentityError DuplicateEmail(string email) => new IdentityError { Code = nameof(DuplicateEmail), Description = $"Email '{email}' уже занят." };
        public override IdentityError DuplicateUserName(string userName) => new IdentityError { Code = nameof(DuplicateUserName), Description = $"Имя пользователя '{userName}' уже занято." };
        public override IdentityError InvalidEmail(string email) => new IdentityError { Code = nameof(InvalidEmail), Description = $"Email '{email}' недействителен." };
        public override IdentityError PasswordTooShort(int length) => new IdentityError { Code = nameof(PasswordTooShort), Description = $"Пароль должен содержать минимум {length} символов." };
        public override IdentityError PasswordRequiresNonAlphanumeric() => new IdentityError { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Пароль должен содержать хотя бы один спецсимвол (!, @, #, и т.д.)." };
        public override IdentityError PasswordRequiresDigit() => new IdentityError { Code = nameof(PasswordRequiresDigit), Description = "Пароль должен содержать хотя бы одну цифру ('0'-'9')." };
        public override IdentityError PasswordRequiresLower() => new IdentityError { Code = nameof(PasswordRequiresLower), Description = "Пароль должен содержать хотя бы одну строчную букву ('a'-'z')." };
        public override IdentityError PasswordRequiresUpper() => new IdentityError { Code = nameof(PasswordRequiresUpper), Description = "Пароль должен содержать хотя бы одну заглавную букву ('A'-'Z')." };
    }
}