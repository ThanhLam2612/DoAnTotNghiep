using System.ComponentModel.DataAnnotations;
namespace DoAnTotNghiep.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Bạn phải nhập tên danh mục")] 
        [StringLength(100)]
        public string CategoryName { get; set; } 
        public int? ParentId { get; set; } 
        public DateTime? CreatedDate { get; set; } = DateTime.Now; 
        public string? CreatedBy { get; set; } 
        public DateTime? UpdatedDate { get; set; } 
        public string? UpdatedBy { get; set; }     
        public virtual Category? ParentCategory { get; set; }
        public virtual ICollection<Category>? SubCategories { get; set; }
        public virtual ICollection<Product>? Products { get; set; }
    }
}
