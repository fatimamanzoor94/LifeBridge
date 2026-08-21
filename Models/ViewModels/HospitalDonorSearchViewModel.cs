using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class HospitalDonorSearchViewModel
    {
        public BloodRequestSummary? SelectedRequest { get; set; }

        public int TotalRegisteredDonors { get; set; }
        public int AvailableDonors { get; set; }
        public int MatchingDonors { get; set; }
        public int AssignedDonors { get; set; }
        public int EmergencyMatches { get; set; }
        public int EligibleDonors { get; set; }
        public int DonorsContacted { get; set; }

        public List<HospitalDonorResult> Donors { get; set; } = new();
        public List<HospitalDonorResult> SuggestedDonors { get; set; } = new();

        public DonorSearchFilters Filters { get; set; } = new();

        public List<string> AvailableBloodGroups { get; set; } = new() { "A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-" };
        public List<string> AvailableCities { get; set; } = new();
        public List<string> AvailableAreas { get; set; } = new();

        public bool IsBloodInventorySufficient { get; set; }
        public int CurrentStockUnits { get; set; }
        public string RequiredBloodGroup { get; set; } = "";

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class HospitalDonorResult
    {
        public int DonorId { get; set; }
        public string FullName { get; set; } = "";
        public string ProfilePicture { get; set; } = "";
        public string BloodGroup { get; set; } = "";
        public string City { get; set; } = "";
        public string Area { get; set; } = "";

        // Enhanced Profile Fields (Requirement 17)
        public string Gender { get; set; } = "";
        public int? Age { get; set; }
        public int? Weight { get; set; }  // ✅ ADDED - Fixes CS1061 error
        public DateTime RegistrationDate { get; set; }
        public DateTime? NextEligibleDate { get; set; }  // ✅ ADDED - Fixes CS1061 error
        public string AverageResponseTime { get; set; } = "N/A";

        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsAvailable { get; set; }
        public DateTime? LastDonationDate { get; set; }
        public int TotalDonations { get; set; }
        public bool IsVerified { get; set; }
        public bool IsEligibleToDonate { get; set; }

        public int MatchScore { get; set; }
        public string MatchQuality { get; set; } = "Fair Match";

        public List<string> MatchReasons { get; set; } = new();
        public string AvailabilityStatus { get; set; } = "Unknown";
        public string Address { get; set; } = "";

        public int PreviousHospitalDonations { get; set; }
        public string CommunicationStatus { get; set; } = "None";
        public DateTime? ResponseDeadline { get; set; }

        public double DistanceKm { get; set; }
        public string TravelTime { get; set; } = "N/A";

        public int TotalRequestsReceived { get; set; }
        public int AcceptedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int ExpiredRequests { get; set; }
        public double AcceptanceRate { get; set; }
    }

    public class DonorSearchFilters
    {
        public string SearchQuery { get; set; } = string.Empty;
        public string BloodGroup { get; set; } = "all";
        public string City { get; set; } = "all";
        public string Area { get; set; } = "all";
        public string AvailabilityStatus { get; set; } = "all";
        public string Gender { get; set; } = "all";
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public string SortBy { get; set; } = "bestmatch"; // bestmatch, nearest, recentlyactive, highestacceptance, mostdonations, prevhospital, highestscore
    }

    public class BackupDonorDto
    {
        public string Name { get; set; } = "";
        public string BloodGroup { get; set; } = "";
        public int Score { get; set; }
        public double DistanceKm { get; set; }
    }
}