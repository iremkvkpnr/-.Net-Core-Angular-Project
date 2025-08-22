using System.ComponentModel.DataAnnotations;

namespace MeetingManagement.Models.DTOs
{
    // Giriş yapmak için gerekli bilgiler
    public class LoginDto
    {
        [Required(ErrorMessage = "Email adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre gereklidir")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        public string Password { get; set; }
    }
}