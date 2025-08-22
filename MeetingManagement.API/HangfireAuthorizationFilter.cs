using Hangfire.Dashboard;

namespace MeetingManagement.API
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // Development ortamında herkese izin ver
            // Production'da daha güvenli bir yetkilendirme mekanizması kullanılmalı
            var httpContext = context.GetHttpContext();
            
            // Development ortamında true döndür
            if (httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                return true;
            }
            
            // Production ortamında JWT token kontrolü yapılabilir
            // Şimdilik development için basit bir kontrol
            return true;
        }
    }
}