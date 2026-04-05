using System;
using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Models
{
    public class News
    {
        [Key]
        public int NewsId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài viết")]
        [StringLength(255)]
        public string Title { get; set; } 
        public string? ShortDescription { get; set; } 
        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        public string Content { get; set; } 
        public string? ImageUrl { get; set; } 
        public DateTime CreatedDate { get; set; } = DateTime.Now; 
        public string? CreatedBy { get; set; } 
        public DateTime? UpdatedDate { get; set; } 
        public string? UpdatedBy { get; set; }     
    }
}
