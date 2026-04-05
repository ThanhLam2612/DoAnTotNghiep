using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Models
{
    public class Promotion
    {
        [Key]
        public int PromotionId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên chương trình khuyến mãi")]
        [StringLength(200)]
        public string PromotionName { get; set; } 

        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập phần trăm giảm giá")]
        [Range(1, 100, ErrorMessage = "Mức giảm giá phải từ 1% đến 100%")]
        public int DiscountPercent { get; set; } 

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true; 

        public ICollection<PromotionProduct> PromotionProducts { get; set; }
    }
}
