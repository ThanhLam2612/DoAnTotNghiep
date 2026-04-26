using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace DoAnTotNghiep.Models
{
    public class ProductAttribute
    {
        [Key]
        public int AttributeId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên tùy chọn")]
        [StringLength(50)]
        [Display(Name = "Tên tùy chọn")]
        public string AttributeName { get; set; }

        [Display(Name = "Danh mục áp dụng")]
        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        public virtual ICollection<PredefinedAttributeValue>? PredefinedValues { get; set; }
    }
}
