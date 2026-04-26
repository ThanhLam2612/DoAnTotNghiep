using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnTotNghiep.Models
{
    public class ProductSpecification
    {
        [Key]
        public int SpecId { get; set; }

        public int ProductId { get; set; }

        [Required(ErrorMessage = "Tên thông số không được để trống")]
        [StringLength(100)]
        public string SpecName { get; set; }

        [Required(ErrorMessage = "Giá trị không được để trống")]
        [StringLength(255)]
        public string SpecValue { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}
