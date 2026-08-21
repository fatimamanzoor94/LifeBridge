namespace Khoon_e_Hayat.ViewModels
{
    public class BloodBankOverviewViewModel
    {
        public int TotalHospitals { get; set; }
        public int TotalBloodUnits { get; set; }
        public int TotalRequests { get; set; }
        public int EmergencyRequests { get; set; }
        public int LowStockHospitals { get; set; }
        public int ExpiredUnits { get; set; }

        public List<ChartData> BloodGroupDistribution { get; set; } = new();
        public List<HospitalStockSummary> HospitalStocks { get; set; } = new();
        public List<RecentActivityItem> RecentActivities { get; set; } = new();
    }

    public class HospitalStockSummary
    {
        public string HospitalName { get; set; } = "";
        public int TotalUnits { get; set; }
        public int LowStockCount { get; set; }
        public string Status { get; set; } = "";
    }

    public class RecentActivityItem
    {
        public string Action { get; set; } = "";
        public string TimeAgo { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Color { get; set; } = "";
    }
}