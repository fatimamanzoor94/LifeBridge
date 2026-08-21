using System.ComponentModel.DataAnnotations;

namespace Khoon_e_Hayat.ViewModels
{
    public class ProfileViewModel
    {
        // --- User Core Fields ---
        public int UserId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Full name can only contain letters and spaces")]
        public string FullName { get; set; }

        public string Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format")]
        [RegularExpression(@"^03[0-9]{9}$", ErrorMessage = "Phone number must be in format 03XXXXXXXXX")]
        public string Phone { get; set; }

        public string ProfilePicture { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }

        [RegularExpression(@"^\d{5}-\d{7}-\d{1}$", ErrorMessage = "CNIC must be in format XXXXX-XXXXXXX-X")]
        public string CNIC { get; set; }

        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
        public string Country { get; set; }

        // --- Donor Specific ---
        [RegularExpression(@"^(A|B|AB|O)[+-]$", ErrorMessage = "Invalid blood group")]
        public string BloodGroup { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [RegularExpression(@"^(Male|Female|Other)$", ErrorMessage = "Invalid gender")]
        public string Gender { get; set; }

        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
        public string Address { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string City { get; set; }

        [Range(30, 200, ErrorMessage = "Weight must be between 30 and 200 kg")]
        public int? Weight { get; set; }
        public bool IsAvailable { get; set; }

        // --- Receiver Specific ---
        [RegularExpression(@"^(A|B|AB|O)[+-]$", ErrorMessage = "Invalid blood group")]
        public string BloodGroupNeeded { get; set; }
        public string UrgencyLevel { get; set; }
        public string HospitalName { get; set; }

        // --- Hospital Specific ---
        public string LicenseNumber { get; set; }

        [StringLength(100, ErrorMessage = "Contact person cannot exceed 100 characters")]
        public string ContactPerson { get; set; }
        public string VerificationStatus { get; set; }
    }
}