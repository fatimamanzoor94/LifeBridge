using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class ReceiverTrackRequestsViewModel
    {
        public TrackingStatisticsViewModel Statistics { get; set; } = new();
        public TrackingFiltersViewModel Filters { get; set; } = new();
        public List<TrackingCardViewModel> Requests { get; set; } = new();
        public List<string> AvailableBloodGroups { get; set; } = new();
        public List<string> AvailableCities { get; set; } = new();
        public List<string> AvailableHospitals { get; set; } = new();
        public int TotalCount { get; set; }
        public DateTime CurrentDate => DateTime.Now;
    }

    public class TrackingStatisticsViewModel
    {
        public int ActiveRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int PendingRequests { get; set; }
        public string AverageProcessingTime { get; set; } = "N/A";
    }

    public class TrackingFiltersViewModel
    {
        public string SearchQuery { get; set; }
        public string BloodGroup { get; set; } = "all";
        public string Status { get; set; } = "all";
        public string Priority { get; set; } = "all";
        public string Hospital { get; set; } = "all";
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }

    public class TrackingCardViewModel
    {
        public int RequestId { get; set; }
        public string RequestCode { get; set; }

        // Patient
        public string PatientName { get; set; }
        public int? PatientAge { get; set; }
        public string Gender { get; set; }
        public string BloodGroup { get; set; }
        public int UnitsRequired { get; set; }

        // Hospital
        public string HospitalName { get; set; }
        public string HospitalCity { get; set; }
        public string HospitalAddress { get; set; }
        public string HospitalContact { get; set; }

        // Status & Progress
        public string OriginalStatus { get; set; }
        public string DisplayStatus { get; set; }
        public string StatusColorClass { get; set; }
        public int ProgressPercentage { get; set; }
        public string UrgencyLevel { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RequiredDate { get; set; }
        public string EstimatedCompletion { get; set; }
        public string LastUpdatedText { get; set; }
        public bool IsLive { get; set; }
        public string CurrentStage { get; set; }
        public int MatchedDonorsCount { get; set; }

        // Timeline & Logs
        public List<TimelineStageViewModel> Timeline { get; set; } = new();
        public List<ActivityLogViewModel> ActivityLogs { get; set; } = new();
    }

    public class TimelineStageViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } // completed, current, pending
        public string Icon { get; set; }
    }

    public class ActivityLogViewModel
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string IconColor { get; set; }
        public string TimeAgo { get; set; }
    }
}