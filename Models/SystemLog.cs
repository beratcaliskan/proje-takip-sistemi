using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjeTakip.Models
{
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string LogType { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string LogContent { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Executor { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties için ek alanlar
        [StringLength(200)]
        public string? AdditionalInfo { get; set; }

        [StringLength(50)]
        public string? IpAddress { get; set; }

        [StringLength(200)]
        public string? UserAgent { get; set; }
    }
}