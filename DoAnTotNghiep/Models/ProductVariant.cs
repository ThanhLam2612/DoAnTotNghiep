using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace DoAnTotNghiep.Models
{
    public class ProductVariant
    {
        [Key]
        public int VariantId { get; set; }

        public int ProductId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Required(ErrorMessage = "Vui lòng nhập giá bán")]
        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public string? ImageUrl { get; set; } // Ảnh đại diện duy nhất của Biến thể này (Để đổi màu khi click)
        
        public DateTime CreatedAt { get; set; } = DateTime.Now; // Mặc định lấy giờ hiện tại khi tạo
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public virtual ICollection<VariantAttributeValue>? AttributeValues { get; set; }
        public virtual ICollection<ProductImage>? ProductImages { get; set; }
        public virtual ICollection<PromotionVariant>? PromotionVariants { get; set; }
    }
}
