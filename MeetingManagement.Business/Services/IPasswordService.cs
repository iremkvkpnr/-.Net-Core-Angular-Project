using System;

namespace MeetingManagement.Business.Services
{
    // Şifre işlemleri için interface
    public interface IPasswordService
    {
        // Şifreyi şifrele
        string HashPassword(string password);
        // Şifre doğrulaması yap
        bool VerifyPassword(string password, string hashedPassword);
    }
}