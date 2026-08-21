using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    [Table("HospitalNotifications")]
    public class HospitalNotification
    {
        [Key]
        public int NotificationId { get; set; }

        public int HospitalId { get; set; }

        public virtual HospitalProfile? Hospital { get; set; }

        [StringLength(200)]
        public string? Title { get; set; }

        public string? Message { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [StringLength(20)]
        public string? Priority { get; set; } = "Medium";

        public int? RequestId { get; set; }

        public int? DonorId { get; set; }

        public string? ActionUrl { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}