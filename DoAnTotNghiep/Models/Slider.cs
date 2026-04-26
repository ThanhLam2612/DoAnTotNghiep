using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Models
{
    public class Slider
    {
        [Key]
        public int SliderId { get; set; }

        [Required(ErrorMessage = "Vui lòng cung cấp hình ảnh cho Slide")]
        [Display(Name = "Đường dẫn ảnh")]
        public string ImageUrl { get; set; }

        [MaxLength(200)]
        [Display(Name = "Tiêu đề chính")]
        public string? Title { get; set; }

        [MaxLength(500)]
        [Display(Name = "Mô tả ngắn")]
        public string? Description { get; set; }

        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Trạng thái hiển thị")]
        public bool IsActive { get; set; } = true;

        // ==========================================
        // CÁC TRƯỜNG LƯU VẾT (AUDIT)
        // ==========================================
        [Display(Name = "Người tạo")]
        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Người sửa cuối")]
        [MaxLength(100)]
        public string? UpdatedBy { get; set; }

        [Display(Name = "Ngày sửa cuối")]
        public DateTime? UpdatedAt { get; set; }
    }
}