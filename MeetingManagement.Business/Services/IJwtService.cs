using System;
using MeetingManagement.Models;

namespace MeetingManagement.Business.Services
{
    // JWT token işlemleri için interface
    public interface IJwtService
    {
        // Kullanıcı için token oluştur
        string GenerateToken(User user);
        // Token geçerliliğini kontrol et
        bool ValidateToken(string token);
        // Token'dan kullanıcı ID'sini al
        int? GetUserIdFromToken(string token);
    }
}