using ShopProject.Data;

namespace ShopProject.Models
{
    public class Cart
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;

        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}