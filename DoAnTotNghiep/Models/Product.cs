using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace DoAnTotNghiep.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [StringLength(200)]
        public string ProductName { get; set; }

        public string? Description { get; set; }

        //[Column(TypeName = "decimal(18,2)")]
        //public decimal BasePrice { get; set; }

        public string? ThumbnailUrl { get; set; }

        public int CategoryId { get; set; }

        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        public int? BrandId { get; set; }
        [ForeignKey("BrandId")]
        public virtual Brand? Brand { get; set; }

        // CÁC MỐI QUAN HỆ CỦA SẢN PHẨM
        public virtual ICollection<ProductVariant>? ProductVariants { get; set; }
        
        public virtual ICollection<Review>? Reviews { get; set; } = new List<Review>();

        // MỚI: Thư viện ảnh chi tiết và Thông số kỹ thuật
        public virtual ICollection<ProductImage>? ProductImages { get; set; }
        public virtual ICollection<ProductSpecification>? Specifications { get; set; }
    }
}
