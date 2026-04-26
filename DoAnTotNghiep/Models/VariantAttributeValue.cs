using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace DoAnTotNghiep.Models
{
    public class VariantAttributeValue
    {
        [Key]
        public int Id { get; set; }

        public int VariantId { get; set; }

        public int PredefinedValueId { get; set; }

        [ForeignKey("VariantId")]
        public virtual ProductVariant? ProductVariant { get; set; }

        [ForeignKey("PredefinedValueId")]
        public virtual PredefinedAttributeValue? PredefinedAttributeValue { get; set; }
    }
}
