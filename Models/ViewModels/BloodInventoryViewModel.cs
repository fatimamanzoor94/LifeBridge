using System;
using System.Collections.Generic;
using Khoon_e_Hayat.Models.Entities;

namespace Khoon_e_Hayat.ViewModels
{
    public class BloodInventoryViewModel
    {
        public int TotalUnits { get; set; }
        public int AvailableUnits { get; set; }
        public int ExpiredUnits { get; set; }
        public int LowStockUnits { get; set; }
        public int OutOfStockUnits { get; set; }
        public int ExpiringSoonUnits { get; set; }
        public int AvailableBloodGroups { get; set; }

        public List<HospitalProfile> AvailableHospitals { get; set; } = new();

        public List<BloodInventory> Inventory { get; set; } = new();
        public InventoryFilters Filters { get; set; } = new();

        // Pagination Properties
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;

        public List<string> AvailableBloodGroupsList { get; set; } = new()
        {
            "A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-"
        };

        // Ensured consistency with the statuses used in the app
        public List<string> AvailableStatuses { get; set; } = new()
        {
            "Available", "Low Stock", "Critical", "Expired"
        };
    }

    public class InventoryFilters
    {
        public string SearchQuery { get; set; } = string.Empty;
        public string BloodGroup { get; set; } = "all";
        public string Status { get; set; } = "all";
        public string SortBy { get; set; } = "newest";
    }

    public class BloodInventoryAddViewModel
    {
        public int InventoryId { get; set; }

        public int HospitalId { get; set; }
        public string BloodGroup { get; set; }
        public int Quantity { get; set; }
        public DateTime CollectionDate { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}