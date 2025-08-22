namespace MeetingManagement.Models.DTOs
{
    // Toplantı bilgilerini döndürmek için DTO
    public class MeetingResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string DocumentPath { get; set; }
        public bool IsCancelled { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Toplantıyı oluşturan kullanıcı bilgileri
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
    }

    // API response wrapper
    public class MeetingApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public MeetingResponseDto Data { get; set; }
    }

    // Liste response için
    public class MeetingListResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<MeetingResponseDto> Data { get; set; }
        public int TotalCount { get; set; }
    }
}