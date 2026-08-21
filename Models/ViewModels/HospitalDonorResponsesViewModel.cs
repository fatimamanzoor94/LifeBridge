// ViewModels/HospitalDonorResponsesViewModel.cs
using Khoon_e_Hayat.Models.Entities;

namespace Khoon_e_Hayat.ViewModels
{
    public class HospitalDonorResponsesViewModel
    {
        public int TotalResponses { get; set; }
        public int PendingResponses { get; set; }
        public int AcceptedDonors { get; set; }
        public int RejectedResponses { get; set; }
        public int CompletedDonations { get; set; }
        public int ScheduledDonations { get; set; }

        public List<DonorResponseItem> Responses { get; set; } = new();
        public DonorResponseFilters Filters { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }

        public List<string> AvailableBloodGroups { get; set; } = new();
        public List<string> AvailableStatuses { get; set; } = new();
    }

    public class DonorResponseItem
    {
        public int MatchId { get; set; }
        public int DonorId { get; set; }
        public int RequestId { get; set; }
        public string DonorName { get; set; } = "";
        public string DonorProfilePicture { get; set; } = "";
        public string DonorBloodGroup { get; set; } = "";
        public string DonorPhone { get; set; } = "";
        public string DonorEmail { get; set; } = "";
        public string ReceiverName { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string BloodGroupRequired { get; set; } = "";
        public int UnitsRequired { get; set; }
        public string HospitalName { get; set; } = "";
        public DateTime AssignedDate { get; set; }
        public DateTime? ResponseDate { get; set; }
        public DateTime? DonationScheduledDate { get; set; }
        public DateTime? DonationCompletedDate { get; set; }
        public string Status { get; set; } = "";
        public string StatusDisplay { get; set; } = "";
        public string StatusBadgeClass { get; set; } = "";
        public string Notes { get; set; } = "";
        public string RejectionReason { get; set; } = "";
        public bool EmailSent { get; set; }
        public bool SmsSent { get; set; }
        public string RequestUrgency { get; set; } = "";
        public int MatchScore { get; set; }
        public DateTime? ResponseDeadline { get; set; }
    }

    public class DonorResponseFilters
    {
        public string SearchQuery { get; set; } = "";
        public string Status { get; set; } = "all";
        public string BloodGroup { get; set; } = "all";
        public string SortBy { get; set; } = "newest";
    }
}