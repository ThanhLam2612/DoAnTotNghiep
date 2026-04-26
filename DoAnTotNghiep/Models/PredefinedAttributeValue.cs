using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnTotNghiep.Models
{
    public class PredefinedAttributeValue
    {
        [Key]
        public int PredefinedValueId { get; set; }

        public int AttributeId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá trị")]
        [StringLength(250)]
        public string Value { get; set; }

        public string? ColorHex { get; set; } // Mã màu hex (VD: #ff0000)

        [ForeignKey("AttributeId")]
        public virtual ProductAttribute? ProductAttribute { get; set; }

        public virtual ICollection<VariantAttributeValue>? VariantAttributeValues { get; set; }
    }
}
