using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnTotNghiep.Models
{
    public class PromotionProduct
    {
        public int PromotionId { get; set; }
        [ForeignKey("PromotionId")]
        public Promotion Promotion { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }
    }
}
