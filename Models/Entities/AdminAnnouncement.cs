using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    [Table("AdminAnnouncements")]
    public class AdminAnnouncement
    {
        [Key]
        public int AnnouncementId { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        [MaxLength(255)]
        public string ActionUrl { get; set; } // Optional link for "Read More"

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}