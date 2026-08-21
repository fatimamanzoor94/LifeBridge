using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class DonorDonationViewModel
    {
        // Summary Statistics
        public int TotalDonations { get; set; }
        public int SuccessfulDonations { get; set; }
        public int PendingDonations { get; set; }
        public int EstimatedLivesSaved { get; set; } // 1 donation = 3 lives
        public DateTime? LastDonationDate { get; set; }
        public DateTime? NextEligibleDate { get; set; }
        public int DaysUntilEligible { get; set; }
        public bool IsEligibleToday { get; set; }
        public string EligibilityStatus { get; set; }
        public string DonorStatus { get; set; } // New/Active/Regular/Hero/Platinum

        // Donation History
        public List<DonorDonationHistoryItem> Donations { get; set; } = new();

        // Chart Data
        public List<ChartData> MonthlyDonations { get; set; } = new();
        public List<ChartData> YearlyDonations { get; set; } = new();
        public List<ChartData> DonationTypeDistribution { get; set; } = new();

        // Available Filters
        public List<string> AvailableHospitals { get; set; } = new();
        public List<string> AvailableBloodGroups { get; set; } = new();
    }

    public class DonorDonationHistoryItem
    {
        public int DonationId { get; set; }
        public string DonorName { get; set; }
        public string BloodGroup { get; set; }
        public string HospitalName { get; set; }
        public string ReceiverName { get; set; }
        public DateTime DonationDate { get; set; }
        public string Status { get; set; } // Completed, Pending, Accepted, Scheduled, Cancelled
        public string Location { get; set; }
        public string DonationType { get; set; } // Emergency, Voluntary, Scheduled
        public int BloodQuantity { get; set; }
        public string ResponseTime { get; set; }
        public string MedicalNotes { get; set; }
    }
}
