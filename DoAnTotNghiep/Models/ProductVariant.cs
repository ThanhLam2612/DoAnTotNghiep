using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace DoAnTotNghiep.Models
{
    public class ProductVariant
    {
        [Key]
        public int VariantId { get; set; }

        public int ProductId { get; set; }
        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")]
        public decimal AdditionalPrice { get; set; }

        public int StockQuantity { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
        public virtual ICollection<VariantAttributeValue>? AttributeValues { get; set; }
    }
}
