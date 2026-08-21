using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    [Table("ReceiverNotifications")]
    public class ReceiverNotification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [ForeignKey("ReceiverId")]
        public virtual User Receiver { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        // Categories: BloodRequest, Hospital, Donor, BloodReserved, BloodReady,
        //             Completed, Rejected, Emergency, Reminder, Information, System, Success
        [Required, MaxLength(50)]
        public string Category { get; set; }

        // Priority: High, Medium, Low
        [MaxLength(20)]
        public string Priority { get; set; } = "Medium";

        // Optional link to a BloodRequest
        public int? RequestId { get; set; }

        // Optional display fields (denormalized for fast rendering)
        [MaxLength(150)]
        public string? HospitalName { get; set; }

        [MaxLength(5)]
        public string? BloodGroup { get; set; }

        [MaxLength(30)]
        public string? RequestStatus { get; set; }

        [MaxLength(255)]
        public string? ActionUrl { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}