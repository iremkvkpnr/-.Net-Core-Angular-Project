using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MeetingManagement.Business.Services;
using MeetingManagement.Data;
using MeetingManagement.Models;
using MeetingManagement.Models.DTOs;
using System.Security.Claims;

namespace MeetingManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Tüm endpoint'ler için authentication gerekli
    public class MeetingController : ControllerBase
    {
        private readonly MeetingManagementDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly IWebHostEnvironment _environment;

        public MeetingController(MeetingManagementDbContext context, IEmailService emailService, IBackgroundJobService backgroundJobService, IWebHostEnvironment environment)
        {
            _context = context;
            _emailService = emailService;
            _backgroundJobService = backgroundJobService;
            _environment = environment;
        }

        // Kullanıcının ID'sini token'dan al
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return int.Parse(userIdClaim);
        }

        // Tüm toplantıları listele (sadece kendi toplantıları)
        [HttpGet]
        public async Task<ActionResult<MeetingListResponse>> GetMeetings()
        {
            try
            {
                var userId = GetCurrentUserId();
                
                var meetings = await _context.Meetings
                    .Include(m => m.User)
                    .Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => new MeetingResponseDto
                    {
                        Id = m.Id,
                        Title = m.Title,
                        Description = m.Description,
                        StartDate = m.StartDate,
                        EndDate = m.EndDate,
                        Location = m.Location,
                        DocumentPath = m.DocumentPath,
                        IsCancelled = m.IsCancelled,
                        CancelledAt = m.CancelledAt,
                        CreatedAt = m.CreatedAt,
                        UpdatedAt = m.UpdatedAt,
                        UserId = m.UserId,
                        UserName = $"{m.User.FirstName} {m.User.LastName}",
                        UserEmail = m.User.Email
                    })
                    .ToListAsync();

                return Ok(new MeetingListResponse
                {
                    Success = true,
                    Message = "Toplantılar başarıyla getirildi",
                    Data = meetings,
                    TotalCount = meetings.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new MeetingListResponse
                {
                    Success = false,
                    Message = "Toplantılar getirilirken hata oluştu",
                    Data = new List<MeetingResponseDto>()
                });
            }
        }

        // ID'ye göre toplantı getir
        [HttpGet("{id}")]
        public async Task<ActionResult<MeetingApiResponse>> GetMeeting(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                var meeting = await _context.Meetings
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

                if (meeting == null)
                {
                    return NotFound(new MeetingApiResponse
                    {
                        Success = false,
                        Message = "Toplantı bulunamadı"
                    });
                }

                var meetingDto = new MeetingResponseDto
                {
                    Id = meeting.Id,
                    Title = meeting.Title,
                    Description = meeting.Description,
                    StartDate = meeting.StartDate,
                    EndDate = meeting.EndDate,
                    Location = meeting.Location,
                    DocumentPath = meeting.DocumentPath,
                    IsCancelled = meeting.IsCancelled,
                    CancelledAt = meeting.CancelledAt,
                    CreatedAt = meeting.CreatedAt,
                    UpdatedAt = meeting.UpdatedAt,
                    UserId = meeting.UserId,
                    UserName = $"{meeting.User.FirstName} {meeting.User.LastName}",
                    UserEmail = meeting.User.Email
                };

                return Ok(new MeetingApiResponse
                {
                    Success = true,
                    Message = "Toplantı başarıyla getirildi",
                    Data = meetingDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new MeetingApiResponse
                {
                    Success = false,
                    Message = "Toplantı getirilirken hata oluştu"
                });
            }
        }

        // Yeni toplantı oluştur
        [HttpPost]
        public async Task<ActionResult<MeetingApiResponse>> CreateMeeting([FromForm] CreateMeetingDto createMeetingDto, IFormFile? document = null)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Tarih kontrolü
                if (createMeetingDto.StartDate >= createMeetingDto.EndDate)
                {
                    return BadRequest(new MeetingApiResponse
                    {
                        Success = false,
                        Message = "Başlangıç tarihi bitiş tarihinden önce olmalıdır"
                    });
                }

                if (createMeetingDto.StartDate < DateTime.UtcNow)
                {
                    return BadRequest(new MeetingApiResponse
                    {
                        Success = false,
                        Message = "Geçmiş tarihte toplantı oluşturamazsınız"
                    });
                }

                string? documentPath = null;
                
                // Dosya yükleme işlemi
                if (document != null && document.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "documents");
                    Directory.CreateDirectory(uploadsFolder);
                    
                    var fileName = $"{Guid.NewGuid()}_{document.FileName}";
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await document.CopyToAsync(stream);
                    }
                    
                    documentPath = $"uploads/documents/{fileName}";
                }

                var meeting = new Meeting
                {
                    Title = createMeetingDto.Title,
                    Description = createMeetingDto.Description,
                    StartDate = createMeetingDto.StartDate,
                    EndDate = createMeetingDto.EndDate,
                    Location = createMeetingDto.Location,
                    DocumentPath = documentPath,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsCancelled = false
                };

                _context.Meetings.Add(meeting);
                await _context.SaveChangesAsync();

                // Oluşturulan toplantıyı kullanıcı bilgileriyle birlikte getir
                var createdMeeting = await _context.Meetings
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.Id == meeting.Id);

                var meetingDto = new MeetingResponseDto
                {
                    Id = createdMeeting.Id,
                    Title = createdMeeting.Title,
                    Description = createdMeeting.Description,
                    StartDate = createdMeeting.StartDate,
                    EndDate = createdMeeting.EndDate,
                    Location = createdMeeting.Location,
                    DocumentPath = createdMeeting.DocumentPath,
                    IsCancelled = createdMeeting.IsCancelled,
                    CancelledAt = createdMeeting.CancelledAt,
                    CreatedAt = createdMeeting.CreatedAt,
                    UpdatedAt = createdMeeting.UpdatedAt,
                    UserId = createdMeeting.UserId,
                    UserName = $"{createdMeeting.User.FirstName} {createdMeeting.User.LastName}",
                    UserEmail = createdMeeting.User.Email
                };

                // Toplantı bilgilendirme emaili gönder (async olarak, hata durumunda toplantı oluşturma işlemini etkilemesin)
                try
                {
                    await _emailService.SendMeetingNotificationEmailAsync(
                        createdMeeting.User.Email,
                        createdMeeting.Title,
                        createdMeeting.Description,
                        createdMeeting.StartDate,
                        createdMeeting.EndDate);
                }
                catch (Exception emailEx)
                {
                    // Email gönderme hatası toplantı oluşturma işlemini etkilemesin, sadece log'la
                    Console.WriteLine($"Toplantı bilgilendirme emaili gönderilemedi: {emailEx.Message}");
                }

                return CreatedAtAction(nameof(GetMeeting), new { id = meeting.Id }, new MeetingApiResponse
                {
                    Success = true,
                    Message = "Toplantı başarıyla oluşturuldu",
                    Data = meetingDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new MeetingApiResponse
                {
                    Success = false,
                    Message = "Toplantı oluşturulurken hata oluştu"
                });
            }
        }

        // Toplantı güncelle
        [HttpPut("{id}")]
        public async Task<ActionResult<MeetingApiResponse>> UpdateMeeting(int id, [FromForm] UpdateMeetingDto updateMeetingDto, IFormFile? document = null)
        {
            try
            {

                var userId = GetCurrentUserId();
                
                var meeting = await _context.Meetings
                    .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

                if (meeting == null)
                {
                    return NotFound(new MeetingApiResponse
                    {
                        Success = false,
                        Message = "Toplantı bulunamadı"
                    });
                }

                // İptal edilmiş toplantı güncellenemez
                if (meeting.IsCancelled)
                {
                    return BadRequest(new MeetingApiResponse
                    {
                        Success = false,
                        Message = "İptal edilmiş toplantı güncellenemez"
                    });
                }

                // Tarih kontrolü
                if (updateMeetingDto.StartDate >= updateMeetingDto.EndDate)
                {
                    return BadRequest(new MeetingApiResponse
                    {
                        Success = false,
                        Message = "Başlangıç tarihi bitiş tarihinden önce olmalıdır"
                    });
                }

                // Dosya yükleme işlemi (varsa)
                if (document != null && document.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "documents");
                    Directory.CreateDirectory(uploadsFolder);
                    
                    var fileName = $"{Guid.NewGuid()}_{document.FileName}";
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await document.CopyToAsync(stream);
                    }
                    
                    // Eski dosyayı sil (varsa)
                    if (!string.IsNullOrEmpty(meeting.DocumentPath))
                    {
                        var oldFilePath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, meeting.DocumentPath);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }
                    
                    meeting.DocumentPath = $"uploads/documents/{fileName}";
                }

                // Güncelleme
                meeting.Title = updateMeetingDto.Title;
                meeting.Description = updateMeetingDto.Description;
                meeting.StartDate = updateMeetingDto.StartDate;
                meeting.EndDate = updateMeetingDto.EndDate;
                meeting.Location = updateMeetingDto.Location;
                meeting.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Güncellenmiş toplantıyı kullanıcı bilgileriyle birlikte getir
                var updatedMeeting = await _context.Meetings
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.Id == meeting.Id);

                // Toplantı güncelleme emaili gönder
                try
                {
                    await _emailService.SendMeetingUpdateEmailAsync(
                        updatedMeeting.User.Email,
                        updatedMeeting.Title,
                        updatedMeeting.Description,
                        updatedMeeting.StartDate,
                        updatedMeeting.EndDate,
                        "Toplantı detayları güncellendi");
                }
                catch (Exception emailEx)
                {
                    // Email gönderme hatası güncelleme işlemini etkilemesin, sadece log'la
                    Console.WriteLine($"Toplantı güncelleme emaili gönderilemedi: {emailEx.Message}");
                }

                var meetingDto = new MeetingResponseDto
                {
                    Id = updatedMeeting.Id,
                    Title = updatedMeeting.Title,
                    Description = updatedMeeting.Description,
                    StartDate = updatedMeeting.StartDate,
                    EndDate = updatedMeeting.EndDate,
                    Location = updatedMeeting.Location,
                    DocumentPath = updatedMeeting.DocumentPath,
                    IsCancelled = updatedMeeting.IsCancelled,
                    CancelledAt = updatedMeeting.CancelledAt,
                    CreatedAt = updatedMeeting.CreatedAt,
                    UpdatedAt = updatedMeeting.UpdatedAt,
                    UserId = updatedMeeting.UserId,
                    UserName = $"{updatedMeeting.User.FirstName} {updatedMeeting.User.LastName}",
                    UserEmail = updatedMeeting.User.Email
                };

                return Ok(new MeetingApiResponse
                {
                    Success = true,
                    Message = "Toplantı başarıyla güncellendi",
                    Data = meetingDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new MeetingApiResponse
                {
                    Success = false,
                    Message = "Toplantı güncellenirken hata oluştu"
                });
            }
        }

        // Toplantıyı iptal et (soft delete)
        [HttpPatch("{id}/cancel")]
        public async Task<ActionResult<MeetingApiResponse>> CancelMeeting(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                var meeting = await _context.Meetings
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

                if (meeting == null)
                {
                    return NotFound(new MeetingApiResponse
                    {
                        Success = false,
                        Message = "Toplantı bulunamadı"
                    });
                }

                if (meeting.IsCancelled)
                {
                    return BadRequest(new MeetingApiResponse
                    {
                        Success = false,
                        Message = "Toplantı zaten iptal edilmiş"
                    });
                }

                // Toplantıyı iptal et
                meeting.IsCancelled = true;
                meeting.CancelledAt = DateTime.UtcNow;
                meeting.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Toplantı iptal emaili gönder
                try
                {
                    await _emailService.SendMeetingCancellationEmailAsync(
                        meeting.User.Email,
                        meeting.Title,
                        "Kullanıcı tarafından iptal edildi");
                }
                catch (Exception emailEx)
                {
                    // Email gönderme hatası iptal işlemini etkilemesin, sadece log'la
                    Console.WriteLine($"Toplantı iptal emaili gönderilemedi: {emailEx.Message}");
                }

                var meetingDto = new MeetingResponseDto
                {
                    Id = meeting.Id,
                    Title = meeting.Title,
                    Description = meeting.Description,
                    StartDate = meeting.StartDate,
                    EndDate = meeting.EndDate,
                    Location = meeting.Location,
                    DocumentPath = meeting.DocumentPath,
                    IsCancelled = meeting.IsCancelled,
                    CancelledAt = meeting.CancelledAt,
                    CreatedAt = meeting.CreatedAt,
                    UpdatedAt = meeting.UpdatedAt,
                    UserId = meeting.UserId,
                    UserName = $"{meeting.User.FirstName} {meeting.User.LastName}",
                    UserEmail = meeting.User.Email
                };

                return Ok(new MeetingApiResponse
                {
                    Success = true,
                    Message = "Toplantı başarıyla iptal edildi",
                    Data = meetingDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new MeetingApiResponse
                {
                    Success = false,
                    Message = "Toplantı iptal edilirken hata oluştu"
                });
            }
        }

        // Toplantıyı kalıcı olarak sil (hard delete)
        [HttpDelete("{id}")]
        public async Task<ActionResult<MeetingApiResponse>> DeleteMeeting(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                var meeting = await _context.Meetings
                    .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

                if (meeting == null)
                {
                    return NotFound(new MeetingApiResponse
                    {
                        Success = false,
                        Message = "Toplantı bulunamadı"
                    });
                }

                _context.Meetings.Remove(meeting);
                await _context.SaveChangesAsync();

                return Ok(new MeetingApiResponse
                {
                    Success = true,
                    Message = "Toplantı başarıyla silindi"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new MeetingApiResponse
                {
                    Success = false,
                    Message = "Toplantı silinirken hata oluştu"
                });
            }
        }
    }
}