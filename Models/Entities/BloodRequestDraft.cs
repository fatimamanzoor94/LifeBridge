using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    [Table("BloodRequestDrafts")]
    public class BloodRequestDraft
    {
        [Key]
        public int DraftId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string DraftType { get; set; } // "ReceiverRequest", "DonorRegistration", etc.

        [Required]
        public string DraftData { get; set; } // JSON string

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}