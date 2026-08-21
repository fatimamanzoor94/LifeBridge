using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class EmergencyRequestsViewModel
    {
        // Statistics
        public int TotalEmergencyRequests { get; set; }
        public int PendingEmergencyRequests { get; set; }
        public int BloodAvailableRequests { get; set; }
        public int DonorSearchRequired { get; set; }
        public int ApprovedEmergencyRequests { get; set; }
        public int CompletedEmergencyRequests { get; set; }

        // Progress Overview Percentages
        public int PendingPercentage { get; set; }
        public int ApprovedPercentage { get; set; }
        public int CompletedPercentage { get; set; }
        public int DonorSearchPercentage { get; set; }

        // Data & Filters
        public List<EmergencyRequestItem> Requests { get; set; } = new();
        public EmergencyRequestFilters Filters { get; set; } = new();

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;

        // Filter Options
        public List<string> AvailableBloodGroups { get; set; } = new() { "A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-" };
        public List<string> AvailableStatuses { get; set; } = new() { "Pending", "Approved", "Searching Donor", "Blood Reserved", "Completed", "Rejected" };
        public List<string> AvailablePriorities { get; set; } = new() { "Critical", "Emergency", "High" };
        public List<string> AvailableAvailabilities { get; set; } = new() { "Available", "Low Stock", "Not Available" };
        public List<string> AvailableCities { get; set; } = new();
    }

    public class EmergencyRequestItem
    {
        public int RequestId { get; set; }
        public string ReceiverName { get; set; }
        public string BloodGroup { get; set; }
        public int RequiredUnits { get; set; }
        public DateTime RequiredBefore { get; set; }
        public string City { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public bool IsBloodAvailable { get; set; }
        public int AvailableStock { get; set; }
        public int MinutesRemaining { get; set; }
        public string TimerText { get; set; }
        public string Reason { get; set; }
        public string HospitalName { get; set; }
        public string ContactNumber { get; set; }
        public DateTime RequestDate { get; set; }
    }

    public class EmergencyRequestFilters
    {
        public string SearchQuery { get; set; } = string.Empty;
        public string Status { get; set; } = "all";
        public string Priority { get; set; } = "all";
        public string BloodGroup { get; set; } = "all";
        public string Availability { get; set; } = "all";
        public string City { get; set; } = "all";
        public string SortBy { get; set; } = "newest";
    }
}