using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Khoon_e_Hayat.Models.Entities
{
    [Table("BloodInventory")]
    public class BloodInventory
    {
        [Key]
        public int InventoryId { get; set; }

        public int HospitalId { get; set; }
        public virtual HospitalProfile Hospital { get; set; }

        [StringLength(10)]
        public string BloodGroup { get; set; }

        public int Quantity { get; set; }
        public int ReorderLevel { get; set; } = 10;
        public DateTime CollectionDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Status { get; set; } = "Available"; // Available, Reserved, Used, Expired
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}