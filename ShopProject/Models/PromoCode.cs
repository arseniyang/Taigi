using System.ComponentModel.DataAnnotations;

namespace ShopProject.Models
{
    public class PromoCode
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Range(0, 100)]
        public int DiscountPercent { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;

        public int? UsageLimit { get; set; }

        public int UsedCount { get; set; } = 0;
    }
}