using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnTotNghiep.Models
{
    public class ReviewLike
    {
        [Key]
        public int ReviewLikeId { get; set; }

        // Liên kết với đánh giá nào
        public int ReviewId { get; set; }
        [ForeignKey("ReviewId")]
        public virtual Review? Review { get; set; }

        // Lưu thông tin người Like (UserId nếu bạn dùng Identity, hoặc UserName)
        [MaxLength(100)]
        public string? UserName { get; set; }

        public string? UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
