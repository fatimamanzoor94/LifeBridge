using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    [Table("ContactLogs")]
    public class ContactLog
    {
        [Key]
        public int LogId { get; set; }

        public int DonorId { get; set; }
        [ForeignKey("DonorId")]
        public User Donor { get; set; }

        public int? AdminId { get; set; }
        [ForeignKey("AdminId")]
        public User Admin { get; set; }

        [Required]
        [StringLength(50)]
        public string ContactType { get; set; }

        public string? Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}