using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    public class HospitalProfile
    {
        [Key]
        public int HospitalId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }

        public string? HospitalName { get; set; }
        public string? LicenseNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? ContactPerson { get; set; }

        public string? LogoUrl { get; set; }
        public bool IsVerified { get; set; }
        public string? LicenseDocumentPath { get; set; }
        public string? VerificationStatus { get; set; } = "Pending";
        public string? RejectionReason { get; set; }

        // ✅ ADD THESE MISSING PROPERTIES
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}