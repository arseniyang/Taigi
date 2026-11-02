using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopProject.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceSnapshot { get; set; }

        public int Quantity { get; set; }

        [StringLength(500)]
        public string? ProductImageUrl { get; set; }

        [StringLength(10)]
        public string? Size { get; set; } // XS, S, M, L, XL, OneSize

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice => PriceSnapshot * Quantity;
    }
}