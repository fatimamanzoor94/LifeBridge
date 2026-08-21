using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    [Table("AdminLogs")]
    public class AdminLog
    {
        [Key]
        public int LogId { get; set; }
        public int AdminId { get; set; }

        [MaxLength(255)]
        public string? Action { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("AdminId")]
        public User Admin { get; set; }
    }
}