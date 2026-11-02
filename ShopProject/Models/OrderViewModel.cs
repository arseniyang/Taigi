using System.ComponentModel.DataAnnotations;

namespace ShopProject.Models
{
    public class OrderViewModel
    {
        [Required(ErrorMessage = "Укажите адрес доставки")]
        [StringLength(200)]
        public string ShippingAddress { get; set; } = string.Empty;

        [StringLength(20)]
        [Phone(ErrorMessage = "Введите корректный номер телефона")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Введите корректный email")]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? DeliveryMethod { get; set; }

        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }
    }
}