using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Khoon_e_Hayat.ViewModels
{
    public class ExpiringBloodViewModel
    {
        // Statistics
        public int TotalBloodUnits { get; set; }
        public int SafeUnits { get; set; }
        public int ExpiringSoonUnits { get; set; }
        public int ExpiredUnits { get; set; }
        public int ExpiringThisWeekUnits { get; set; }
        public int ExpiringTodayUnits { get; set; }

        // Blood Items
        public List<ExpiringBloodItem> ExpiringBlood { get; set; } = new List<ExpiringBloodItem>();

        // Filters
        public ExpiringBloodFilters Filters { get; set; } = new ExpiringBloodFilters();

        // Pagination Properties (THESE WERE MISSING)
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    public class ExpiringBloodItem
    {
        public int InventoryId { get; set; }
        public string BloodGroup { get; set; }
        public int AvailableUnits { get; set; }
        public DateTime CollectionDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int DaysRemaining { get; set; }
        public int ExpiryPercentage { get; set; }
        public string StorageLocation { get; set; }
        public string ExpiryStatus { get; set; }
        public string StatusColorClass { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class ExpiringBloodFilters
    {
        public string SearchQuery { get; set; }
        public string ExpiryStatus { get; set; }
        public string SortBy { get; set; } = "nearest";
    }
}