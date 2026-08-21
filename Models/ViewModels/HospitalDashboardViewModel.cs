// File: Khoon_e_Hayat/ViewModels/HospitalDashboardViewModel.cs

using System;
using System.Collections.Generic;
using Khoon_e_Hayat.Models.Entities;

namespace Khoon_e_Hayat.ViewModels
{
    public class HospitalDashboardViewModel
    {
        public string HospitalName { get; set; }
        public string HospitalImage { get; set; }
        public bool IsVerified { get; set; }
        public int TotalBloodUnits { get; set; }
        public int AvailableBloodGroups { get; set; }
        public int PendingRequests { get; set; }
        public int EmergencyRequests { get; set; }
        public int AssignedDonors { get; set; }
        public List<BloodInventoryItem> BloodInventory { get; set; } = new();
        public List<BloodRequestSummary> RecentRequests { get; set; } = new();
        public List<BloodRequestSummary> EmergencyRequestList { get; set; } = new();
        public List<ActivityLog> RecentActivities { get; set; } = new();
        public Dictionary<string, int> BloodGroupDistribution { get; set; } = new();
        public Dictionary<string, int> MonthlyBloodUsage { get; set; } = new();

        public Dictionary<string, Dictionary<string, int>> MonthlyBloodUsageByGroup { get; set; } = new();
    }

    public class BloodInventoryItem
    {
        public string BloodGroup { get; set; }
        public int AvailableUnits { get; set; }
        public string Status { get; set; }
    }

    // ✅ KEEP ONLY THIS ONE DEFINITION with ALL properties
    public class BloodRequestSummary
    {
        public int RequestId { get; set; }
        public string ReceiverName { get; set; }
        public string BloodGroup { get; set; }
        public int RequiredUnits { get; set; }
        public int UnitsRequired { get; set; }  // ✅ For SmartDonorSearch view
        public string Priority { get; set; }
        public string UrgencyLevel { get; set; }  // ✅ For SmartDonorSearch view
        public string HospitalName { get; set; }  // ✅ For SmartDonorSearch view
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
    }

    public class ActivityLog
    {
        public string Activity { get; set; }
        public DateTime Timestamp { get; set; }
        public string Icon { get; set; }
    }
}