using System.Threading.Tasks;

namespace MeetingManagement.Business.Services
{
    /// <summary>
    /// Email gönderme işlemleri için servis interface'i
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Hoş geldiniz emaili gönderir
        /// </summary>
        /// <param name="toEmail">Alıcı email adresi</param>
        /// <param name="firstName">Kullanıcının adı</param>
        /// <param name="lastName">Kullanıcının soyadı</param>
        /// <returns></returns>
        Task SendWelcomeEmailAsync(string toEmail, string firstName, string lastName);

        /// <summary>
        /// Toplantı bilgilendirme emaili gönderir
        /// </summary>
        /// <param name="toEmail">Alıcı email adresi</param>
        /// <param name="meetingTitle">Toplantı başlığı</param>
        /// <param name="meetingDescription">Toplantı açıklaması</param>
        /// <param name="startDate">Başlangıç tarihi</param>
        /// <param name="endDate">Bitiş tarihi</param>
        /// <returns></returns>
        Task SendMeetingNotificationEmailAsync(string toEmail, string meetingTitle, string meetingDescription, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Toplantı iptal bilgilendirme emaili gönderir
        /// </summary>
        /// <param name="toEmail">Alıcı email adresi</param>
        /// <param name="meetingTitle">Toplantı başlığı</param>
        /// <param name="cancellationReason">İptal nedeni</param>
        /// <returns></returns>
        Task SendMeetingCancellationEmailAsync(string toEmail, string meetingTitle, string cancellationReason = "Belirtilmemiş");
    }
}