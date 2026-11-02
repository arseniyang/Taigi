using System.ComponentModel.DataAnnotations;

namespace ShopProject.Models
{
    public class AdminOrderActionViewModel
    {
        public int OrderId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Note { get; set; }

        public string? TrackingNumber { get; set; }

        // Для отмены заказа
        public bool ReturnStock { get; set; } = true; // По умолчанию возвращаем
    }
}