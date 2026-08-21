using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    [Table("BloodIssueHistory")]
    public class BloodIssueHistory
    {
        [Key]
        public int IssueId { get; set; }

        // ✅ NEW: Foreign Key to BloodRequest
        public int? BloodRequestId { get; set; }

        public int HospitalId { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string BloodGroup { get; set; }

        public int UnitsIssued { get; set; }

        public DateTime IssueDate { get; set; }

        [Column(TypeName = "nvarchar(100)")]
        public string IssuedBy { get; set; }

        [Column(TypeName = "nvarchar(200)")]
        public string HospitalName { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Status { get; set; }

        [Column(TypeName = "nvarchar(500)")]
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("BloodRequestId")]
        public virtual BloodRequest BloodRequest { get; set; }

        [ForeignKey("HospitalId")]
        public virtual HospitalProfile Hospital { get; set; }
    }
}