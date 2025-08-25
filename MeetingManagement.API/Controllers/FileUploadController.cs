using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MeetingManagement.Data;
using System.Security.Claims;
using System.IO.Compression;

namespace MeetingManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly MeetingManagementDbContext _context;
        private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private readonly string[] _allowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".txt", ".xlsx", ".pptx" };

        public FileUploadController(IWebHostEnvironment environment, MeetingManagementDbContext context)
        {
            _environment = environment;
            _context = context;
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
                
                // Dosyayı sıkıştır (sadece büyük dosyalar için)
                var finalFilePath = filePath;
                var finalFileName = fileName;
                
                if (file.Length > 1024 * 1024) // 1MB'dan büyükse sıkıştır
                {
                    var compressedFileName = fileName + ".gz";
                    var compressedFilePath = Path.Combine(uploadsPath, compressedFileName);
                    
                    finalFilePath = await CompressFileAsync(filePath, compressedFilePath);
                    finalFileName = Path.GetFileName(finalFilePath);
                }
                
                // Relative path döndür
                var relativePath = $"/uploads/documents/{finalFileName}";
                
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
        /// Dosya indirme - Güvenlik kontrollü
        /// </summary>
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName, [FromQuery] string type = "document")
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Dosya adı güvenlik kontrolü - path traversal saldırılarını önle
                if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
                {
                    return BadRequest(new { Success = false, Message = "Geçersiz dosya adı" });
                }
                
                // Dosya yolunu oluştur
                var subFolder = type == "profile" ? "profiles" : "documents";
                var uploadsPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", subFolder);
                var filePath = Path.Combine(uploadsPath, fileName);
                
                // Dosya var mı kontrol et
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { Success = false, Message = "Dosya bulunamadı" });
                }
                
                // Erişim kontrolü - sadece kendi dosyalarına erişebilir
                if (!await HasFileAccess(fileName, userId.ToString(), type))
                 {
                     return StatusCode(403, new { Success = false, Message = "Bu dosyaya erişim yetkiniz yok" });
                 }
                
                // Sıkıştırılmış dosya kontrolü ve açma
                byte[] fileBytes;
                var contentType = GetContentType(fileName);
                var originalFileName = GetOriginalFileName(fileName);
                
                if (IsCompressedFile(fileName))
                {
                    // Sıkıştırılmış dosyayı aç
                    var tempFilePath = await DecompressFileAsync(filePath);
                    fileBytes = System.IO.File.ReadAllBytes(tempFilePath);
                    
                    // Geçici dosyayı sil
                    if (tempFilePath != filePath && System.IO.File.Exists(tempFilePath))
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                    
                    // Content type'ı orijinal dosya uzantısına göre ayarla
                    var originalExtension = fileName.Replace(".gz", "");
                    contentType = GetContentType(originalExtension);
                }
                else
                {
                    fileBytes = System.IO.File.ReadAllBytes(filePath);
                }
                
                return File(fileBytes, contentType, originalFileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Dosya indirilirken hata oluştu" });
            }
        }

        /// <summary>
        /// Dosya önizleme - Güvenlik kontrollü
        /// </summary>
        [HttpGet("preview/{fileName}")]
        public async Task<IActionResult> PreviewFile(string fileName, [FromQuery] string type = "document")
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Dosya adı güvenlik kontrolü
                if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
                {
                    return BadRequest(new { Success = false, Message = "Geçersiz dosya adı" });
                }
                
                // Dosya yolunu oluştur
                var subFolder = type == "profile" ? "profiles" : "documents";
                var uploadsPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", subFolder);
                var filePath = Path.Combine(uploadsPath, fileName);
                
                // Dosya var mı kontrol et
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound();
                }
                
                // Erişim kontrolü
                if (!await HasFileAccess(fileName, userId.ToString(), type))
                 {
                     return StatusCode(403, new { Success = false, Message = "Bu dosyaya erişim yetkiniz yok" });
                 }
                
                // Sadece resim dosyaları için önizleme
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(extension))
                {
                    return BadRequest(new { Success = false, Message = "Bu dosya türü için önizleme desteklenmiyor" });
                }
                
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var contentType = GetContentType(fileName);
                
                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Dosya önizlenirken hata oluştu" });
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

        /// <summary>
        /// Mevcut dosyayı sıkıştır
        /// </summary>
        [HttpPost("compress/{fileName}")]
        public async Task<IActionResult> CompressFile(string fileName, [FromQuery] string type = "document")
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Dosya adı güvenlik kontrolü
                if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
                {
                    return BadRequest(new { Success = false, Message = "Geçersiz dosya adı" });
                }
                
                // Zaten sıkıştırılmış mı kontrol et
                if (IsCompressedFile(fileName))
                {
                    return BadRequest(new { Success = false, Message = "Dosya zaten sıkıştırılmış" });
                }
                
                // Dosya yolunu oluştur
                var subFolder = type == "profile" ? "profiles" : "documents";
                var uploadsPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", subFolder);
                var filePath = Path.Combine(uploadsPath, fileName);
                
                // Dosya var mı kontrol et
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { Success = false, Message = "Dosya bulunamadı" });
                }
                
                // Erişim kontrolü
                if (!await HasFileAccess(fileName, userId.ToString(), type))
                {
                    return StatusCode(403, new { Success = false, Message = "Bu dosyaya erişim yetkiniz yok" });
                }
                
                // Sıkıştırılmış dosya yolu
                var compressedFileName = fileName + ".gz";
                var compressedFilePath = Path.Combine(uploadsPath, compressedFileName);
                
                // Dosyayı sıkıştır
                var finalPath = await CompressFileAsync(filePath, compressedFilePath);
                var finalFileName = Path.GetFileName(finalPath);
                
                // Veritabanındaki dosya yolunu güncelle (eğer document ise)
                if (type == "document")
                {
                    var meeting = await _context.Meetings
                        .FirstOrDefaultAsync(m => m.DocumentPath != null && m.DocumentPath.Contains(fileName));
                    
                    if (meeting != null)
                    {
                        meeting.DocumentPath = meeting.DocumentPath.Replace(fileName, finalFileName);
                        await _context.SaveChangesAsync();
                    }
                }
                
                var originalSize = new FileInfo(filePath).Length;
                var compressedSize = new FileInfo(finalPath).Length;
                var compressionRatio = Math.Round((1.0 - (double)compressedSize / originalSize) * 100, 2);
                
                return Ok(new 
                { 
                    Success = true, 
                    Message = "Dosya başarıyla sıkıştırıldı",
                    OriginalFileName = fileName,
                    CompressedFileName = finalFileName,
                    OriginalSize = originalSize,
                    CompressedSize = compressedSize,
                    CompressionRatio = $"%{compressionRatio}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Dosya sıkıştırılırken hata oluştu" });
            }
        }

        /// <summary>
        /// Kullanıcının dosyaya erişim yetkisi olup olmadığını kontrol eder
        /// </summary>
        private async Task<bool> HasFileAccess(string fileName, string userId, string type)
        {
            try
            {
                if (type == "profile")
                {
                    // Profil resmi - sadece kendi profil resmini görebilir
                    return fileName.StartsWith($"profile_{userId}_");
                }
                else if (type == "document")
                {
                    // Sıkıştırılmış dosya adını temizle
                    var cleanFileName = fileName.Replace(".gz", "");
                    
                    // Toplantı dokümanı - dosya yolunda dosya adı geçen toplantıyı bul
                    var meeting = await _context.Meetings
                        .FirstOrDefaultAsync(m => m.DocumentPath != null && 
                            (m.DocumentPath.Contains(fileName) || m.DocumentPath.Contains(cleanFileName)));
                    
                    if (meeting == null)
                        return false;
                    
                    // Toplantı sahibi olmalı (şu an için sadece sahibi erişebilir)
                    return meeting.UserId.ToString() == userId;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
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

        // Dosya türüne göre content type belirle
        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }

        // Orijinal dosya adını çıkar
        private string GetOriginalFileName(string fileName)
        {
            // Format: meeting_userId_timestamp.extension veya profile_userId_timestamp.extension
            var parts = fileName.Split('_');
            if (parts.Length >= 3)
            {
                var extension = Path.GetExtension(fileName);
                var timestampPart = parts[2].Replace(extension, "");
                return $"document{extension}"; // Basit bir isim döndür
            }
            return fileName;
        }

        /// <summary>
        /// Dosyayı sıkıştırır ve sıkıştırılmış dosya yolunu döndürür
        /// </summary>
        private async Task<string> CompressFileAsync(string originalFilePath, string compressedFilePath)
        {
            try
            {
                using (var originalFileStream = new FileStream(originalFilePath, FileMode.Open, FileAccess.Read))
                using (var compressedFileStream = new FileStream(compressedFilePath, FileMode.Create, FileAccess.Write))
                using (var gzipStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                {
                    await originalFileStream.CopyToAsync(gzipStream);
                }
                
                // Orijinal dosyayı sil
                System.IO.File.Delete(originalFilePath);
                
                return compressedFilePath;
            }
            catch
            {
                // Hata durumunda orijinal dosyayı koru
                if (System.IO.File.Exists(compressedFilePath))
                {
                    System.IO.File.Delete(compressedFilePath);
                }
                return originalFilePath;
            }
        }

        /// <summary>
        /// Sıkıştırılmış dosyayı açar ve geçici dosya yolunu döndürür
        /// </summary>
        private async Task<string> DecompressFileAsync(string compressedFilePath)
        {
            try
            {
                var tempFilePath = Path.GetTempFileName();
                
                using (var compressedFileStream = new FileStream(compressedFilePath, FileMode.Open, FileAccess.Read))
                using (var gzipStream = new GZipStream(compressedFileStream, CompressionMode.Decompress))
                using (var decompressedFileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    await gzipStream.CopyToAsync(decompressedFileStream);
                }
                
                return tempFilePath;
            }
            catch
            {
                return compressedFilePath; // Sıkıştırılmamış dosya olarak döndür
            }
        }

        /// <summary>
        /// Dosyanın sıkıştırılmış olup olmadığını kontrol eder
        /// </summary>
        private bool IsCompressedFile(string fileName)
        {
            return fileName.EndsWith(".gz");
        }
    }
}