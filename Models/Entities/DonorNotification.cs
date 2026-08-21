using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    [Table("DonorNotifications")]
    public class DonorNotification
    {
        [Key]
        public int NotificationId { get; set; }

        public int DonorId { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        // Categories: "EmergencyAlert", "SmartMatch", "DonationUpdate", "Eligibility", "System"
        [Required, MaxLength(50)]
        public string Category { get; set; }

        // Links to RequestId, MatchId, etc.
        public int? ReferenceId { get; set; }

        [MaxLength(255)]
        public string ActionUrl { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
