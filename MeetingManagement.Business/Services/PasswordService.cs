using System;
using MeetingManagement.Business.Services;
using BCrypt.Net;

namespace MeetingManagement.Business.Services
{
    // Şifre şifreleme ve doğrulama işlemleri için servis
    public class PasswordService : IPasswordService
    {
        // Şifreyi BCrypt ile şifrele (güvenli salt ile)
        public string HashPassword(string password)
        {
            // BCrypt ile şifreyi hash'le (otomatik salt oluşturur)
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        // Girilen şifre ile kayıtlı şifreyi karşılaştır
        public bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                // BCrypt ile şifre doğrulaması
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                // Hatalı hash formatı durumunda false döndür
                return false;
            }
        }
    }
}