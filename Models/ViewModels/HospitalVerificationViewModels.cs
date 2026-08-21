using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    // ViewModel for the Table List
    public class HospitalVerificationViewModel
    {
        public int UserId { get; set; }
        public string HospitalName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? LicenseNumber { get; set; }
        public string? ContactPerson { get; set; }
        public string? City { get; set; }
        public DateTime SubmittedDate { get; set; }
        public string VerificationStatus { get; set; } = "Pending";
    }

    // ViewModel for the View Details Modal
    public class HospitalVerificationDetailsViewModel
    {
        public int UserId { get; set; }
        public string HospitalName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? LicenseNumber { get; set; }
        public string? ContactPerson { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public DateTime SubmittedDate { get; set; }
        public string VerificationStatus { get; set; } = "Pending";
        public string? LicenseDocumentPath { get; set; }
        public string? RejectionReason { get; set; }
    }

    // ViewModel for the Page (Statistics + List)
    public class HospitalVerificationListViewModel
    {
        public List<HospitalVerificationViewModel> Requests { get; set; } = new List<HospitalVerificationViewModel>();

        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int TodayCount { get; set; }
        public int TotalCount { get; set; }

        public List<string> AvailableCities { get; set; } = new List<string>();
    }
}