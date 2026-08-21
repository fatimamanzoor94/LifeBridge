namespace Khoon_e_Hayat.ViewModels
{
    public class EmergencyAlertListViewModel
    {
        public List<EmergencyAlertViewModel> Alerts { get; set; } = new();
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int ResolvedCount { get; set; }
        public int CriticalCount { get; set; }
        public int HospitalAlertsCount { get; set; }
        public int TodayCount { get; set; }
        public List<string> AvailableCities { get; set; } = new();
    }

    public class EmergencyAlertViewModel
    {
        public int AlertId { get; set; }
        public int RequestId { get; set; }
        public string ReceiverName { get; set; }
        public string BloodGroup { get; set; }
        public string HospitalName { get; set; }
        public string City { get; set; }
        public string AlertMessage { get; set; }
        public string PriorityLevel { get; set; }
        public string AlertStatus { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class EmergencyAlertDetailsViewModel
    {
        public int AlertId { get; set; }
        public string AlertMessage { get; set; }
        public string PriorityLevel { get; set; }
        public string AlertStatus { get; set; }
        public DateTime CreatedDate { get; set; }

        public string ReceiverName { get; set; }
        public string ReceiverEmail { get; set; }
        public string ReceiverPhone { get; set; }

        public string BloodGroup { get; set; }
        public int UnitsRequired { get; set; }
        public string UrgencyLevel { get; set; }
        public DateTime? RequiredDate { get; set; }

        public string HospitalName { get; set; }
        public string City { get; set; }

        public int RequestId { get; set; }
        public string RequestStatus { get; set; }
        public DateTime RequestCreatedDate { get; set; }
    }
}