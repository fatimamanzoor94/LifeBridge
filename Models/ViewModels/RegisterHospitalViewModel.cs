using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Khoon_e_Hayat.Models.ViewModels
{
    public class RegisterHospitalViewModel
    {
        [Required(ErrorMessage = "Admin Name is required")]
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

        [Required(ErrorMessage = "Hospital Name is required")]
        public string HospitalName { get; set; } = string.Empty;

        [Required(ErrorMessage = "License Number is required")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact Person is required")]
        public string ContactPerson { get; set; } = string.Empty;

        [Required(ErrorMessage = "Verification Document is required")]
        public IFormFile LicenseDocument { get; set; }
    }
}