using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MeetingManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private readonly string[] _allowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".txt", ".xlsx", ".pptx" };

        public FileUploadController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Profil resmi yükleme
        /// </summary>
        [HttpPost("profile-image")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { Success = false, Message = "Dosya seçilmedi" });
                }

                // Dosya boyutu kontrolü
                if (file.Length > _maxFileSize)
                {
                    return BadRequest(new { Success = false, Message = "Dosya boyutu 10MB'dan büyük olamaz" });
                }

                // Dosya uzantısı kontrolü
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(extension))
                {
                    return BadRequest(new { Success = false, Message = "Sadece resim dosyaları yüklenebilir (.jpg, .jpeg, .png, .gif)" });
                }

                // Kullanıcı ID'sini al
                var userId = GetCurrentUserId();
                
                // Dosya adını oluştur
                var fileName = $"profile_{userId}_{DateTime.Now.Ticks}{extension}";
                
                // Uploads klasörünü oluştur
                var uploadsPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "profiles");
                Directory.CreateDirectory(uploadsPath);
                
                // Dosya yolunu oluştur
                var filePath = Path.Combine(uploadsPath, fileName);
                
                // Dosyayı kaydet
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                
                // Relative path döndür
                var relativePath = $"/uploads/profiles/{fileName}";
                
                return Ok(new 
                { 
                    Success = true, 
                    Message = "Profil resmi başarıyla yüklendi", 
                    FilePath = relativePath 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Dosya yüklenirken hata oluştu" });
            }
        }

        /// <summary>
        /// Toplantı dokümanı yükleme
        /// </summary>
        [HttpPost("meeting-document")]
        public async Task<IActionResult> UploadMeetingDocument(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { Success = false, Message = "Dosya seçilmedi" });
                }

                // Dosya boyutu kontrolü
                if (file.Length > _maxFileSize)
                {
                    return BadRequest(new { Success = false, Message = "Dosya boyutu 10MB'dan büyük olamaz" });
                }

                // Dosya uzantısı kontrolü
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedDocumentExtensions.Contains(extension))
                {
                    return BadRequest(new { Success = false, Message = "Sadece doküman dosyaları yüklenebilir (.pdf, .doc, .docx, .txt, .xlsx, .pptx)" });
                }

                // Kullanıcı ID'sini al
                var userId = GetCurrentUserId();
                
                // Dosya adını oluştur
                var fileName = $"meeting_{userId}_{DateTime.Now.Ticks}{extension}";
                
                // Uploads klasörünü oluştur
                var uploadsPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "documents");
                Directory.CreateDirectory(uploadsPath);
                
                // Dosya yolunu oluştur
                var filePath = Path.Combine(uploadsPath, fileName);
                
                // Dosyayı kaydet
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                
                // Relative path döndür
                var relativePath = $"/uploads/documents/{fileName}";
                
                return Ok(new 
                { 
                    Success = true, 
                    Message = "Doküman başarıyla yüklendi", 
                    FilePath = relativePath,
                    OriginalFileName = file.FileName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Dosya yüklenirken hata oluştu" });
            }
        }

        /// <summary>
        /// Dosya silme
        /// </summary>
        [HttpDelete("{fileName}")]
        public IActionResult DeleteFile(string fileName, [FromQuery] string type = "document")
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Dosya yolunu oluştur
                var subFolder = type == "profile" ? "profiles" : "documents";
                var uploadsPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", subFolder);
                var filePath = Path.Combine(uploadsPath, fileName);
                
                // Dosya var mı kontrol et
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { Success = false, Message = "Dosya bulunamadı" });
                }
                
                // Güvenlik kontrolü - sadece kendi dosyalarını silebilir
                if (!fileName.Contains($"_{userId}_"))
                {
                    return Forbid();
                }
                
                // Dosyayı sil
                System.IO.File.Delete(filePath);
                
                return Ok(new { Success = true, Message = "Dosya başarıyla silindi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Dosya silinirken hata oluştu" });
            }
        }

        // Kullanıcının ID'sini token'dan al
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim!);
        }
    }
}