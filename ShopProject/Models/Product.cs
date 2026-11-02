using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopProject.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название товара обязательно")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OldPrice { get; set; } // Старая цена (для скидки)

        [NotMapped]
        public int? StockQuantity => HasSizes ? Sizes?.Sum(s => s.StockQuantity) : null;

        [StringLength(100)]
        public string? Article { get; set; } // Артикул

        [StringLength(50)]
        public string? Color { get; set; } // Цвет

        [StringLength(100)]
        public string? Material { get; set; } // Материал

        [StringLength(50)]
        public string? Country { get; set; } // Страна производства

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // --- ВНЕШНИЙ КЛЮЧ ---
        public int CategoryId { get; set; }

        public bool HasSizes { get; set; } = true; // true — у товара есть размеры, false — нет

        // --- НАВИГАЦИОННЫЕ СВОЙСТВА ---
        public Category Category { get; set; } = null!;

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        public ICollection<ProductSize> Sizes { get; set; } = new List<ProductSize>();

        public bool IsActive { get; set; } = true;

        // --- ВСПОМОГАТЕЛЬНЫЕ СВОЙСТВА (не хранятся в БД) ---
        [NotMapped]
        public string? MainImageUrl => Images?.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImageUrl;

        [NotMapped]
        public decimal? DiscountPercent => OldPrice.HasValue && OldPrice > Price
            ? Math.Round((1 - Price / OldPrice.Value) * 100, 0)
            : null;
    }
}