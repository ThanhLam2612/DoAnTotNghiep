using System.ComponentModel.DataAnnotations;

namespace DoAnTotNghiep.Models
{
    public class Contact
    {
        [Key]
        public int ContactId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; } 

        [Required]
        public string Message { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int Status { get; set; } = 0;
    }
}
