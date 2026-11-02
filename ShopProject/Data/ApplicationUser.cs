using Microsoft.AspNetCore.Identity;
using ShopProject.Models;

namespace ShopProject.Data
{
    public class ApplicationUser : IdentityUser
    {
        // Существующие поля
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public Cart? Cart { get; set; }

        // ===== НОВЫЕ ПОЛЯ ДЛЯ СОГЛАСИЙ (ТОЛЬКО ЭТО) =====
        public bool AgreedToOffer { get; set; }                 // Согласие с офертой
        public DateTime? AgreedToOfferDate { get; set; }        // Дата согласия с офертой

        public bool AgreedToPrivacy { get; set; }               // Согласие на обработку ПД
        public DateTime? AgreedToPrivacyDate { get; set; }      // Дата согласия на обработку ПД

       
    }
}