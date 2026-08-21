using BCrypt.Net;
using Khoon_e_Hayat.Data;
using Khoon_e_Hayat.Models.Entities;
using Khoon_e_Hayat.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Khoon_e_Hayat.Controllers
{
    [Authorize]
    public class ProfileController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Profile
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var vm = new ProfileViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                ProfilePicture = user.ProfilePicture ?? "/aassets/img/avatars/DefaultAvatar.png",
                Role = user.Role,
                IsActive = user.IsActive,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                CNIC = user.CNIC,
                Country = user.Country
            };

            // Load role-specific data ONLY for non-Admin users
            if (user.Role == "Donor")
            {
                var donorProfile = await _context.DonorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
                if (donorProfile != null)
                {
                    vm.BloodGroup = donorProfile.BloodGroup;
                    vm.DateOfBirth = donorProfile.DateOfBirth;
                    vm.Gender = donorProfile.Gender;
                    vm.Address = donorProfile.Address;
                    vm.City = donorProfile.City;
                    vm.Weight = donorProfile.Weight;
                    vm.IsAvailable = donorProfile.IsAvailable;
                }
            }
            else if (user.Role == "Receiver")
            {
                var receiverProfile = await _context.ReceiverProfiles.FirstOrDefaultAsync(r => r.UserId == userId);
                if (receiverProfile != null)
                {
                    vm.BloodGroupNeeded = receiverProfile.BloodGroupNeeded;
                    vm.UrgencyLevel = receiverProfile.UrgencyLevel;
                    vm.HospitalName = receiverProfile.HospitalName;
                    vm.City = receiverProfile.City;
                }
            }
            else if (user.Role == "Hospital")
            {
                var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
                if (hospitalProfile != null)
                {
                    vm.LicenseNumber = hospitalProfile.LicenseNumber;
                    vm.ContactPerson = hospitalProfile.ContactPerson;
                    vm.VerificationStatus = hospitalProfile.VerificationStatus;
                    vm.HospitalName = hospitalProfile.HospitalName;
                    vm.City = hospitalProfile.City;
                    vm.Address = hospitalProfile.Address;
                }
            }

            ViewBag.UnreadMessageCount = await _context.ContactMessages.CountAsync(m => m.Status == "New");
            ViewBag.AdminName = User.FindFirstValue(ClaimTypes.Name) ?? "User";
            ViewBag.AdminEmail = User.FindFirstValue(ClaimTypes.Email) ?? user.Email;
            ViewBag.AdminRole = user.Role;
            ViewBag.ProfilePicture = user.ProfilePicture ?? "/aassets/img/avatars/DefaultAvatar.png";

            return View(vm);
        }

        // POST: Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Json(new { success = false, message = "User not authenticated" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Json(new { success = false, message = "User not found" });

            // Update basic fields for ALL users
            user.FullName = model.FullName?.Trim();
            user.Phone = model.Phone?.Trim();
            user.CNIC = model.CNIC?.Trim();
            user.Country = model.Country?.Trim();
            user.UpdatedAt = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();

                // Update role-specific data ONLY for non-Admin users
                if (user.Role == "Donor")
                {
                    var donorProfile = await _context.DonorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
                    if (donorProfile != null)
                    {
                        donorProfile.BloodGroup = model.BloodGroup;
                        donorProfile.DateOfBirth = model.DateOfBirth;
                        donorProfile.Gender = model.Gender;
                        donorProfile.Address = model.Address;
                        donorProfile.City = model.City;
                        donorProfile.Weight = model.Weight;
                        await _context.SaveChangesAsync();
                    }
                }
                else if (user.Role == "Receiver")
                {
                    var receiverProfile = await _context.ReceiverProfiles.FirstOrDefaultAsync(r => r.UserId == userId);
                    if (receiverProfile != null)
                    {
                        receiverProfile.BloodGroupNeeded = model.BloodGroupNeeded;
                        receiverProfile.City = model.City;
                        receiverProfile.HospitalName = model.HospitalName;
                        await _context.SaveChangesAsync();
                    }
                }
                else if (user.Role == "Hospital")
                {
                    var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
                    if (hospitalProfile != null)
                    {
                        hospitalProfile.ContactPerson = model.ContactPerson;
                        hospitalProfile.HospitalName = model.HospitalName;
                        hospitalProfile.City = model.City;
                        hospitalProfile.Address = model.Address;
                        await _context.SaveChangesAsync();
                    }
                }

                return Json(new { success = true, message = "Profile updated successfully!" });
            }
            catch (DbUpdateException ex)
            {
                return Json(new { success = false, message = "An error occurred while updating your profile.", error = ex.Message });
            }
        }

        // POST: Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Validation failed", errors = errors });
            }

            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Verify current password
            var passwordValid = await VerifyPasswordAsync(user, model.CurrentPassword);

            if (!passwordValid)
            {
                return Json(new { success = false, message = "Current password is incorrect." });
            }

            // Update password with BCrypt
            user.PasswordHash = HashPassword(model.NewPassword);
            user.UpdatedAt = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Password changed successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while changing password.", error = ex.Message });
            }
        }

        // POST: Profile/UploadPhoto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            try
            {
                if (photo == null || photo.Length == 0)
                {
                    return Json(new { success = false, message = "Please select a valid image file." });
                }

                var userId = GetCurrentUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { success = false, message = "Only JPG, JPEG, PNG, GIF, and WEBP files are allowed." });
                }

                // Validate file size (5MB max)
                if (photo.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "File size must not exceed 5MB." });
                }

                // Save file
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                // Delete old profile picture if exists
                if (!string.IsNullOrEmpty(user.ProfilePicture) &&
                    !user.ProfilePicture.Contains("DefaultAvatar", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePicture.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }
                    catch { }
                }

                // Update user profile picture
                user.ProfilePicture = $"/uploads/profiles/{fileName}";
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // ✅ Update session
                HttpContext.Session.SetString("ProfileImageUrl", user.ProfilePicture);

                return Json(new
                {
                    success = true,
                    message = "Profile photo updated successfully!",
                    fileName = fileName,
                    imageUrl = user.ProfilePicture
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Upload failed: " + ex.Message });
            }
        }

        // POST: Profile/RemovePicture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePicture()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Delete file if exists
                if (!string.IsNullOrEmpty(user.ProfilePicture) &&
                    !user.ProfilePicture.Contains("DefaultAvatar", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePicture.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                    catch { }
                }

                user.ProfilePicture = "/aassets/img/avatars/DefaultAvatar.png";
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Profile picture removed successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to remove picture: " + ex.Message });
            }
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        // ✅ PROFESSIONAL PASSWORD VERIFICATION
        private Task<bool> VerifyPasswordAsync(User user, string password)
        {
            try
            {
                // Check if it's a valid BCrypt hash (starts with $2)
                if (user.PasswordHash.StartsWith("$2"))
                {
                    bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                    return Task.FromResult(isValid);
                }
                else
                {
                    // Plain text password - verify and auto-migrate
                    if (user.PasswordHash == password)
                    {
                        // Migrate to BCrypt
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10);
                        _context.SaveChanges();
                        return Task.FromResult(true);
                    }
                    return Task.FromResult(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Password verification error: {ex.Message}");
                // Fallback to plain text
                return Task.FromResult(user.PasswordHash == password);
            }
        }

        // ✅ PROFESSIONAL PASSWORD HASHING
        private string HashPassword(string password)
        {
            // BCrypt with work factor 10 (recommended for production)
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10);
        }
    }

    // ==========================================
    // VIEW MODELS
    // ==========================================

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class SecurityViewModel
    {
        public bool IsEmailVerified { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}