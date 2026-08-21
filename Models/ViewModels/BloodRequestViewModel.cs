namespace Khoon_e_Hayat.ViewModels
{
    public class BloodRequestListViewModel
    {
        public List<BloodRequestViewModel> Requests { get; set; } = new();
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int FulfilledCount { get; set; }
        public int EmergencyCount { get; set; }
        public int ActiveCitiesCount { get; set; }
        public List<string> AvailableCities { get; set; } = new();

        // ADD THESE PAGINATION PROPERTIES
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class BloodRequestViewModel
    {
        public int RequestId { get; set; }
        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverEmail { get; set; }
        public string ReceiverPhone { get; set; }
        public string BloodGroup { get; set; }
        public int UnitsRequired { get; set; }
        public string HospitalName { get; set; }
        public string City { get; set; }
        public string UrgencyLevel { get; set; }
        public string RequestStatus { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? RequiredDate { get; set; }
    }

    public class BloodRequestDetailsViewModel
    {
        public int RequestId { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverEmail { get; set; }
        public string ReceiverPhone { get; set; }
        public string BloodGroup { get; set; }
        public int UnitsRequired { get; set; }
        public string UrgencyLevel { get; set; }
        public DateTime? RequiredDate { get; set; }
        public string HospitalName { get; set; }
        public string City { get; set; }
        public string RequestStatus { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}