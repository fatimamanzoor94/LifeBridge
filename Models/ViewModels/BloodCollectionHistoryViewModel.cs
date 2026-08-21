// File: Khoon_e_Hayat/ViewModels/BloodCollectionHistoryViewModel.cs

using System;
using System.Collections.Generic;
using Khoon_e_Hayat.Models.Entities;

namespace Khoon_e_Hayat.ViewModels
{
    public class BloodCollectionHistoryViewModel
    {
        // Statistics
        public int TotalCollections { get; set; }
        public int TotalUnitsCollected { get; set; }
        public int TodayCollections { get; set; }
        public int MonthCollections { get; set; }
        public string MostCollectedBloodGroup { get; set; }
        public int ThisWeekCollections { get; set; }

        // Collection List
        public List<BloodInventory> Collections { get; set; } = new();

        // Filters
        public CollectionFilters Filters { get; set; } = new();

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;

        // Available options for filters
        public List<string> AvailableBloodGroups { get; set; } = new()
        {
            "A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-"
        };

        public List<string> AvailableStatuses { get; set; } = new()
        {
            "Available", "Reserved", "Used", "Expired"
        };
    }

    public class CollectionFilters
    {
        public string SearchQuery { get; set; } = string.Empty;
        public string BloodGroup { get; set; } = "all";
        public string Status { get; set; } = "all";
        public string SortBy { get; set; } = "newest";
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}