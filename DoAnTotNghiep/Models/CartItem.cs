using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnTotNghiep.Models
{
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }
        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual AppUser? User { get; set; }

        

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public int? VariantId { get; set; }
        [ForeignKey("VariantId")]
        public virtual ProductVariant? ProductVariant { get; set; }

        [Required]
        public int Quantity { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        [NotMapped]
        public string ProductName { get; set; } = "";

        [NotMapped]
        public string? VariantName { get; set; }

        [NotMapped]
        public string ThumbnailUrl { get; set; } = "";

        [NotMapped]
        public decimal Price { get; set; }

        [NotMapped]
        public decimal TotalAmount => Quantity * Price;
    }
}
