using System.ComponentModel.DataAnnotations;

namespace MeetingManagement.Models.DTOs
{
    // Yeni toplantı oluşturmak için gerekli bilgiler
    public class CreateMeetingDto
    {
        [Required(ErrorMessage = "Toplantı başlığı gereklidir")]
        [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir")]
        public string Title { get; set; }

        [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Başlangıç tarihi gereklidir")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Bitiş tarihi gereklidir")]
        public DateTime EndDate { get; set; }

        // Dosya yolu opsiyonel
        public string DocumentPath { get; set; }
    }
}