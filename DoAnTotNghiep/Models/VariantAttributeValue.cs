using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace DoAnTotNghiep.Models
{
    public class VariantAttributeValue
    {
        [Key]
        public int Id { get; set; }

        public int VariantId { get; set; }

        public int AttributeId { get; set; }

        [Required]
        [StringLength(250)]
        public string Value { get; set; } 
        [ForeignKey("VariantId")]
        public virtual ProductVariant ProductVariant { get; set; }

        [ForeignKey("AttributeId")]
        public virtual ProductAttribute ProductAttribute { get; set; }
    }
}
