using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Models
{
    public class Brand
    {
        [Key]
        public int BrandId { get; set; }

        [Required(ErrorMessage = "Tên thương hiệu không được để trống")]
        [StringLength(100)]
        public string BrandName { get; set; } 

        public string? Description { get; set; } 

        public string? LogoUrl { get; set; } 
        public DateTime? CreatedDate { get; set; } = DateTime.Now; 
        public string? CreatedBy { get; set; } 
        public DateTime? UpdatedDate { get; set; } 
        public string? UpdatedBy { get; set; }     

        public virtual ICollection<Product>? Products { get; set; }
    }
}
