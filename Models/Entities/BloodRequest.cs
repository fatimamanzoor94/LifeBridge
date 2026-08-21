using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    [Table("BloodRequests")]
    public class BloodRequest
    {
        [Key]
        public int RequestId { get; set; }

        [ForeignKey("Receiver")]
        public int ReceiverId { get; set; }

        // ✅ FIXED: Added '?' for nullable navigation property
        public virtual User? Receiver { get; set; }

        [StringLength(5)]
        public string? BloodGroup { get; set; }

        public int? UnitsRequired { get; set; }
        [StringLength(150)]
        public string? HospitalName { get; set; }
        [StringLength(100)]
        public string? City { get; set; }
        [StringLength(20)]
        public string? UrgencyLevel { get; set; }
        [StringLength(20)]
        public string RequestStatus { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? RequiredDate { get; set; }
        public string? HospitalContact { get; set; }
        public string? PatientName { get; set; }
        public int? PatientAge { get; set; }
        [StringLength(255)]
        public string? Address { get; set; }
        [StringLength(500)]
        public string? Reason { get; set; }
        [StringLength(1000)]
        public string? AdditionalNotes { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int? HospitalId { get; set; }

        // ✅ FIXED: Added '?' for nullable navigation property
        public virtual HospitalProfile? Hospital { get; set; }
    }
}