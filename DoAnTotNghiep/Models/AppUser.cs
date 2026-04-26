using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnTotNghiep.Models
{
    public class AppUser
    {
        [Key]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên tài khoản")]
        [StringLength(50)]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(255)]
        public string Password { get; set; } 

        [StringLength(20)]
        public string Role { get; set; } = "Admin"; 
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Số điện thoại")]
        public string Phone { get; set; }

        public DateTime? DateOfBirth { get; set; }
        public bool IsActive { get; set; } = true;

        [NotMapped] 
        [Required(ErrorMessage = "Vui lòng xác nhận lại mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp!")]
        public string ConfirmPassword { get; set; }

    }
}
