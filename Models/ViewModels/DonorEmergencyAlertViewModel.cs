using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class DonorEmergencyAlertViewModel
    {
        public int TotalAlerts { get; set; }
        public int ActiveAlerts { get; set; }
        public int CriticalCases { get; set; }
        public int ExpiringSoon { get; set; }
        public int NearbyEmergencies { get; set; }

        public int MyEmergencyResponses { get; set; }
        public string AverageResponseTime { get; set; }

        // Cooldown Properties
        public bool IsInCooldown { get; set; }
        public DateTime? NextEligibleDate { get; set; }
        public int DaysUntilEligible { get; set; }

        // Pre-formatted display strings
        public string NextEligibleDateDisplay { get; set; }
        public string CurrentDateDisplay { get; set; }

        public List<DonorEmergencyAlertItem> Alerts { get; set; } = new();
        public List<string> AvailableCities { get; set; } = new();
        public List<string> AvailableBloodGroups { get; set; } = new();
    }

    public class DonorEmergencyAlertItem
    {
        public int AlertId { get; set; }
        public string BloodGroup { get; set; }
        public string HospitalName { get; set; }
        public string HospitalAddress { get; set; }
        public string HospitalContact { get; set; }
        public string City { get; set; }
        public string UrgencyLevel { get; set; }
        public int UnitsRequired { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime RequiredBefore { get; set; }
        public string Status { get; set; }
        public double DistanceKm { get; set; }
        public int TravelTimeMins { get; set; }
        public int RespondedDonors { get; set; }
        public string EmergencyDescription { get; set; }
        public string PatientName { get; set; }
        public string SpecialInstructions { get; set; }
        public int PatientAge { get; set; }

        // --- NEW: Pre-calculated Display Properties ---
        public string UrgencyBadgeColor { get; set; }
        public string UrgencyBorderColor { get; set; }
        public string StatusColor { get; set; }
        public string PostedTimeText { get; set; }
        public int ProgressPercent { get; set; }
        public string ProgressColor { get; set; }
        public string RequiredBeforeDisplay { get; set; }

        // --- NEW: Pre-processed Data Attributes for JS ---
        public string HospitalNameLower { get; set; }
        public string CityLower { get; set; }
        public string RequiredBeforeIso { get; set; }
        public string RequestDateIso { get; set; }
    }
}
