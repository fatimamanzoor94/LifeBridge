// File: Khoon_e_Hayat/ViewModels/LowStockMonitorViewModel.cs

using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class LowStockMonitorViewModel
    {
        // Statistics
        public int TotalBloodGroups { get; set; }
        public int HealthyStockGroups { get; set; }
        public int LowStockGroups { get; set; }
        public int CriticalStockGroups { get; set; }
        public int OutOfStockGroups { get; set; }
        public int TotalAvailableUnits { get; set; }

        // Blood Group Stock List
        public List<BloodGroupStockItem> BloodGroupStock { get; set; } = new();

        // Filters
        public LowStockFilters Filters { get; set; } = new();

        // Available options for filters
        public List<string> AvailableBloodGroups { get; set; } = new()
        {
            "A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-"
        };

        public List<string> AvailableStatuses { get; set; } = new()
        {
            "Healthy", "Low Stock", "Critical", "Out of Stock"
        };
    }

    public class BloodGroupStockItem
    {
        public string BloodGroup { get; set; }
        public int TotalUnits { get; set; }
        public int ReorderLevel { get; set; }
        public string Status { get; set; }
        public string StatusColorClass { get; set; }
        public string RecommendedAction { get; set; }
        public DateTime LastUpdated { get; set; }
        public int PercentageRemaining { get; set; }
    }

    public class LowStockFilters
    {
        public string SearchQuery { get; set; } = string.Empty;
        public string Status { get; set; } = "all";
        public string SortBy { get; set; } = "lowest";
    }
}