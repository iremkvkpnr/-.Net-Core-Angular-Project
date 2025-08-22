namespace MeetingManagement.Models.DTOs
{
    // Giriş/kayıt işlemi sonrası dönen bilgiler
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public UserDto User { get; set; }
    }

    // Kullanıcı bilgileri için DTO
    public class UserDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string ProfileImagePath { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}