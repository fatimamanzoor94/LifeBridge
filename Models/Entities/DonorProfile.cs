using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    [Table("DonorProfiles")]
    public class DonorProfile
    {
        [Key]
        public int DonorId { get; set; }

        public int UserId { get; set; }

        [MaxLength(5)]
        public string? BloodGroup { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        public DateTime? LastDonationDate { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        // NEW: Area property for donor's current area/locality
        [MaxLength(100)]
        public string? Area { get; set; }

        public int? Weight { get; set; }

        public bool IsAvailable { get; set; } = true;

        [MaxLength(100)]
        public string? PreferredArea { get; set; }

        // ==================== REAL GPS & STATS (From SQL Schema) ====================
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [MaxLength(20)]
        public string? OnlineStatus { get; set; }

        public int? SuccessfulDonations { get; set; }
        public double? AcceptanceRate { get; set; }
        public double? ResponseRate { get; set; }
        // ===========================================================================

        [MaxLength(500)]
        public string? Notes { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}