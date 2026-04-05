using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnTotNghiep.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
        [Required]
        [MaxLength(100)]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn số sao")]
        [Range(1, 5)]
        public int Rating { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
        public string Comment { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
       
        public bool IsApproved { get; set; } = true;
        
    }
}
