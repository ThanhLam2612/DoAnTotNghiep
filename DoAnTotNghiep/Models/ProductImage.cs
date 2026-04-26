using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnTotNghiep.Models
{
    public class ProductImage
    {
        [Key]
        public int ImageId { get; set; }

        public int ProductId { get; set; }
        public int? VariantId { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("VariantId")]
        public virtual ProductVariant? ProductVariant { get; set; }
    }
}
