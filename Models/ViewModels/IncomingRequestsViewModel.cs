// File: Khoon_e_Hayat/ViewModels/IncomingRequestsViewModel.cs

using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class IncomingRequestsViewModel
    {
        // Statistics
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int EmergencyRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int RejectedRequests { get; set; }

        // Requests List
        public List<IncomingRequestItem> Requests { get; set; } = new();

        // Emergency Requests
        public List<IncomingRequestItem> EmergencyRequestList { get; set; } = new();

        // Filters
        public IncomingRequestFilters Filters { get; set; } = new();

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
            "Pending", "Approved", "Completed", "Rejected", "In Progress"
        };

        public List<string> AvailablePriorities { get; set; } = new()
        {
            "Normal", "High", "Critical"
        };
    }

    public class IncomingRequestItem
    {
        public int RequestId { get; set; }
        public string ReceiverName { get; set; }
        public string BloodGroup { get; set; }
        public int RequiredUnits { get; set; }
        public DateTime RequestDate { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public string HospitalName { get; set; }
        public string City { get; set; }
        public string PatientName { get; set; }
        public string Reason { get; set; }
        public string ContactNumber { get; set; }
        public bool IsBloodAvailable { get; set; }
        public int AvailableStock { get; set; }
    }

    public class IncomingRequestFilters
    {
        public string SearchQuery { get; set; } = string.Empty;
        public string Status { get; set; } = "all";
        public string Priority { get; set; } = "all";
        public string BloodGroup { get; set; } = "all";
        public string SortBy { get; set; } = "newest";
    }
}