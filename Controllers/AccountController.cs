using BCrypt.Net;
using Khoon_e_Hayat.Data;
using Khoon_e_Hayat.Models.Entities;
using Khoon_e_Hayat.Models.ViewModels;
using Khoon_e_Hayat.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Khoon_e_Hayat.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(
            ApplicationDbContext context,
            IEmailService emailService,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _emailService = emailService;
            _webHostEnvironment = webHostEnvironment;
        }

        // ==========================================
        // REGISTER - ROLE SELECTION PAGE
        // ==========================================
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var role = User.FindFirstValue(ClaimTypes.Role);
                return role switch
                {
                    "Donor" => RedirectToAction("Dashboard", "Donor"),
                    "Receiver" => RedirectToAction("Dashboard", "Receiver"),
                    "Hospital" => RedirectToAction("Dashboard", "Hospital"),
                    "Admin" => RedirectToAction("Dashboard", "Admin"),
                    _ => RedirectToAction("Index", "Home")
                };
            }

            return View();
        }

        // ==========================================
        // DONOR REGISTRATION
        // ==========================================
        [HttpGet]
        public IActionResult RegisterDonor() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterDonor(RegisterDonorViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. Duplicate Email Check
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == model.Email.Trim().ToLower()))
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            // 2. Business Rule: Age Validation (Must be 18+)
            var age = DateTime.Today.Year - model.DateOfBirth.Year;
            if (model.DateOfBirth > DateTime.Today.AddYears(-age)) age--;
            if (age < 18)
            {
                ModelState.AddModelError("DateOfBirth", "You must be at least 18 years old to register as a donor.");
                return View(model);
            }

            // 3. Business Rule: Weight Validation (Must be 50+ kg)
            if (model.Weight < 50)
            {
                ModelState.AddModelError("Weight", "Weight must be at least 50 kg to donate blood.");
                return View(model);
            }

            // 4. Create User Entity with BCrypt Password
            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, workFactor: 10),
                Role = "Donor",
                Phone = model.Phone,
                IsActive = true,
                IsApproved = true,
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                EmailVerificationToken = Guid.NewGuid().ToString(),
                EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 5. Create Donor Profile
            var profile = new DonorProfile
            {
                UserId = user.UserId,
                BloodGroup = model.BloodGroup,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                Weight = model.Weight,
                Address = model.Address.Trim(),
                City = model.City.Trim(),
                IsAvailable = true
            };

            _context.DonorProfiles.Add(profile);
            await _context.SaveChangesAsync();

            // 6. Send Verification Email
            var callbackUrl = Url.Action("VerifyEmail", "Account",
                new { token = user.EmailVerificationToken, email = user.Email },
                protocol: HttpContext.Request.Scheme);

            await _emailService.SendEmailVerificationAsync(user.Email, user.FullName, callbackUrl);

            TempData["SuccessMessage"] = "Registration successful! Please check your email to verify your account.";

            return RedirectToAction("Login");
        }

        // ==========================================
        // RECEIVER REGISTRATION
        // ==========================================
        [HttpGet]
        public IActionResult RegisterReceiver() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterReceiver(RegisterReceiverViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. Duplicate Email Check
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == model.Email.Trim().ToLower()))
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            // 2. Business Rule: Required Date Validation
            if (model.RequiredDate < DateTime.Today)
            {
                ModelState.AddModelError("RequiredDate", "Required date cannot be in the past.");
                return View(model);
            }

            // 3. Create User Entity with BCrypt Password
            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, workFactor: 10),
                Role = "Receiver",
                Phone = model.Phone,
                IsActive = true,
                IsApproved = true,
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                EmailVerificationToken = Guid.NewGuid().ToString(),
                EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 4. Create Receiver Profile
            var profile = new ReceiverProfile
            {
                UserId = user.UserId,
                BloodGroupNeeded = model.BloodGroupNeeded,
                UrgencyLevel = model.UrgencyLevel,
                HospitalName = model.HospitalName.Trim(),
                City = model.City.Trim(),
                RequiredDate = model.RequiredDate
            };

            _context.ReceiverProfiles.Add(profile);
            await _context.SaveChangesAsync();

            // 5. Send Verification Email
            var callbackUrl = Url.Action("VerifyEmail", "Account",
                new { token = user.EmailVerificationToken, email = user.Email },
                protocol: HttpContext.Request.Scheme);

            await _emailService.SendEmailVerificationAsync(user.Email, user.FullName, callbackUrl);

            TempData["SuccessMessage"] = "Registration successful! Please check your email to verify your account.";

            return RedirectToAction("Login");
        }

        // ==========================================
        // HOSPITAL REGISTRATION
        // ==========================================
        [HttpGet]
        public IActionResult RegisterHospital() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterHospital(RegisterHospitalViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. Duplicate Email Check
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == model.Email.Trim().ToLower()))
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            // 2. File Upload Validation
            if (model.LicenseDocument == null || model.LicenseDocument.Length == 0)
            {
                ModelState.AddModelError("LicenseDocument", "Please upload a verification document.");
                return View(model);
            }

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(model.LicenseDocument.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("LicenseDocument", "Invalid file type. Only PDF, JPG, and PNG are allowed.");
                return View(model);
            }

            if (model.LicenseDocument.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("LicenseDocument", "File size cannot exceed 5MB.");
                return View(model);
            }

            // 3. Create User Entity with BCrypt Password
            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, workFactor: 10),
                Role = "Hospital",
                Phone = model.Phone,
                IsActive = true,
                IsApproved = false, // Requires Admin Approval
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                EmailVerificationToken = Guid.NewGuid().ToString(),
                EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 4. Save File Securely
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "hospitals");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.LicenseDocument.CopyToAsync(stream);
            }

            // 5. Create Hospital Profile
            var profile = new HospitalProfile
            {
                UserId = user.UserId,
                HospitalName = model.HospitalName.Trim(),
                LicenseNumber = model.LicenseNumber.Trim(),
                Address = model.Address.Trim(),
                City = model.City.Trim(),
                ContactPerson = model.ContactPerson.Trim(),
                LicenseDocumentPath = $"/uploads/hospitals/{fileName}",
                VerificationStatus = "Pending"
            };

            _context.HospitalProfiles.Add(profile);
            await _context.SaveChangesAsync();

            // 6. Send Verification Email
            var callbackUrl = Url.Action("VerifyEmail", "Account",
                new { token = user.EmailVerificationToken, email = user.Email },
                protocol: HttpContext.Request.Scheme);

            await _emailService.SendEmailVerificationAsync(user.Email, user.FullName, callbackUrl);

            TempData["SuccessMessage"] = "Your hospital registration has been submitted successfully. Please verify your email address to continue. Our admin team will review your application shortly.";

            return RedirectToAction("Login");
        }

        // ==========================================
        // EMAIL VERIFICATION
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Invalid verification link.";
                return RedirectToAction("Register");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email.ToLower() == email.ToLower() &&
                u.EmailVerificationToken == token);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid verification link or email not found.";
                return RedirectToAction("Register");
            }

            if (user.EmailVerificationTokenExpiry < DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Verification link has expired. Please register again.";
                return RedirectToAction("Register");
            }

            // Activate Account
            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiry = null;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Email verified successfully. You can now login.";
            return RedirectToAction("Login");
        }

        // ==========================================
        // LOGIN
        // ==========================================
        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var role = User.FindFirstValue(ClaimTypes.Role);
                return role switch
                {
                    "Donor" => RedirectToAction("Dashboard", "Donor"),
                    "Receiver" => RedirectToAction("Dashboard", "Receiver"),
                    "Hospital" => RedirectToAction("Dashboard", "Hospital"),
                    "Admin" => RedirectToAction("Dashboard", "Admin"),
                    _ => RedirectToAction("Index", "Home")
                };
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Please correct the errors below and try again.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError("Email", "Email is required.");
                return View(model);
            }

            var normalizedEmail = model.Email.Trim().ToLower();
            var user = await GetUserByEmailSafe(normalizedEmail);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            // ✅ PROFESSIONAL PASSWORD VERIFICATION WITH BCrypt
            bool isPasswordValid = VerifyPassword(model.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            if (!user.IsEmailVerified)
            {
                ModelState.AddModelError(string.Empty, "Please verify your email address before logging in.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Your account is inactive. Please contact support.");
                return View(model);
            }

            if (user.Role == "Hospital" && !user.IsApproved)
            {
                ModelState.AddModelError(string.Empty, "Your hospital account is under review. Please wait for admin approval.");
                return View(model);
            }

            // ✅ Create Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? ""),
                new Claim("ProfilePicture", user.ProfilePicture ?? "default.png")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // ✅ Update Session
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", user.FullName ?? "User");
            HttpContext.Session.SetString("UserRole", user.Role ?? "User");
            HttpContext.Session.SetString("ProfileImageUrl", user.ProfilePicture ?? "/aassets/img/avatars/DefaultAvatar.png");

            // ✅ Update Last Login
            user.LastLogin = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return user.Role switch
            {
                "Donor" => RedirectToAction("Dashboard", "Donor"),
                "Receiver" => RedirectToAction("Dashboard", "Receiver"),
                "Hospital" => RedirectToAction("Dashboard", "Hospital"),
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // ✅ PROFESSIONAL PASSWORD VERIFICATION METHOD
        private bool VerifyPassword(string password, string passwordHash)
        {
            try
            {
                // Check if it's a valid BCrypt hash (starts with $2)
                if (passwordHash.StartsWith("$2"))
                {
                    return BCrypt.Net.BCrypt.Verify(password, passwordHash);
                }
                else
                {
                    // Plain text password (legacy) - verify and auto-migrate
                    if (passwordHash == password)
                    {
                        // This will be handled by the caller to save changes
                        return true;
                    }
                    return false;
                }
            }
            catch
            {
                // Fallback to plain text comparison
                return passwordHash == password;
            }
        }

        // ✅ ADO.NET HELPER METHOD - Safe data fetch
        private async Task<User?> GetUserByEmailSafe(string email)
        {
            using var connection = new SqlConnection(_context.Database.GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand(
                "SELECT * FROM Users WHERE Email IS NOT NULL AND LOWER(Email) = @Email",
                connection);
            command.Parameters.AddWithValue("@Email", email);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new User
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    FullName = reader.IsDBNull(reader.GetOrdinal("FullName")) ? "" : reader.GetString(reader.GetOrdinal("FullName")),
                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? "" : reader.GetString(reader.GetOrdinal("Email")),
                    PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                    Role = reader.GetString(reader.GetOrdinal("Role")),
                    Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? "" : reader.GetString(reader.GetOrdinal("Phone")),
                    ProfilePicture = reader.IsDBNull(reader.GetOrdinal("ProfilePicture")) ? "" : reader.GetString(reader.GetOrdinal("ProfilePicture")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    IsEmailVerified = reader.GetBoolean(reader.GetOrdinal("IsEmailVerified")),
                    IsApproved = reader.GetBoolean(reader.GetOrdinal("IsApproved")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    LastLogin = reader.IsDBNull(reader.GetOrdinal("LastLogin")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("LastLogin"))
                };
            }

            return null;
        }

        // ==========================================
        // LOGOUT
        // ==========================================
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ==========================================
        // FORGOT PASSWORD
        // ==========================================
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var email = model.Email.Trim().ToLower();
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

                if (user != null && user.IsActive)
                {
                    var token = GenerateSecureToken();

                    user.ResetToken = BCrypt.Net.BCrypt.HashPassword(token);
                    user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);

                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();

                    var resetLink = Url.Action("ResetPassword", "Account",
                        new { token, email = user.Email }, Request.Scheme);

                    await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetLink);
                }

                TempData["SuccessMessage"] = "If an account with that email exists, we've sent a password reset link.";
                return RedirectToAction("Login");
            }
            return View(model);
        }

        // ==========================================
        // RESET PASSWORD
        // ==========================================
        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Invalid password reset link.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == model.Email.Trim().ToLower());

                if (user == null || string.IsNullOrEmpty(user.ResetToken) || user.ResetTokenExpiry < DateTime.UtcNow)
                {
                    ModelState.AddModelError(string.Empty, "Invalid or expired reset token. Please request a new one.");
                    return View(model);
                }

                if (!BCrypt.Net.BCrypt.Verify(model.Token, user.ResetToken))
                {
                    ModelState.AddModelError(string.Empty, "Invalid or expired reset token. Please request a new one.");
                    return View(model);
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword, workFactor: 10);
                user.ResetToken = null;
                user.ResetTokenExpiry = null;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your password has been reset successfully. Please login with your new password.";
                return RedirectToAction("Login");
            }
            return View(model);
        }

        // Helper: Generate 32-byte cryptographically secure token
        private string GenerateSecureToken()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}