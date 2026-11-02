using System.ComponentModel.DataAnnotations;

namespace ShopProject.Models
{
    public class CheckoutViewModel
    {
        // ===== СПОСОБ ДОСТАВКИ =====
        [Required(ErrorMessage = "Выберите способ доставки")]
        public string DeliveryType { get; set; } = "delivery"; // delivery / pickup

        // ===== АДРЕС ДОСТАВКИ =====
        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(200)]
        public string? Street { get; set; }

        [StringLength(20)]
        public string? House { get; set; }

        [StringLength(20)]
        public string? Apartment { get; set; }

        [StringLength(10)]
        public string? Entrance { get; set; }

        [StringLength(10)]
        public string? Floor { get; set; }

        [StringLength(20)]
        public string? IntercomCode { get; set; }

        // ===== САМОВЫВОЗ =====
        public int? PickupPointId { get; set; }

        // ===== КОНТАКТНЫЕ ДАННЫЕ =====
        [Required(ErrorMessage = "Укажите ФИО получателя")]
        [StringLength(100)]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Укажите телефон")]
        [Phone(ErrorMessage = "Введите корректный номер телефона")]
        [StringLength(20)]
        public string ReceiverPhone { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        // ===== ДОПОЛНИТЕЛЬНО =====
        [StringLength(500)]
        public string? Comment { get; set; }

        public DateTime? DeliveryDate { get; set; }

        public TimeSpan? DeliveryTimeFrom { get; set; }

        public TimeSpan? DeliveryTimeTo { get; set; }

        // ===== ДЛЯ ОТОБРАЖЕНИЯ В VIEW =====
        public Cart? Cart { get; set; }
        public string? PromoCode { get; set; }
        public int? PromoDiscount { get; set; }
        public List<PickupPoint>? PickupPoints { get; set; }
        public bool IsPhoneFromProfile { get; set; }
        public string? UserFirstName { get; set; }
        public string? UserLastName { get; set; }

        // ===== СКИДКА (ДОБАВЛЕНО) =====
        public decimal? DiscountAmount { get; set; }

        public decimal Subtotal => Cart?.CartItems.Sum(ci => ci.Product.Price * ci.Quantity) ?? 0;

        public decimal TotalWithDiscount => Subtotal - (DiscountAmount ?? 0);


        // ============================================================
        // ===== НОВЫЕ ПОЛЯ ДЛЯ ЧЕКБОКСОВ СОГЛАСИЙ =====
        // ============================================================

        /// <summary>
        /// Согласие с условиями публичной оферты при оформлении заказа
        /// </summary>
        [Display(Name = "Я подтверждаю согласие с условиями публичной оферты")]
        public bool AgreeToTerms { get; set; }

        /// <summary>
        /// Согласие на обработку персональных данных для выполнения заказа
        /// </summary>
        [Display(Name = "Я даю согласие на обработку персональных данных для выполнения заказа")]
        public bool AgreeToOrderProcessing { get; set; }
    }
}