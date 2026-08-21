using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    [Table("NotificationLogs")]
    public class NotificationLog
    {
        [Key]
        public int LogId { get; set; }

        // Link to specific requests/alerts if applicable
        public int? RequestId { get; set; }
        public int? AlertId { get; set; }
        public int? DonorId { get; set; }

        [MaxLength(150)]
        public string? RecipientEmail { get; set; }

        [MaxLength(20)]
        public string? RecipientPhone { get; set; }

        [Required, MaxLength(20)]
        public string NotificationType { get; set; } // "Email", "SMS"

        [Required, MaxLength(50)]
        public string Category { get; set; } // "MatchFound", "EmergencyAlert", "DonorAssigned", "RequestCancelled"

        [MaxLength(255)]
        public string? Subject { get; set; }

        public string Message { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending"; // "Sent", "Failed", "Pending"

        public string? ErrorMessage { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}