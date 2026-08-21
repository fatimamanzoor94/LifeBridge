using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    [Table("ReceiverProfiles")]
    public class ReceiverProfile
    {
        [Key]
        public int ReceiverId { get; set; }
        public int UserId { get; set; }

        [MaxLength(5)]
        public string? BloodGroupNeeded { get; set; }

        [MaxLength(20)]
        public string? UrgencyLevel { get; set; }

        [MaxLength(150)]
        public string? HospitalName { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        public DateTime? RequiredDate { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}