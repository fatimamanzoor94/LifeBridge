using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class DonorSmartMatchViewModel
    {
        // Cooldown Properties
        public bool IsInCooldown { get; set; }
        public DateTime? NextEligibleDate { get; set; }
        public int DaysUntilEligible { get; set; }

        public int TotalMatches { get; set; }
        public int PerfectMatches { get; set; }
        public int NearbyRequests { get; set; }
        public int MatchSuccessRate { get; set; }

        // Quick Statistics
        public int AcceptedMatches { get; set; }
        public int CompletedDonations { get; set; }
        public int AverageCompatibilityScore { get; set; }

        public List<DonorSmartMatchItem> Matches { get; set; } = new();
        public List<string> AvailableCities { get; set; } = new();
        public List<string> AvailableHospitals { get; set; } = new();
    }

    public class DonorSmartMatchItem
    {
        public int MatchId { get; set; }
        public int RequestId { get; set; }
        public string BloodGroup { get; set; }
        public string ReceiverName { get; set; }
        public string HospitalName { get; set; }
        public string HospitalAddress { get; set; }
        public string City { get; set; }
        public double DistanceKm { get; set; }
        public int UnitsRequired { get; set; }
        public string UrgencyLevel { get; set; }
        public int MatchScore { get; set; }
        public string CompatibilityBadge { get; set; } // Perfect, Excellent, Good, Low
        public DateTime RequestDate { get; set; }
        public DateTime? RequiredBeforeTime { get; set; }
        public string Status { get; set; } // Available, Accepted, Assigned, Expired, Cancelled
        public string Compatibility { get; set; }
        public string EligibilityStatus { get; set; }
        public string TravelTime { get; set; }
        public string PriorityLevel { get; set; }

        // AI Features
        public List<string> MatchReasons { get; set; } = new();
        public string AdditionalNotes { get; set; }
    }
}