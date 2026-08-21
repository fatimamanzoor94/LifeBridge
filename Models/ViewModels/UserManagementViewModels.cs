using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class DonorViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? BloodGroup { get; set; }
        public string? City { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DonorDetailsViewModel
    {
        // Personal Information
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }

        // Blood Information
        public string? BloodGroup { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime? LastDonationDate { get; set; }
        public int? Weight { get; set; }

        // Location Information
        public string? Address { get; set; }
        public string? City { get; set; }

        // Account Information
        public bool IsEmailVerified { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DonorListViewModel
    {
        public List<DonorViewModel> Donors { get; set; } = new List<DonorViewModel>();
        public int TotalDonors { get; set; }
        public int AvailableDonors { get; set; }
        public int UnavailableDonors { get; set; }
        public int VerifiedDonors { get; set; }

        // IMPROVEMENT #5: Added Blood Groups Covered property
        public int BloodGroupsCovered { get; set; }
    }

    // Updated ReceiverViewModel for Table Listing
    public class ReceiverViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? BloodGroupNeeded { get; set; }
        public string? HospitalName { get; set; }
        public string? City { get; set; }
        public string? UrgencyLevel { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // New ReceiverDetailsViewModel for View Modal
    public class ReceiverDetailsViewModel
    {
        // Personal Information
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }

        // Blood Requirement Information
        public string? BloodGroupNeeded { get; set; }
        public string? UrgencyLevel { get; set; }
        public DateTime? RequiredDate { get; set; }

        // Hospital Information
        public string? HospitalName { get; set; }
        public string? City { get; set; }

        // Account Information
        public bool IsEmailVerified { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // New ReceiverListViewModel for Page Statistics
    public class ReceiverListViewModel
    {
        public List<ReceiverViewModel> Receivers { get; set; } = new List<ReceiverViewModel>();
        public int TotalReceivers { get; set; }
        public int VerifiedReceivers { get; set; }
        public int ActiveReceivers { get; set; }
        public int CriticalBloodNeed { get; set; }
        public int BloodGroupsNeeded { get; set; }
    }

    // Updated HospitalViewModel for Table Listing
    public class HospitalViewModel
    {
        public int UserId { get; set; }
        public string HospitalName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? LicenseNumber { get; set; }
        public string? ContactPerson { get; set; }
        public string? City { get; set; }
        public string? VerificationStatus { get; set; } // Approved, Pending, Rejected
        public bool IsEmailVerified { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // New HospitalDetailsViewModel for View Modal
    public class HospitalDetailsViewModel
    {
        // Hospital Information
        public string HospitalName { get; set; } = string.Empty;
        public string? LicenseNumber { get; set; }
        public string? ContactPerson { get; set; }

        // Account Information
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Location Information
        public string? Address { get; set; }
        public string? City { get; set; }

        // Verification Information
        public string? VerificationStatus { get; set; }
        public string? LicenseDocumentPath { get; set; }
    }

    // New HospitalListViewModel for Page Statistics
    public class HospitalListViewModel
    {
        public List<HospitalViewModel> Hospitals { get; set; } = new List<HospitalViewModel>();
        public int TotalHospitals { get; set; }
        public int VerifiedHospitals { get; set; }
        public int PendingVerification { get; set; }
        public int RejectedHospitals { get; set; }
        public int ActiveHospitals { get; set; }
        public List<string> AvailableCities { get; set; } = new List<string>(); // For dynamic city filter
    }

    public class UserManagementStatsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalDonors { get; set; }
        public int TotalReceivers { get; set; }
        public int TotalHospitals { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
    }
}