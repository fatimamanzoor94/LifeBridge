using Khoon_e_Hayat.Data;
using Khoon_e_Hayat.Models.Entities;
using Khoon_e_Hayat.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Khoon_e_Hayat.Services
{
    public class ProfileService : IProfileService
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(
            ApplicationDbContext db,
            IWebHostEnvironment webHostEnvironment,
            ILogger<ProfileService> logger)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public async Task<ProfileViewModel> GetProfileAsync(int userId)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return null;
                }

                var model = new ProfileViewModel
                {
                    UserId = user.UserId,
                    FullName = user.FullName ?? "",
                    Email = user.Email ?? "",
                    Phone = user.Phone ?? "",
                    ProfilePicture = string.IsNullOrEmpty(user.ProfilePicture) ? "/aassets/img/avatars/DefaultAvatar.png" : user.ProfilePicture,
                    Role = user.Role ?? "",
                    IsActive = user.IsActive,
                    IsEmailVerified = user.IsEmailVerified,
                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLogin,
                    CNIC = user.CNIC ?? "",
                    Country = user.Country ?? ""
                };

                // Fetch role-specific data
                switch (user.Role?.ToLowerInvariant())
                {
                    case "donor":
                        var donor = await _db.DonorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
                        if (donor != null)
                        {
                            model.BloodGroup = donor.BloodGroup;
                            model.DateOfBirth = donor.DateOfBirth;
                            model.Gender = donor.Gender;
                            model.Address = donor.Address;
                            model.City = donor.City;
                            model.Weight = donor.Weight;
                            model.IsAvailable = donor.IsAvailable;
                        }
                        break;

                    case "receiver":
                        var receiver = await _db.ReceiverProfiles.FirstOrDefaultAsync(r => r.UserId == userId);
                        if (receiver != null)
                        {
                            model.BloodGroupNeeded = receiver.BloodGroupNeeded;
                            model.UrgencyLevel = receiver.UrgencyLevel;
                            model.HospitalName = receiver.HospitalName;
                            model.City = receiver.City;
                        }
                        break;

                    case "hospital":
                        var hospital = await _db.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
                        if (hospital != null)
                        {
                            model.HospitalName = hospital.HospitalName;
                            model.LicenseNumber = hospital.LicenseNumber;
                            model.Address = hospital.Address;
                            model.City = hospital.City;
                            model.ContactPerson = hospital.ContactPerson;
                            model.VerificationStatus = hospital.VerificationStatus;
                        }
                        break;
                }

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateProfileAsync(int userId, ProfileViewModel model)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for update: {UserId}", userId);
                    return false;
                }

                // Update Core User Fields
                user.FullName = model.FullName;
                user.Phone = model.Phone;
                user.CNIC = model.CNIC;
                user.Country = model.Country;
                user.UpdatedAt = DateTime.Now;

                // Update Role-Specific Fields
                switch (user.Role?.ToLowerInvariant())
                {
                    case "donor":
                        var donor = await _db.DonorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
                        if (donor != null)
                        {
                            donor.BloodGroup = model.BloodGroup;
                            donor.DateOfBirth = model.DateOfBirth;
                            donor.Gender = model.Gender;
                            donor.Address = model.Address;
                            donor.City = model.City;
                            donor.Weight = model.Weight;
                        }
                        break;

                    case "receiver":
                        var receiver = await _db.ReceiverProfiles.FirstOrDefaultAsync(r => r.UserId == userId);
                        if (receiver != null)
                        {
                            receiver.BloodGroupNeeded = model.BloodGroupNeeded;
                            receiver.HospitalName = model.HospitalName;
                            receiver.City = model.City;
                        }
                        break;

                    case "hospital":
                        var hospital = await _db.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
                        if (hospital != null)
                        {
                            hospital.HospitalName = model.HospitalName;
                            hospital.ContactPerson = model.ContactPerson;
                            hospital.Address = model.Address;
                            hospital.City = model.City;
                        }
                        break;
                }

                await _db.SaveChangesAsync();
                _logger.LogInformation("Profile updated successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return false;
            }
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null)
                {
                    return (false, "User not found.");
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                {
                    _logger.LogWarning("Incorrect password attempt for user {UserId}", userId);
                    return (false, "Current password is incorrect.");
                }

                // Hash and save new password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Password changed successfully for user {UserId}", userId);
                return (true, "Password changed successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return (false, "An error occurred while changing password.");
            }
        }

        public async Task<(bool Success, string FilePath, string Message)> UploadProfilePictureAsync(int userId, IFormFile file)
        {
            string uploadsFolder = "";
            string oldFilePath = "";

            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null)
                {
                    return (false, null, "User not found.");
                }

                // Define uploads folder path
                uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");

                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                    _logger.LogInformation("Created uploads directory: {Path}", uploadsFolder);
                }

                // Delete old image if it exists and is not the default
                if (!string.IsNullOrEmpty(user.ProfilePicture) && !user.ProfilePicture.Contains("DefaultAvatar", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Remove leading slash if present
                        var relativePath = user.ProfilePicture.TrimStart('/', '~');
                        oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                            _logger.LogInformation("Deleted old profile picture: {Path}", oldFilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete old profile picture: {Path}", oldFilePath);
                        // Continue anyway - don't fail the upload because of this
                    }
                }

                // Generate unique filename
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileName = $"{userId}_{Guid.NewGuid():N}{extension}"; // N = 32 digits
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Verify file was saved
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogError("File was not saved to disk: {Path}", filePath);
                    return (false, null, "Failed to save file to disk");
                }

                // Get file info for logging
                var fileInfo = new FileInfo(filePath);
                _logger.LogInformation("Saved profile picture: {Path}, Size: {Size} bytes", filePath, fileInfo.Length);

                // Update database
                var relativeUrl = $"/uploads/profiles/{fileName}";
                user.ProfilePicture = relativeUrl;
                user.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();

                _logger.LogInformation("Profile picture updated in database for user {UserId}: {Url}", userId, relativeUrl);

                return (true, relativeUrl, "Profile picture updated successfully!");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Permission denied accessing: {Path}", uploadsFolder);
                return (false, null, "Permission denied. Please check folder permissions.");
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "IO error during upload to: {Path}", uploadsFolder);
                return (false, null, "Failed to save file. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error uploading profile picture for user {UserId}", userId);
                return (false, null, $"Upload failed: {ex.Message}");
            }
        }

        public async Task<bool> RemoveProfilePictureAsync(int userId)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null)
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(user.ProfilePicture) && !user.ProfilePicture.Contains("DefaultAvatar", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var relativePath = user.ProfilePicture.TrimStart('/', '~');
                        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                            _logger.LogInformation("Deleted profile picture: {Path}", filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete profile picture file");
                        // Continue anyway
                    }
                }

                user.ProfilePicture = "/aassets/img/avatars/DefaultAvatar.png";
                user.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Profile picture removed for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing profile picture for user {UserId}", userId);
                return false;
            }
        }
    }
}