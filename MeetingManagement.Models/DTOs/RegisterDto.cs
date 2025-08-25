using System.ComponentModel.DataAnnotations;

namespace MeetingManagement.Models.DTOs
{
    // Kayıt olmak için gerekli bilgiler
    public class RegisterDto
    {
        [Required(ErrorMessage = "Ad gereklidir")]
        [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Soyad gereklidir")]
        [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "E-posta gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Telefon numarası gereklidir")]
        [StringLength(15, ErrorMessage = "Telefon numarası en fazla 15 karakter olabilir")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Şifre gereklidir")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6, en fazla 100 karakter olmalıdır")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Şifre onayı gereklidir")]
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor")]
        public string? ConfirmPassword { get; set; }
    }
}