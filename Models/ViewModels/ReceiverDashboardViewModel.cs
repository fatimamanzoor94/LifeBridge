using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class ReceiverDashboardViewModel
    {
        // Welcome Section
        public string ReceiverName { get; set; }
        public string ProfileImage { get; set; }
        public string BloodGroupNeeded { get; set; }

        // Summary Statistics
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int MatchedDonors { get; set; }
        public int CompletedRequests { get; set; }
        public int EmergencyRequests { get; set; }

        // Charts Data
        public List<ChartData> RequestStatusData { get; set; } = new();
        public List<ChartData> BloodGroupDistributionData { get; set; } = new();

        // Recent Requests Table
        public List<RecentBloodRequestItem> RecentRequests { get; set; } = new();

        // Emergency Section
        public List<RecentEmergencyAlertItem> ActiveEmergencyRequests { get; set; } = new();

        // Notifications
        public List<RecentNotificationItem> RecentNotifications { get; set; } = new();
        public int UnreadNotificationCount { get; set; }

        // Compatibility
        public List<string> CompatibleDonorGroups { get; set; } = new();
    }
}