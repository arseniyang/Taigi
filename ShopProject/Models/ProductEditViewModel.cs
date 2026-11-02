using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ShopProject.Models
{
    public class ProductEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название товара обязательно")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Range(0.01, 999999.99)]
        public decimal Price { get; set; }

        [Range(0, 999999.99)]
        public decimal? OldPrice { get; set; }

        [StringLength(100)]
        public string? Article { get; set; }

        [StringLength(50)]
        public string? Color { get; set; }

        [StringLength(100)]
        public string? Material { get; set; }

        [StringLength(50)]
        public string? Country { get; set; }

        public string? ImageOrder { get; set; } // <-- ДОЛЖНО БЫТЬ ТАК
        [Required]
        public int CategoryId { get; set; }

        public bool HasSizes { get; set; } = true;

        // Изображения
        public List<ProductImage> ExistingImages { get; set; } = new List<ProductImage>();
        public List<IFormFile>? NewImageFiles { get; set; }
        public List<string>? NewImageUrls { get; set; }
        public List<int>? ImagesToDelete { get; set; }
    }
}