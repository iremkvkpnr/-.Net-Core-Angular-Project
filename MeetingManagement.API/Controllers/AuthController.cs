using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MeetingManagement.Business.Services;
using MeetingManagement.Data;
using MeetingManagement.Models;
using MeetingManagement.Models.DTOs;

namespace MeetingManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly MeetingManagementDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;

        public AuthController(
            MeetingManagementDbContext context,
            IPasswordService passwordService,
            IJwtService jwtService,
            IEmailService emailService)
        {
            _context = context;
            _passwordService = passwordService;
            _jwtService = jwtService;
            _emailService = emailService;
        }

        // Kullanıcı kayıt işlemi
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
        {
            try
            {
                // Email kontrolü - zaten kayıtlı mı?
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == registerDto.Email);
                
                if (existingUser != null)
                {
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Bu email adresi zaten kayıtlı"
                    });
                }

                // Yeni kullanıcı oluştur
                var user = new User
                {
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    Email = registerDto.Email,
                    Phone = registerDto.Phone,
                    PasswordHash = _passwordService.HashPassword(registerDto.Password),
                    ProfileImagePath = registerDto.ProfileImagePath,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // JWT token oluştur
                var token = _jwtService.GenerateToken(user);

                // Hoş geldiniz emaili gönder (async olarak, hata durumunda kayıt işlemini etkilemesin)
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName, user.LastName);
                }
                catch (Exception emailEx)
                {
                    // Email gönderme hatası kayıt işlemini etkilemesin, sadece log'la
                    Console.WriteLine($"Hoş geldiniz emaili gönderilemedi: {emailEx.Message}");
                }

                return Ok(new AuthResponseDto
                {
                    Success = true,
                    Message = "Kayıt başarılı",
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        Phone = user.Phone,
                        ProfileImagePath = user.ProfileImagePath,
                        CreatedAt = user.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "Kayıt sırasında bir hata oluştu"
                });
            }
        }

        // Kullanıcı giriş işlemi
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            try
            {
                // Kullanıcıyı bul
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == loginDto.Email && u.IsActive);

                if (user == null)
                {
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Email veya şifre hatalı"
                    });
                }

                // Şifre kontrolü
                if (!_passwordService.VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Email veya şifre hatalı"
                    });
                }

                // JWT token oluştur
                var token = _jwtService.GenerateToken(user);

                return Ok(new AuthResponseDto
                {
                    Success = true,
                    Message = "Giriş başarılı",
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        Phone = user.Phone,
                        ProfileImagePath = user.ProfileImagePath,
                        CreatedAt = user.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "Giriş sırasında bir hata oluştu"
                });
            }
        }
    }
}