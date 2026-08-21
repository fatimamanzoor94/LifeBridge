using System;
using System.ComponentModel.DataAnnotations;

namespace Khoon_e_Hayat.ViewModels
{
    public class ReceiverCreateBloodRequestViewModel
    {
        // Receiver Information (Read Only - from logged in user)
        [Display(Name = "Receiver Name")]
        public string ReceiverName { get; set; }

        [Display(Name = "Receiver Email")]
        public string ReceiverEmail { get; set; }

        [Display(Name = "Receiver Status")]
        public string ReceiverStatus { get; set; } = "Verified Receiver";

        // Patient Information
        [Display(Name = "Patient Name")]
        [Required(ErrorMessage = "Patient name is required")]
        [StringLength(100, ErrorMessage = "Patient name cannot exceed 100 characters")]
        public string PatientName { get; set; }

        [Display(Name = "Patient Age")]
        [Required(ErrorMessage = "Patient age is required")]
        [Range(1, 120, ErrorMessage = "Age must be between 1 and 120")]
        public int PatientAge { get; set; }

        [Display(Name = "Gender")]
        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; }

        [Display(Name = "Blood Group")]
        [Required(ErrorMessage = "Blood group is required")]
        public string BloodGroup { get; set; }

        [Display(Name = "Units Required")]
        [Required(ErrorMessage = "Units required is required")]
        [Range(1, 10, ErrorMessage = "Units must be between 1 and 10")]
        public int UnitsRequired { get; set; }

        [Display(Name = "Hospital Name")]
        [Required(ErrorMessage = "Hospital name is required")]
        [StringLength(150, ErrorMessage = "Hospital name cannot exceed 150 characters")]
        public string HospitalName { get; set; }

        [Display(Name = "Hospital Contact")]
        //[Required(ErrorMessage = "Hospital contact is required")]
        [StringLength(100, ErrorMessage = "Contact information cannot exceed 100 characters")]
        public string HospitalContact { get; set; }  // ✅ Phone attribute removed

        [Display(Name = "Hospital Address")]
        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
        public string Address { get; set; }

        [Display(Name = "City")]
        [Required(ErrorMessage = "City is required")]
        public string City { get; set; }

        [Display(Name = "Required Date")]
        [Required(ErrorMessage = "Required date is required")]
        [DataType(DataType.Date)]
        public DateTime RequiredDate { get; set; }

        [Display(Name = "Urgency Level")]
        [Required(ErrorMessage = "Urgency level is required")]
        public string UrgencyLevel { get; set; }

        [Display(Name = "Reason")]
        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; }

        [Display(Name = "Additional Notes")]
        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string AdditionalNotes { get; set; }

        [Display(Name = "Area")]
        [StringLength(100, ErrorMessage = "Area cannot exceed 100 characters")]
        public string Area { get; set; }

        [Display(Name = "Full Address")]
        [StringLength(255, ErrorMessage = "Full address cannot exceed 255 characters")]
        public string FullAddress { get; set; }

        [Display(Name = "Emergency Request")]
        public bool IsEmergency { get; set; }

        // "I am the Patient" checkbox
        [Display(Name = "I am the patient")]
        public bool IAmThePatient { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [Display(Name = "Hospital ID")]
        public int? HospitalId { get; set; }

        [Display(Name = "Hospital ID")]
        public int? SelectedHospitalId { get; set; }
        public string? HospitalContactAuto { get; set; }
        public string? HospitalAddressAuto { get; set; }
        public string? HospitalCityAuto { get; set; }
        public double? HospitalLat { get; set; }
        public double? HospitalLng { get; set; }
        public string? HospitalVerificationStatus { get; set; }
    }
}