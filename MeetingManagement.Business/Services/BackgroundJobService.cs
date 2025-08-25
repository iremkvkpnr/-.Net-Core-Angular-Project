using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MeetingManagement.Data;

namespace MeetingManagement.Business.Services
{
    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly MeetingManagementDbContext _context;
        private readonly ILogger<BackgroundJobService> _logger;

        public BackgroundJobService(MeetingManagementDbContext context, ILogger<BackgroundJobService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public void ScheduleDeleteCancelledMeetings(int delayInMinutes = 30)
        {
            try
            {
                // Belirtilen süre sonra iptal edilen toplantıları sil
                BackgroundJob.Schedule(
                    () => DeleteCancelledMeetingsAsync(),
                    TimeSpan.FromMinutes(delayInMinutes));
                
                _logger.LogInformation($"İptal edilen toplantıları silme job'u {delayInMinutes} dakika sonra çalışacak şekilde planlandı.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İptal edilen toplantıları silme job'u planlanırken hata oluştu.");
            }
        }

        public async Task DeleteCancelledMeetingsAsync()
        {
            try
            {
                // 30 dakikadan fazla önce iptal edilen toplantıları bul
                var cutoffTime = DateTime.UtcNow.AddMinutes(-30);
                
                var cancelledMeetings = await _context.Meetings
                    .Where(m => m.IsCancelled && m.CancelledAt.HasValue && m.CancelledAt.Value <= cutoffTime)
                    .ToListAsync();

                if (cancelledMeetings.Any())
                {
                    _context.Meetings.RemoveRange(cancelledMeetings);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"{cancelledMeetings.Count} adet iptal edilen toplantı silindi.");
                }
                else
                {
                    _logger.LogInformation("Silinecek iptal edilen toplantı bulunamadı.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İptal edilen toplantıları silerken hata oluştu.");
                throw;
            }
        }

        public void ScheduleDeleteSpecificMeeting(int meetingId, int delayInMinutes = 30)
        {
            try
            {
                BackgroundJob.Schedule(
                    () => DeleteSpecificMeetingAsync(meetingId),
                    TimeSpan.FromMinutes(delayInMinutes));
                
                _logger.LogInformation($"Toplantı ID {meetingId} için silme job'u {delayInMinutes} dakika sonra çalışacak şekilde planlandı.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Toplantı ID {meetingId} için silme job'u planlanırken hata oluştu.");
            }
        }

        public async Task DeleteSpecificMeetingAsync(int meetingId)
        {
            try
            {
                var meeting = await _context.Meetings
                    .FirstOrDefaultAsync(m => m.Id == meetingId && m.IsCancelled);

                if (meeting != null)
                {
                    try
                    {
                        var meetingLog = new MeetingManagement.Models.MeetingLog
                        {
                            MeetingId = meeting.Id,
                            Title = meeting.Title,
                            Description = meeting.Description,
                            StartDate = meeting.StartDate,
                            EndDate = meeting.EndDate,
                            DocumentPath = meeting.DocumentPath,
                            UserId = meeting.UserId,
                            Operation = "DELETE",
                            LoggedAt = DateTime.UtcNow,
                            LoggedBy = "BackgroundJobService"
                        };
                        
                        _context.MeetingLogs.Add(meetingLog);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogWarning(logEx, $"Toplantı ID {meetingId} için log kaydı oluşturulamadı, ancak silme işlemi devam edecek.");
                    }
                    

                    _context.Meetings.Remove(meeting);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Toplantı ID {meetingId} başarıyla silindi.");
                }
                else
                {
                    _logger.LogWarning($"Toplantı ID {meetingId} bulunamadı veya iptal edilmemiş.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Toplantı ID {meetingId} silinirken hata oluştu.");
            }
        }
    }
}