using System.ComponentModel.DataAnnotations;

namespace ShopProject.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название категории обязательно")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Связь: у категории много товаров
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}