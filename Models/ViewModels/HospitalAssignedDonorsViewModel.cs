// File: Khoon_e_Hayat/ViewModels/HospitalAssignedDonorsViewModel.cs

using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class HospitalAssignedDonorsViewModel
    {
        // Statistics
        public int TotalAssignedDonors { get; set; }
        public int PendingResponses { get; set; }
        public int AcceptedDonors { get; set; }
        public int CompletedDonations { get; set; }
        public int CancelledAssignments { get; set; }
        public int RejectedAssignments { get; set; }

        // Assigned Donors List
        public List<AssignedDonorItem> AssignedDonors { get; set; } = new();

        // Filters
        public AssignedDonorFilters Filters { get; set; } = new();

        // Available options for filters
        public List<string> AvailableBloodGroups { get; set; } = new()
        {
            "A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-"
        };

        public List<string> AvailableStatuses { get; set; } = new()
        {
            "PotentialMatch", "RequestSent", "Accepted", "Rejected",
            "DonationScheduled", "Completed", "Cancelled"
        };

        // ==================== ADD THESE PAGINATION PROPERTIES ====================
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        // ========================================================================
    }

    public class AssignedDonorItem
    {
        public int MatchId { get; set; }
        public int DonorId { get; set; }
        public int RequestId { get; set; }
        public string DonorName { get; set; }
        public string DonorProfilePicture { get; set; }
        public string DonorBloodGroup { get; set; }
        public string DonorCity { get; set; }
        public string DonorPhone { get; set; }
        public string DonorEmail { get; set; }
        public string ReceiverName { get; set; }
        public string PatientName { get; set; }
        public string BloodGroupRequired { get; set; }
        public int UnitsRequired { get; set; }
        public string HospitalName { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? DonationScheduledDate { get; set; }
        public DateTime? DonationCompletedDate { get; set; }
        public string Status { get; set; }
        public string StatusDisplay { get; set; }
        public string StatusBadgeClass { get; set; }
        public int MatchScore { get; set; }
        public double DistanceKm { get; set; }
        public string TravelTime { get; set; }
        public string Notes { get; set; }
        public string RejectionReason { get; set; }
        public bool EmailSent { get; set; }
        public bool SmsSent { get; set; }
        public string RequestStatus { get; set; }
        public string RequestUrgency { get; set; }
    }

    public class AssignedDonorFilters
    {
        public string SearchQuery { get; set; } = string.Empty;
        public string Status { get; set; } = "all";
        public string BloodGroup { get; set; } = "all";
        public string SortBy { get; set; } = "recent";
    }
}