// File: Khoon_e_Hayat/ViewModels/BloodIssueHistoryViewModel.cs

using System;
using System.Collections.Generic;
using Khoon_e_Hayat.Models.Entities;

namespace Khoon_e_Hayat.ViewModels
{
    public class BloodIssueHistoryViewModel
    {
        // Statistics
        public int TotalIssues { get; set; }
        public int TotalUnitsIssued { get; set; }
        public int TodayIssues { get; set; }
        public int MonthIssues { get; set; }
        public int SuccessfulDeliveries { get; set; }
        public string MostIssuedBloodGroup { get; set; }
        public int ThisWeekIssues { get; set; }

        // Issue History List
        public List<BloodIssueHistoryItem> Issues { get; set; } = new();

        // Filters
        public IssueHistoryFilters Filters { get; set; } = new();

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
            "Completed", "Pending", "Delivered", "Cancelled", "Rejected"
        };
    }

    public class BloodIssueHistoryItem
    {
        public int IssueId { get; set; }
        public int? RequestId { get; set; }
        public string ReceiverName { get; set; }
        public string BloodGroup { get; set; }
        public int UnitsIssued { get; set; }
        public DateTime IssueDate { get; set; }
        public string IssuedBy { get; set; }
        public string HospitalName { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
    }

    public class IssueHistoryFilters
    {
        public string SearchQuery { get; set; } = string.Empty;
        public string BloodGroup { get; set; } = "all";
        public string Status { get; set; } = "all";
        public string SortBy { get; set; } = "newest";
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? RequestId { get; set; }
    }
}