using System;
using System.Security.Cryptography;
using System.Text;
using MeetingManagement.Business.Services;

namespace MeetingManagement.Business.Services
{
    // Şifre şifreleme ve doğrulama işlemleri için servis
    public class PasswordService : IPasswordService
    {
        // Şifreyi SHA256 ile şifrele
        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                // Şifreyi byte dizisine çevir ve hash'le
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Girilen şifre ile kayıtlı şifreyi karşılaştır
        public bool VerifyPassword(string password, string hashedPassword)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput.Equals(hashedPassword);
        }
    }
}