using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; }

        // ✅ NULLABLE - Database mein NULL ho sakta hai
        [MaxLength(150)]
        public string? Email { get; set; }

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; }

        [Required, MaxLength(20)]
        public string Role { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(255)]
        public string? ProfilePicture { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsEmailVerified { get; set; } = false;

        [MaxLength(255)]
        public string? EmailVerificationToken { get; set; }

        public DateTime? EmailVerificationTokenExpiry { get; set; }

        public bool IsApproved { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [MaxLength(255)]
        public string? ResetToken { get; set; }

        public DateTime? ResetTokenExpiry { get; set; }

        // Navigation Properties
        public DonorProfile? DonorProfile { get; set; }
        public ReceiverProfile? ReceiverProfile { get; set; }
        public HospitalProfile? HospitalProfile { get; set; }

        public ICollection<BloodRequest> BloodRequests { get; set; } = new List<BloodRequest>();
        public ICollection<Donation> Donations { get; set; } = new List<Donation>();
        public ICollection<AdminLog> AdminLogs { get; set; } = new List<AdminLog>();

        // NEW FIELDS FOR PROFILE MODULE
        public string? CNIC { get; set; }
        public string? Country { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}