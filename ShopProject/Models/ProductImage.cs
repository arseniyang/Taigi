using System.ComponentModel.DataAnnotations;

namespace ShopProject.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        public int ProductId { get; set; }

        public Product Product { get; set; } = null!;

        // Если true — это внешний URL, если false — локальный файл
        public bool IsExternal { get; set; } = false;
    }
}