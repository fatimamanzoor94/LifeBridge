using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class AssignedDonorsViewModel
    {
        public int TotalAssigned { get; set; }
        public int PendingResponse { get; set; }
        public int AcceptedOrScheduled { get; set; }
        public int Completed { get; set; }
        public int RejectedOrExpired { get; set; }

        public List<AssignedDonorItemViewModel> Donors { get; set; } = new();
        public AssignedDonorFilters Filters { get; set; } = new();

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class AssignedDonorItemViewModel
    {
        public int MatchId { get; set; }
        public int DonorId { get; set; }
        public string DonorName { get; set; } = "Unknown";
        public string DonorBloodGroup { get; set; } = "Unknown";
        public int RequestId { get; set; }
        public string PatientName { get; set; } = "Unknown";
        public int UnitsRequired { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? ResponseDeadline { get; set; }
        public string Status { get; set; } = "Unknown";
        public string StatusDisplay { get; set; } = "Unknown";
        public string StatusBadgeClass { get; set; } = "badge-normal";
        public int MatchScore { get; set; }
        public DateTime? DonationScheduledDate { get; set; }
        public string? Notes { get; set; }
    }

}