using Microsoft.EntityFrameworkCore;
using DoAnTotNghiep.Models; 
namespace DoAnTotNghiep.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductAttribute> ProductAttributes { get; set; }
        public DbSet<VariantAttributeValue> VariantAttributeValues { get; set; }
        public DbSet<Brand> Brands { get; set; } 
        public DbSet<News> News { get; set; } 
        public DbSet<AppUser> AppUsers { get; set; } 
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<PromotionProduct> PromotionProducts { get; set; }
        public DbSet<Review> Reviews { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PromotionProduct>()
                .HasKey(pp => new { pp.PromotionId, pp.ProductId });
        }
    }
}
