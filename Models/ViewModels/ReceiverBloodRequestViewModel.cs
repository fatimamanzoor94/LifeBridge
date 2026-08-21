using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class ReceiverBloodRequestViewModel
    {
        public List<ReceiverBloodRequestItem> Requests { get; set; } = new();

        // ADD THESE PAGINATION PROPERTIES
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;

        // Statistics - FIXED: Added missing properties
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ActiveRequests { get; set; }
        public int CompletedRequests { get; set; }

        // Keep these for compatibility if needed
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int FulfilledCount { get; set; }
        public int EmergencyCount { get; set; }
        public int ActiveCitiesCount { get; set; }

        // Filters
        public List<string> AvailableCities { get; set; } = new();
    }

    public class ReceiverBloodRequestItem
    {
        public int RequestId { get; set; }
        public string BloodGroup { get; set; }
        public int UnitsRequired { get; set; }
        public string HospitalName { get; set; }
        public string City { get; set; }
        public string UrgencyLevel { get; set; }
        public string RequestStatus { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? RequiredDate { get; set; }

        // FIXED: Added missing properties
        public string PatientName { get; set; }
        public string HospitalAddress { get; set; }
        public string Notes { get; set; }
        public int MatchedDonorsCount { get; set; }
    }
}