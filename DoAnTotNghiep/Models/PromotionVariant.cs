using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnTotNghiep.Models
{
    public class PromotionVariant
    {
        public int PromotionId { get; set; }
        [ForeignKey("PromotionId")]
        public virtual Promotion Promotion { get; set; }

        // MỚI: Trỏ vào VariantId thay vì ProductId
        public int VariantId { get; set; }
        [ForeignKey("VariantId")]
        public virtual ProductVariant ProductVariant { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập phần trăm giảm giá")]
        [Range(0, 100, ErrorMessage = "Mức giảm giá phải từ 0% đến 100%")]
        public int DiscountPercent { get; set; }
    }
}
