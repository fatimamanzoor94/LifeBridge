using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    [Table("Donations")]
    public class Donation
    {
        [Key]
        public int DonationId { get; set; }

        public int DonorId { get; set; }

        [ForeignKey("DonorId")]
        public User Donor { get; set; }

        // ✅ ADD THESE MISSING PROPERTIES
        public int? HospitalId { get; set; }
        public int? BloodRequestId { get; set; }

        [MaxLength(150)]
        public string? HospitalName { get; set; }

        [MaxLength(5)]
        public string? BloodGroup { get; set; }

        public int? UnitsDonated { get; set; }

        public DateTime DonationDate { get; set; } = DateTime.Now;

        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Completed, Cancelled, etc.
    }
}