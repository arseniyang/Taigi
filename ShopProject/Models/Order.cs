using ShopProject.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopProject.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered, Cancelled

        // ===== АДРЕС ДОСТАВКИ (ОСНОВНОЙ) =====
        [Required(ErrorMessage = "Укажите адрес доставки")]
        [StringLength(200)]
        public string ShippingAddress { get; set; } = string.Empty;

        // ===== КОНТАКТНЫЕ ДАННЫЕ =====
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        // ===== СПОСОБЫ ДОСТАВКИ И ОПЛАТЫ =====
        [StringLength(50)]
        public string? DeliveryMethod { get; set; } // Courier, Pickup, Post

        [StringLength(50)]
        public string? PaymentMethod { get; set; } // Card, Cash, BankTransfer

        // ===== ПРОМОКОД =====
        public int? PromoCodeId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountAmount { get; set; }

        // ===== ДОПОЛНИТЕЛЬНО =====
        [StringLength(500)]
        public string? Comment { get; set; }

        [StringLength(50)]
        public string? TrackingNumber { get; set; }

        // ===== НОВЫЕ ПОЛЯ ДЛЯ ДОСТАВКИ =====
        [StringLength(20)]
        public string? DeliveryType { get; set; } // "delivery" или "pickup"

        public int? PickupPointId { get; set; }

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

        public DateTime? DeliveryDate { get; set; }

        public TimeSpan? DeliveryTimeFrom { get; set; }

        public TimeSpan? DeliveryTimeTo { get; set; }

        [StringLength(100)]
        public string? ReceiverName { get; set; }

        [StringLength(20)]
        public string? ReceiverPhone { get; set; }

        [StringLength(500)]
        public string? DeliveryComment { get; set; }

        // ============================================================
        // ===== НОВЫЕ ПОЛЯ ДЛЯ СОГЛАСИЙ ПО ЗАКАЗУ =====
        // ============================================================

        /// <summary>
        /// Согласие с условиями публичной оферты для этого заказа
        /// </summary>
        public bool AgreedToTerms { get; set; }

        /// <summary>
        /// Дата согласия с условиями публичной оферты
        /// </summary>
        public DateTime? AgreedToTermsDate { get; set; }

        /// <summary>
        /// Согласие на обработку персональных данных для выполнения этого заказа
        /// </summary>
        public bool AgreedToOrderProcessing { get; set; }

        /// <summary>
        /// Дата согласия на обработку персональных данных
        /// </summary>
        public DateTime? AgreedToOrderProcessingDate { get; set; }

        // ============================================================
        // ===== НАВИГАЦИОННЫЕ СВОЙСТВА =====
        // ============================================================

        public ApplicationUser? User { get; set; }
        public PromoCode? PromoCode { get; set; }
        public PickupPoint? PickupPoint { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<OrderStatusHistory>? StatusHistory { get; set; }
    }
}