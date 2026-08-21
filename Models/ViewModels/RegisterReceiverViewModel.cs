using System.ComponentModel.DataAnnotations;

namespace Khoon_e_Hayat.Models.ViewModels
{
    public class RegisterReceiverViewModel
    {
        [Required(ErrorMessage = "Full Name is required")]
        [MinLength(3, ErrorMessage = "Name must be at least 3 characters")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Only alphabets are allowed")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{8,}$",
            ErrorMessage = "Password must contain uppercase, lowercase, number & special character")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^03[0-9]{9}$", ErrorMessage = "Phone format should be 03XXXXXXXXX")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Blood Group is required")]
        public string BloodGroupNeeded { get; set; } = string.Empty;

        [Required(ErrorMessage = "Urgency Level is required")]
        public string UrgencyLevel { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hospital Name is required")]
        public string HospitalName { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Required Date is required")]
        [DataType(DataType.Date)]
        public DateTime RequiredDate { get; set; }
    }
}