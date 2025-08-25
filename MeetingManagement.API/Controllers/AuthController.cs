using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<ActionResult<AuthResponseDto>> Register([FromForm] RegisterDto registerDto, IFormFile? profileImage = null)
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

                // Profil resmi yükleme işlemi
                string? profileImagePath = null;
                if (profileImage != null && profileImage.Length > 0)
                {
                    // Dosya boyutu kontrolü (10MB)
                    if (profileImage.Length > 10 * 1024 * 1024)
                    {
                        return BadRequest(new AuthResponseDto
                        {
                            Success = false,
                            Message = "Profil resmi 10MB'dan büyük olamaz"
                        });
                    }

                    // Dosya uzantısı kontrolü
                    var extension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    if (!allowedExtensions.Contains(extension))
                    {
                        return BadRequest(new AuthResponseDto
                        {
                            Success = false,
                            Message = "Sadece resim dosyaları yüklenebilir (.jpg, .jpeg, .png, .gif)"
                        });
                    }

                    // Dosya adını oluştur
                    var fileName = $"profile_{DateTime.Now.Ticks}{extension}";
                    
                    // Uploads klasörünü oluştur
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                    Directory.CreateDirectory(uploadsPath);
                    
                    // Dosya yolunu oluştur
                    var filePath = Path.Combine(uploadsPath, fileName);
                    
                    // Dosyayı kaydet
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(stream);
                    }
                    
                    // Relative path
                    profileImagePath = $"/uploads/profiles/{fileName}";
                }

                // Yeni kullanıcı oluştur
                var user = new User
                {
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    Email = registerDto.Email,
                    Phone = registerDto.Phone,
                    PasswordHash = _passwordService.HashPassword(registerDto.Password),
                    ProfileImagePath = profileImagePath,
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

        // Kullanıcı profil bilgilerini getir
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("Kullanıcı kimliği bulunamadı");
                }

                var userId = int.Parse(userIdClaim);
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    return NotFound("Kullanıcı bulunamadı");
                }

                return Ok(new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    ProfileImagePath = user.ProfileImagePath,
                    CreatedAt = user.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Profil bilgileri alınırken hata oluştu");
            }
        }

        // Kullanıcı profil bilgilerini güncelle
        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult<UserDto>> UpdateProfile([FromForm] RegisterDto updateDto, IFormFile? profileImage = null)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("Kullanıcı kimliği bulunamadı");
                }

                var userId = int.Parse(userIdClaim);
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    return NotFound("Kullanıcı bulunamadı");
                }

                // Email değişikliği kontrolü
                if (updateDto.Email != user.Email)
                {
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == updateDto.Email && u.Id != userId);
                    
                    if (existingUser != null)
                    {
                        return BadRequest("Bu email adresi zaten kullanılıyor");
                    }
                }

                // Profil resmi yükleme işlemi
                if (profileImage != null && profileImage.Length > 0)
                {
                    // Dosya boyutu kontrolü (10MB)
                    if (profileImage.Length > 10 * 1024 * 1024)
                    {
                        return BadRequest("Profil resmi 10MB'dan büyük olamaz");
                    }

                    // Dosya uzantısı kontrolü
                    var extension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    if (!allowedExtensions.Contains(extension))
                    {
                        return BadRequest("Sadece resim dosyaları yüklenebilir (.jpg, .jpeg, .png, .gif)");
                    }

                    // Eski dosyayı sil
                    if (!string.IsNullOrEmpty(user.ProfileImagePath))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfileImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Dosya adını oluştur
                    var fileName = $"profile_{DateTime.Now.Ticks}{extension}";
                    
                    // Uploads klasörünü oluştur
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                    Directory.CreateDirectory(uploadsPath);
                    
                    // Dosya yolunu oluştur
                    var filePath = Path.Combine(uploadsPath, fileName);
                    
                    // Dosyayı kaydet
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(stream);
                    }
                    
                    // Relative path
                    user.ProfileImagePath = $"/uploads/profiles/{fileName}";
                }

                // Kullanıcı bilgilerini güncelle
                user.FirstName = updateDto.FirstName ?? user.FirstName;
                user.LastName = updateDto.LastName ?? user.LastName;
                user.Email = updateDto.Email ?? user.Email;
                user.Phone = updateDto.Phone ?? user.Phone;
                user.UpdatedAt = DateTime.UtcNow;

                // Şifre güncellemesi (eğer verilmişse)
                if (!string.IsNullOrEmpty(updateDto.Password))
                {
                    user.PasswordHash = _passwordService.HashPassword(updateDto.Password);
                }

                await _context.SaveChangesAsync();

                return Ok(new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    ProfileImagePath = user.ProfileImagePath,
                    CreatedAt = user.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Profil güncellenirken hata oluştu");
            }
        }
    }
}