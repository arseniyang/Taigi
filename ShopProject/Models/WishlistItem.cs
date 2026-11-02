using ShopProject.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopProject.Models
{
    public class WishlistItem
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public int ProductId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.Now;

        // Навигационные свойства
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }
}