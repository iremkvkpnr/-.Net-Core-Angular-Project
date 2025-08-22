using System.ComponentModel.DataAnnotations;

namespace MeetingManagement.Models
{
    public class MeetingLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MeetingId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [StringLength(255)]
        public string? DocumentPath { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Operation { get; set; } = string.Empty; // INSERT, UPDATE, DELETE

        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? LoggedBy { get; set; }
    }
}