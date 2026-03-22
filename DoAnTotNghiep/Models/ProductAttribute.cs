using System.ComponentModel.DataAnnotations;
namespace DoAnTotNghiep.Models
{
    public class ProductAttribute
    {
        [Key]
        public int AttributeId { get; set; }

        [Required]
        [StringLength(50)]
        public string AttributeName { get; set; } 
    }
}
