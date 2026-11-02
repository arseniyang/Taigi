using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopProject.Models;

namespace ShopProject.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ProductSize> ProductSizes { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
        public DbSet<PromoCode> PromoCodes { get; set; }
        public DbSet<PickupPoint> PickupPoints { get; set; } // <-- ДОБАВЛЕНО

        public DbSet<WishlistItem> WishlistItems { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Cart → User
            builder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithOne(u => u.Cart)
                .HasForeignKey<Cart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem → Product
            builder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product → ProductImage
            builder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== НОВЫЕ НАСТРОЙКИ =====

            // Order → User
            builder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order → PromoCode
            builder.Entity<Order>()
                .HasOne(o => o.PromoCode)
                .WithMany()
                .HasForeignKey(o => o.PromoCodeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Order → PickupPoint (НОВОЕ)
            builder.Entity<Order>()
                .HasOne(o => o.PickupPoint)
                .WithMany()
                .HasForeignKey(o => o.PickupPointId)
                .OnDelete(DeleteBehavior.SetNull);

            // OrderItem → Order
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderItem → Product
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // OrderStatusHistory → Order
            builder.Entity<OrderStatusHistory>()
                .HasOne(h => h.Order)
                .WithMany(o => o.StatusHistory)
                .HasForeignKey(h => h.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderStatusHistory → User (кто изменил)
            builder.Entity<OrderStatusHistory>()
                .HasOne(h => h.ChangedByUser)
                .WithMany()
                .HasForeignKey(h => h.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product → ProductSize
            builder.Entity<ProductSize>()
                .HasOne(ps => ps.Product)
                .WithMany(p => p.Sizes)
                .HasForeignKey(ps => ps.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // WishlistItem → User
            builder.Entity<WishlistItem>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // WishlistItem → Product
            builder.Entity<WishlistItem>()
                .HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<PickupPoint>(entity =>
            {
                entity.Property(p => p.Latitude)
                    .HasPrecision(10, 7); // 10 цифр всего, 7 после запятой

                entity.Property(p => p.Longitude)
                    .HasPrecision(10, 7); // 10 цифр всего, 7 после запятой
            });
        }
    }
}