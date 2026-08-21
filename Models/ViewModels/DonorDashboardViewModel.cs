using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class DonorDashboardViewModel
    {
        // Summary Cards
        public int TotalDonations { get; set; }
        public string BloodGroup { get; set; }
        public bool IsAvailable { get; set; }
        public int SmartMatches { get; set; }
        public int ActiveBloodRequests { get; set; }
        public int EmergencyAlerts { get; set; }
        public int Notifications { get; set; }
        public DateTime? LastDonationDate { get; set; }

        // Charts Data
        public List<ChartData> MonthlyDonationTrend { get; set; } = new();
        public List<ChartData> DonationStatus { get; set; } = new();

        // Recent Activity Lists
        public List<RecentEmergencyAlertItem> RecentEmergencyAlerts { get; set; } = new();
        public List<RecentSmartMatchItem> RecentSmartMatches { get; set; } = new();
        public List<RecentNotificationItem> RecentNotifications { get; set; } = new();

        // Eligibility
        public string EligibilityStatus { get; set; }
        public int DaysUntilEligible { get; set; }
    }

    public class RecentEmergencyAlertItem
    {
        public int AlertId { get; set; }
        public string BloodGroup { get; set; }
        public string HospitalName { get; set; }
        public string City { get; set; }
        public string UrgencyLevel { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class RecentSmartMatchItem
    {
        public int MatchId { get; set; }
        public string BloodGroup { get; set; }
        public string HospitalName { get; set; }
        public string City { get; set; }
        public string Status { get; set; }
        public DateTime MatchDate { get; set; }
    }

    public class RecentNotificationItem
    {
        public int LogId { get; set; }
        public string Category { get; set; }
        public string Subject { get; set; }
        public string Status { get; set; }
        public DateTime SentAt { get; set; }
    }
}