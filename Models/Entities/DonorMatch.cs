using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    [Table("DonorMatches")]
    public class DonorMatch
    {
        [Key]
        public int MatchId { get; set; }

        public int BloodRequestId { get; set; }
        [ForeignKey("BloodRequestId")]
        public BloodRequest BloodRequest { get; set; }

        // Maps to Users.UserId as per SQL Schema FK
        public int DonorId { get; set; }
        [ForeignKey("DonorId")]
        public User Donor { get; set; }

        public int MatchScore { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "PotentialMatch";

        public DateTime MatchDate { get; set; } = DateTime.Now;

        // NEW: Response Deadline for assignment expiry (Requirement #13)
        public DateTime? ResponseDeadline { get; set; }

        public int? AdminId { get; set; }
        [ForeignKey("AdminId")]
        public User Admin { get; set; }

        public string? Notes { get; set; }
        public DateTime? DonationScheduledDate { get; set; }
        public DateTime? DonationCompletedDate { get; set; }
        public DateTime? CancelledDate { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        public double DistanceKm { get; set; }
        public double TravelDistance { get; set; }

        [StringLength(50)]
        public string? TravelTime { get; set; }

        public string? ScoreBreakdown { get; set; }

        public bool? EmailSent { get; set; }
        public bool? SmsSent { get; set; }

        public int? HospitalId { get; set; }
        public virtual HospitalProfile Hospital { get; set; }
    }
}