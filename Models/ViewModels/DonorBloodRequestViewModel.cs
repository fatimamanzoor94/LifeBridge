using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class DonorBloodRequestViewModel
    {
        public int TotalRequests { get; set; }
        public int ActiveRequests { get; set; }
        public int EmergencyRequests { get; set; }
        public int MatchingRequests { get; set; }

        public List<DonorBloodRequestItem> Requests { get; set; } = new();
    }

    public class DonorBloodRequestItem
    {
        public int RequestId { get; set; }
        public string BloodGroup { get; set; }
        public string PatientName { get; set; }
        public string HospitalName { get; set; }
        public string City { get; set; }
        public string ContactNumber { get; set; }
        public int UnitsRequired { get; set; }
        public DateTime RequiredDate { get; set; }
        public string UrgencyLevel { get; set; }
        public string Status { get; set; }
        public DateTime PostedDate { get; set; }
        public bool IsMatchingBloodGroup { get; set; }
        public string Distance { get; set; }
        public string Eligibility { get; set; }
    }
}