using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class SmartMatchViewModel
    {
        public int TotalMatchRequests { get; set; }
        public int TodayMatches { get; set; }
        public int ActiveMatches { get; set; }
        public int SuccessfulMatches { get; set; }
        public int FailedMatches { get; set; }
        public int PendingRequests { get; set; }
        public int AvgMatchScore { get; set; }
        public double AvgDistance { get; set; }
        public int AvailableDonors { get; set; }
        public int BusyDonors { get; set; }
        public int EmergencyMatches { get; set; }
        public int ResponseRate { get; set; }
        public int AcceptanceRate { get; set; }
        public int CompletionRate { get; set; }

        public List<string> AvailableCities { get; set; } = new();
        public List<DonorMatchResultViewModel> MatchedDonors { get; set; } = new();

        public bool IsEmergencyMode { get; set; }
        public int? EmergencyAlertId { get; set; }
        public string EmergencyBloodGroup { get; set; }
        public string EmergencyCity { get; set; }
        public string EmergencyHospital { get; set; }
        public string EmergencyUrgency { get; set; }
        public int EmergencyUnits { get; set; }
        public DateTime? EmergencyRequiredDate { get; set; }
    }

    public class DonorMatchResultViewModel
    {
        public int DonorId { get; set; }
        public string DonorName { get; set; }
        public string ProfilePicture { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string BloodGroup { get; set; }
        public string City { get; set; }
        public string Area { get; set; }
        public double DistanceKm { get; set; }
        public string TravelTime { get; set; }

        public string AvailabilityStatus { get; set; }
        public DateTime? LastDonationDate { get; set; }
        public string EligibilityStatus { get; set; } // "Eligible" or "In Cooldown"
        public bool IsVerified { get; set; }          // Verification Status

        public int TotalDonations { get; set; }
        public int MatchScore { get; set; }
        public string ScoreBreakdown { get; set; }
        public string Explanation { get; set; }
        public string AiBadge { get; set; }

        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime RegistrationDate { get; set; }
    }

    public class SmartMatchInputViewModel
    {
        public string BloodGroupRequired { get; set; }
        public int UnitsRequired { get; set; }
        public string City { get; set; }
        public string HospitalName { get; set; }
        public string UrgencyLevel { get; set; }
        public int RadiusKm { get; set; }
        public int? BloodRequestId { get; set; }
    }

    public class DonorProfileViewModel
    {
        public int DonorId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string ProfilePicture { get; set; }
        public string Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string BloodGroup { get; set; }
        public string City { get; set; }
        public string Area { get; set; }
        public string Address { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime? LastDonationDate { get; set; }
        public int TotalDonations { get; set; }
        public DateTime RegistrationDate { get; set; }
        public List<DonationHistoryItem> DonationHistory { get; set; }
        public List<MatchTimelineItem> Timeline { get; set; }
    }

    public class DonationHistoryItem
    {
        public DateTime Date { get; set; }
        public string Hospital { get; set; }
        public string Status { get; set; }
    }

    public class MatchTimelineItem
    {
        public string Status { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
    }
}