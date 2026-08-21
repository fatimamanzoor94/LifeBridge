using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class DashboardViewModel
    {
        // ==================== SUMMARY CARDS ====================
        public int TotalUsers { get; set; }
        public int TotalDonors { get; set; }
        public int TotalReceivers { get; set; }
        public int TotalHospitals { get; set; }
        public int PendingHospitalApprovals { get; set; }
        public int ActiveBloodRequests { get; set; }
        public int EmergencyRequests { get; set; }
        public int ContactMessages { get; set; }

        // ==================== CHARTS DATA ====================
        public List<ChartData> UserDistributionData { get; set; } = new();
        public List<ChartData> BloodRequestsByStatusData { get; set; } = new();
        public List<ChartData> BloodRequestsByBloodGroupData { get; set; } = new();
        public List<MonthlyRegistrationsData> MonthlyRegistrationsData { get; set; } = new();

        // ==================== TABLES DATA ====================
        public List<RecentBloodRequestItem> RecentBloodRequests { get; set; } = new();
        public List<RecentUserItem> RecentUsers { get; set; } = new();
        public List<PendingHospitalItem> PendingHospitals { get; set; } = new();
        public List<RecentContactMessageItem> RecentContactMessages { get; set; } = new();
    }

    // ==================== HELPER CLASSES ====================

    public class ChartData
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Color { get; set; } = "#0d6efd";
    }

    public class MonthlyRegistrationsData
    {
        public string Month { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class RecentBloodRequestItem
    {
        public int RequestId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string BloodGroup { get; set; } = string.Empty;
        public int UnitsRequired { get; set; }
        public string HospitalName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string UrgencyLevel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class RecentUserItem
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
    }

    public class PendingHospitalItem
    {
        public int HospitalId { get; set; }
        public int UserId { get; set; }
        public string HospitalName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
    }

    public class RecentContactMessageItem
    {
        public int MessageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}

