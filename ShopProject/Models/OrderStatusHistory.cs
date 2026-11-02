using ShopProject.Data;
using System.ComponentModel.DataAnnotations;

namespace ShopProject.Models
{
    public class OrderStatusHistory
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = string.Empty;

        public DateTime ChangedAt { get; set; } = DateTime.Now;

        [StringLength(450)]
        public string? ChangedByUserId { get; set; } // Кто изменил статус

        [StringLength(500)]
        public string? Note { get; set; } // Примечание к изменению

        // Навигационные свойства
        public Order Order { get; set; } = null!;
        public ApplicationUser? ChangedByUser { get; set; }
    }
}