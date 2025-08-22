using System;
using System.Threading.Tasks;

namespace MeetingManagement.Business.Services
{
    public interface IBackgroundJobService
    {
        /// <summary>
        /// İptal edilen toplantıları belirli bir süre sonra silen background job'u başlatır
        /// </summary>
        /// <param name="delayInMinutes">Kaç dakika sonra silinecek (varsayılan: 30 dakika)</param>
        void ScheduleDeleteCancelledMeetings(int delayInMinutes = 30);
        
        /// <summary>
        /// İptal edilen toplantıları siler
        /// </summary>
        Task DeleteCancelledMeetingsAsync();
        
        /// <summary>
        /// Belirli bir toplantıyı belirli bir süre sonra silen job'u başlatır
        /// </summary>
        /// <param name="meetingId">Silinecek toplantı ID'si</param>
        /// <param name="delayInMinutes">Kaç dakika sonra silinecek</param>
        void ScheduleDeleteSpecificMeeting(int meetingId, int delayInMinutes = 30);
        
        /// <summary>
        /// Belirli bir toplantıyı siler
        /// </summary>
        /// <param name="meetingId">Silinecek toplantı ID'si</param>
        Task DeleteSpecificMeetingAsync(int meetingId);
    }
}