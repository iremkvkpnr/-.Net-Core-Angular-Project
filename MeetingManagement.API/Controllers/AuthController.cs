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
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            MeetingManagementDbContext context,
            IPasswordService passwordService,
            IJwtService jwtService,
            IEmailService emailService,
            IWebHostEnvironment environment,
            ILogger<AuthController> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _jwtService = jwtService;
            _emailService = emailService;
            _environment = environment;
            _logger = logger;
        }

        // Email configuration test endpoint
        [HttpGet("test-email-config")]
        public async Task<IActionResult> TestEmailConfig()
        {
            try
            {
                _logger.LogInformation("Email configuration test başlatılıyor...");
                // EmailService'i çağırarak constructor loglarını tetikle
                await _emailService.SendWelcomeEmailAsync("test@example.com", "Test", "User");
                return Ok("Email configuration test tamamlandı - logları kontrol edin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email configuration test hatası: {Message}", ex.Message);
                return BadRequest($"Email test hatası: {ex.Message}");
            }
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
                    _logger.LogInformation($"Hoş geldiniz emaili gönderiliyor: {user.Email}");
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName, user.LastName);
                    _logger.LogInformation($"Hoş geldiniz emaili başarıyla gönderildi: {user.Email}");
                }
                catch (Exception emailEx)
                {
                    // Email gönderme hatası kayıt işlemini etkilemesin, sadece log'la
                    _logger.LogError(emailEx, $"Hoş geldiniz emaili gönderilemedi: {user.Email} - {emailEx.Message}");
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
        public async Task<ActionResult<UserDto>> UpdateProfile([FromForm] UpdateProfileDto updateDto, IFormFile? profileImage = null)
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
                        var oldFilePath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, user.ProfileImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Dosya adını oluştur
                    var fileName = $"profile_{DateTime.Now.Ticks}{extension}";
                    
                    // Uploads klasörünü oluştur
                    var uploadsPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "profiles");
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

        [HttpPost("test-email")]
        public async Task<IActionResult> TestEmail([FromBody] TestEmailDto testEmailDto)
        {
            try
            {
                await _emailService.SendWelcomeEmailAsync(
                    testEmailDto.Email,
                    "Test",
                    "User");

                return Ok(new { success = true, message = "Test email başarıyla gönderildi" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Email gönderilemedi: {ex.Message}" });
            }
        }
    }

    public class TestEmailDto
    {
        public string Email { get; set; } = string.Empty;
    }
}