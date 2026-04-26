using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnTotNghiep.Models
{
    public class Favorite
    {
        [Key]
        public int FavoriteId { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
