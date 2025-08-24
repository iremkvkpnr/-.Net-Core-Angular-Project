using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace MeetingManagement.Business.Services
{
    /// <summary>
    /// Email gönderme işlemleri için servis implementasyonu
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["EmailSettings:SmtpUsername"] ?? "";
            _smtpPassword = _configuration["EmailSettings:SmtpPassword"] ?? "";
            _fromEmail = _configuration["EmailSettings:FromEmail"] ?? "noreply@meetingmanagement.com";
            _fromName = _configuration["EmailSettings:FromName"] ?? "Meeting Management System";
        }

        /// <summary>
        /// Hoş geldiniz emaili gönderir
        /// </summary>
        public async Task SendWelcomeEmailAsync(string toEmail, string firstName, string lastName)
        {
            var subject = "Meeting Management Sistemine Hoş Geldiniz!";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #2c3e50;'>Hoş Geldiniz {firstName} {lastName}!</h2>
                        <p>Meeting Management sistemine başarıyla kayıt oldunuz.</p>
                        <p>Artık toplantılarınızı kolayca yönetebilir, planlayabilir ve takip edebilirsiniz.</p>
                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <h3 style='color: #495057; margin-top: 0;'>Sistemin Özellikleri:</h3>
                            <ul style='color: #6c757d;'>
                                <li>Toplantı oluşturma ve düzenleme</li>
                                <li>Toplantı takibi ve yönetimi</li>
                                <li>Otomatik bildirimler</li>
                                <li>Güvenli kullanıcı yönetimi</li>
                            </ul>
                        </div>
                        <p>İyi kullanımlar dileriz!</p>
                        <hr style='border: none; border-top: 1px solid #dee2e6; margin: 30px 0;'>
                        <p style='color: #6c757d; font-size: 12px;'>Bu email otomatik olarak gönderilmiştir.</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        /// <summary>
        /// Toplantı bilgilendirme emaili gönderir
        /// </summary>
        public async Task SendMeetingNotificationEmailAsync(string toEmail, string meetingTitle, string meetingDescription, DateTime startDate, DateTime endDate)
        {
            var subject = $"Yeni Toplantı: {meetingTitle}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #2c3e50;'>Yeni Toplantı Bildirimi</h2>
                        <div style='background-color: #e3f2fd; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                            <h3 style='color: #1976d2; margin-top: 0;'>{meetingTitle}</h3>
                            <p style='color: #424242;'><strong>Açıklama:</strong> {meetingDescription}</p>
                            <p style='color: #424242;'><strong>Başlangıç:</strong> {startDate:dd.MM.yyyy HH:mm}</p>
                            <p style='color: #424242;'><strong>Bitiş:</strong> {endDate:dd.MM.yyyy HH:mm}</p>
                            <p style='color: #424242;'><strong>Süre:</strong> {(endDate - startDate).TotalMinutes} dakika</p>
                        </div>
                        <p>Lütfen toplantı saatinizi not alın ve zamanında katılım sağlayın.</p>
                        <hr style='border: none; border-top: 1px solid #dee2e6; margin: 30px 0;'>
                        <p style='color: #6c757d; font-size: 12px;'>Bu email otomatik olarak gönderilmiştir.</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        /// <summary>
        /// Toplantı iptal bilgilendirme emaili gönderir
        /// </summary>
        public async Task SendMeetingCancellationEmailAsync(string toEmail, string meetingTitle, string cancellationReason = "Belirtilmemiş")
        {
            var subject = $"Toplantı İptal Edildi: {meetingTitle}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #d32f2f;'>Toplantı İptal Bildirimi</h2>
                        <div style='background-color: #ffebee; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #d32f2f;'>
                            <h3 style='color: #d32f2f; margin-top: 0;'>{meetingTitle}</h3>
                            <p style='color: #424242;'><strong>Durum:</strong> İptal Edildi</p>
                            <p style='color: #424242;'><strong>İptal Nedeni:</strong> {cancellationReason}</p>
                        </div>
                        <p>Üzgünüz, yukarıda belirtilen toplantı iptal edilmiştir.</p>
                        <p>Herhangi bir sorunuz varsa lütfen organizatör ile iletişime geçin.</p>
                        <hr style='border: none; border-top: 1px solid #dee2e6; margin: 30px 0;'>
                        <p style='color: #6c757d; font-size: 12px;'>Bu email otomatik olarak gönderilmiştir.</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        /// <summary>
        /// Email gönderme işlemini gerçekleştirir
        /// </summary>
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = _smtpPort == 587 || _smtpPort == 465
                };

                // Only set credentials if username and password are provided
                if (!string.IsNullOrEmpty(_smtpUsername) && !string.IsNullOrEmpty(_smtpPassword))
                {
                    client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                }

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Log the error (in a real application, use a proper logging framework)
                Console.WriteLine($"Email gönderme hatası: {ex.Message}");
                throw new Exception($"Email gönderilemedi: {ex.Message}", ex);
            }
        }
    }
}