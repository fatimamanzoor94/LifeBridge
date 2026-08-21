using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    public class EmergencyAlert
    {
        [Key]
        public int AlertId { get; set; }

        [ForeignKey("BloodRequest")]
        public int RequestId { get; set; }
        public virtual BloodRequest BloodRequest { get; set; }

        public string AlertMessage { get; set; }

        [StringLength(20)]
        public string PriorityLevel { get; set; }

        [StringLength(20)]
        public string AlertStatus { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}