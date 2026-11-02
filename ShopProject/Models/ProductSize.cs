using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopProject.Models
{
    public class ProductSize
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        [StringLength(10)]
        public string Size { get; set; } = string.Empty; // XS, S, M, L, XL, XXL

        public int StockQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PriceModifier { get; set; } // Доплата за размер (опционально)

        public bool IsAvailable { get; set; } = true;

        // Навигационное свойство
        public Product Product { get; set; } = null!;
    }
}