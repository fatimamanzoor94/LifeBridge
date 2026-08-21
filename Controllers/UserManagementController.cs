using Khoon_e_Hayat.Data;
using Khoon_e_Hayat.Models;
using Khoon_e_Hayat.Models.Entities;
using Khoon_e_Hayat.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Khoon_e_Hayat.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public UserManagementController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================== DONORS MANAGEMENT ====================

        [HttpGet]
        public async Task<IActionResult> Donors()
        {
            var donors = await _context.Users
                .Where(u => u.Role == "Donor")
                .Join(_context.DonorProfiles,
                      u => u.UserId,
                      d => d.UserId,
                      (u, d) => new DonorViewModel
                      {
                          UserId = u.UserId,
                          FullName = u.FullName,
                          Email = u.Email,
                          Phone = u.Phone,
                          BloodGroup = d.BloodGroup,
                          City = d.City,
                          IsAvailable = d.IsAvailable,
                          IsEmailVerified = u.IsEmailVerified,
                          IsActive = u.IsActive,
                          CreatedAt = u.CreatedAt
                      })
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var viewModel = new DonorListViewModel
            {
                Donors = donors,
                TotalDonors = await _context.Users.CountAsync(u => u.Role == "Donor"),
                AvailableDonors = donors.Count(d => d.IsAvailable),
                UnavailableDonors = donors.Count(d => !d.IsAvailable),
                VerifiedDonors = donors.Count(d => d.IsEmailVerified),
                BloodGroupsCovered = donors.Where(d => !string.IsNullOrEmpty(d.BloodGroup))
                                           .Select(d => d.BloodGroup)
                                           .Distinct()
                                           .Count()
            };

            ViewBag.UnreadMessageCount = await _context.ContactMessages.CountAsync(m => m.Status == "New");

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetDonorDetails(int userId)
        {
            try
            {
                // Debug: Check if user exists
                var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
                if (!userExists)
                {
                    return Json(new { error = $"User with ID {userId} not found in Users table" });
                }

                // Debug: Check user role
                var userRole = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => u.Role)
                    .FirstOrDefaultAsync();

                if (userRole != "Donor")
                {
                    return Json(new { error = $"User role is '{userRole}', not 'Donor'" });
                }

                // Debug: Check if donor profile exists
                var profileExists = await _context.DonorProfiles.AnyAsync(d => d.UserId == userId);
                if (!profileExists)
                {
                    return Json(new { error = $"No donor profile found for UserId {userId}" });
                }

                // Main query
                var donor = await _context.Users
                    .Where(u => u.UserId == userId && u.Role == "Donor")
                    .Join(_context.DonorProfiles,
                          u => u.UserId,
                          d => d.UserId,
                          (u, d) => new DonorDetailsViewModel
                          {
                              FullName = u.FullName,
                              Email = u.Email,
                              Phone = u.Phone,
                              Gender = d.Gender,
                              DateOfBirth = d.DateOfBirth,
                              BloodGroup = d.BloodGroup,
                              IsAvailable = d.IsAvailable,
                              LastDonationDate = d.LastDonationDate,
                              Weight = d.Weight,
                              Address = d.Address,
                              City = d.City,
                              IsEmailVerified = u.IsEmailVerified,
                              IsActive = u.IsActive,
                              CreatedAt = u.CreatedAt
                          })
                    .FirstOrDefaultAsync();

                if (donor == null)
                    return Json(new { error = "Donor details could not be loaded" });

                return Json(donor);
            }
            catch (Exception ex)
            {
                return Json(new { error = $"Server error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateDonor(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.Role != "Donor")
                    return Json(new { success = false, message = "Donor not found" });

                user.IsActive = true;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                await LogAdminAction($"Activated donor account: {user.Email}");

                return Json(new { success = true, message = "Donor activated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateDonor(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.Role != "Donor")
                    return Json(new { success = false, message = "Donor not found" });

                user.IsActive = false;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                await LogAdminAction($"Deactivated donor account: {user.Email}");

                return Json(new { success = true, message = "Donor deactivated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // ==================== RECEIVERS MANAGEMENT ====================

        [HttpGet]
        public async Task<IActionResult> Receivers()
        {
            var receivers = await _context.Users
                .Where(u => u.Role == "Receiver")
                .Join(_context.ReceiverProfiles,
                      u => u.UserId,
                      r => r.UserId,
                      (u, r) => new ReceiverViewModel
                      {
                          UserId = u.UserId,
                          FullName = u.FullName,
                          Email = u.Email,
                          Phone = u.Phone,
                          BloodGroupNeeded = r.BloodGroupNeeded,
                          HospitalName = r.HospitalName,
                          City = r.City,
                          UrgencyLevel = r.UrgencyLevel,
                          IsEmailVerified = u.IsEmailVerified,
                          IsActive = u.IsActive,
                          CreatedAt = u.CreatedAt
                      })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var viewModel = new ReceiverListViewModel
            {
                Receivers = receivers,
                TotalReceivers = await _context.Users.CountAsync(u => u.Role == "Receiver"),
                VerifiedReceivers = receivers.Count(r => r.IsEmailVerified),
                ActiveReceivers = receivers.Count(r => r.IsActive),
                CriticalBloodNeed = receivers.Count(r => r.UrgencyLevel == "Critical"),
                BloodGroupsNeeded = receivers.Where(r => !string.IsNullOrEmpty(r.BloodGroupNeeded))
                                             .Select(r => r.BloodGroupNeeded)
                                             .Distinct()
                                             .Count()
            };

            ViewBag.UnreadMessageCount = await _context.ContactMessages.CountAsync(m => m.Status == "New");

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetReceiverDetails(int userId)
        {
            var receiver = await _context.Users
                .Where(u => u.UserId == userId && u.Role == "Receiver")
                .Join(_context.ReceiverProfiles,
                      u => u.UserId,
                      r => r.UserId,
                      (u, r) => new ReceiverDetailsViewModel
                      {
                          FullName = u.FullName,
                          Email = u.Email,
                          Phone = u.Phone,
                          BloodGroupNeeded = r.BloodGroupNeeded,
                          UrgencyLevel = r.UrgencyLevel,
                          RequiredDate = r.RequiredDate,
                          HospitalName = r.HospitalName,
                          City = r.City,
                          IsEmailVerified = u.IsEmailVerified,
                          IsActive = u.IsActive,
                          CreatedAt = u.CreatedAt
                      })
                .FirstOrDefaultAsync();

            if (receiver == null)
                return NotFound();

            return Json(receiver);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateReceiver(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.Role != "Receiver")
                    return Json(new { success = false, message = "Receiver not found" });

                user.IsActive = true;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                await LogAdminAction($"Activated receiver account: {user.Email}");

                return Json(new { success = true, message = "Receiver activated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateReceiver(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.Role != "Receiver")
                    return Json(new { success = false, message = "Receiver not found" });

                user.IsActive = false;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                await LogAdminAction($"Deactivated receiver account: {user.Email}");

                return Json(new { success = true, message = "Receiver deactivated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportReceiversExcel()
        {
            var receivers = await _context.Users
                .Where(u => u.Role == "Receiver")
                .Join(_context.ReceiverProfiles, u => u.UserId, r => r.UserId, (u, r) => new { u, r })
                .OrderByDescending(x => x.u.CreatedAt)
                .Select(x => new ReceiverViewModel
                {
                    UserId = x.u.UserId,
                    FullName = x.u.FullName,
                    Email = x.u.Email,
                    Phone = x.u.Phone,
                    BloodGroupNeeded = x.r.BloodGroupNeeded,
                    HospitalName = x.r.HospitalName,
                    City = x.r.City,
                    UrgencyLevel = x.r.UrgencyLevel,
                    IsEmailVerified = x.u.IsEmailVerified,
                    IsActive = x.u.IsActive,
                    CreatedAt = x.u.CreatedAt
                }).ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Receiver ID,Full Name,Email,Phone,Blood Group Needed,Hospital Name,City,Urgency Level,Email Verification,Account Status,Registration Date");
            foreach (var r in receivers)
            {
                csv.AppendLine($"REC-{r.UserId.ToString().PadLeft(4, '0')},{r.FullName},{r.Email},{r.Phone},{r.BloodGroupNeeded},{r.HospitalName},{r.City},{r.UrgencyLevel},{(r.IsEmailVerified ? "Verified" : "Not Verified")},{(r.IsActive ? "Active" : "Inactive")},{r.CreatedAt:dd-MMM-yyyy}");
            }
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "Receivers.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportReceiversPdf()
        {
            var receivers = await _context.Users
                .Where(u => u.Role == "Receiver")
                .Join(_context.ReceiverProfiles, u => u.UserId, r => r.UserId, (u, r) => new { u, r })
                .OrderByDescending(x => x.u.CreatedAt)
                .Select(x => new ReceiverViewModel
                {
                    UserId = x.u.UserId,
                    FullName = x.u.FullName,
                    Email = x.u.Email,
                    Phone = x.u.Phone,
                    BloodGroupNeeded = x.r.BloodGroupNeeded,
                    HospitalName = x.r.HospitalName,
                    City = x.r.City,
                    UrgencyLevel = x.r.UrgencyLevel,
                    IsActive = x.u.IsActive,
                    CreatedAt = x.u.CreatedAt
                }).ToListAsync();

            var html = new StringBuilder();
            html.AppendLine("<html><body><h2>Receivers List</h2><table border='1' cellpadding='5' cellspacing='0'><tr><th>ID</th><th>Name</th><th>Email</th><th>Phone</th><th>Blood Group</th><th>Hospital</th><th>City</th><th>Urgency</th><th>Status</th></tr>");
            foreach (var r in receivers)
            {
                html.AppendLine($"<tr><td>REC-{r.UserId.ToString().PadLeft(4, '0')}</td><td>{r.FullName}</td><td>{r.Email}</td><td>{r.Phone}</td><td>{r.BloodGroupNeeded}</td><td>{r.HospitalName}</td><td>{r.City}</td><td>{r.UrgencyLevel}</td><td>{(r.IsActive ? "Active" : "Inactive")}</td></tr>");
            }
            html.AppendLine("</table></body></html>");

            return File(Encoding.UTF8.GetBytes(html.ToString()), "application/pdf", "Receivers.pdf");
        }

        // ==================== HOSPITALS MANAGEMENT ====================

        [HttpGet]
        public async Task<IActionResult> Hospitals()
        {
            var hospitals = await _context.Users
                .Where(u => u.Role == "Hospital")
                .Join(_context.HospitalProfiles,
                      u => u.UserId,
                      h => h.UserId,
                      (u, h) => new HospitalViewModel
                      {
                          UserId = u.UserId,
                          HospitalName = h.HospitalName,
                          Email = u.Email,
                          Phone = u.Phone,
                          LicenseNumber = h.LicenseNumber,
                          ContactPerson = h.ContactPerson,
                          City = h.City,
                          VerificationStatus = h.VerificationStatus ?? "Pending",
                          IsEmailVerified = u.IsEmailVerified,
                          IsActive = u.IsActive,
                          CreatedAt = u.CreatedAt
                      })
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();

            var viewModel = new HospitalListViewModel
            {
                Hospitals = hospitals,
                TotalHospitals = await _context.Users.CountAsync(u => u.Role == "Hospital"),
                VerifiedHospitals = hospitals.Count(h => h.VerificationStatus == "Approved"),
                PendingVerification = hospitals.Count(h => h.VerificationStatus == "Pending"),
                RejectedHospitals = hospitals.Count(h => h.VerificationStatus == "Rejected"),
                ActiveHospitals = hospitals.Count(h => h.IsActive),
                AvailableCities = hospitals.Where(h => !string.IsNullOrEmpty(h.City))
                                           .Select(h => h.City)
                                           .Distinct()
                                           .OrderBy(c => c)
                                           .ToList()
            };

            ViewBag.UnreadMessageCount = await _context.ContactMessages.CountAsync(m => m.Status == "New");

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetHospitalDetails(int userId)
        {
            var hospital = await _context.Users
                .Where(u => u.UserId == userId && u.Role == "Hospital")
                .Join(_context.HospitalProfiles,
                      u => u.UserId,
                      h => h.UserId,
                      (u, h) => new HospitalDetailsViewModel
                      {
                          HospitalName = h.HospitalName,
                          LicenseNumber = h.LicenseNumber,
                          ContactPerson = h.ContactPerson,
                          Email = u.Email,
                          Phone = u.Phone,
                          IsEmailVerified = u.IsEmailVerified,
                          IsActive = u.IsActive,
                          CreatedAt = u.CreatedAt,
                          Address = h.Address,
                          City = h.City,
                          VerificationStatus = h.VerificationStatus ?? "Pending",
                          LicenseDocumentPath = h.LicenseDocumentPath
                      })
                .FirstOrDefaultAsync();

            if (hospital == null)
                return NotFound();

            return Json(hospital);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateHospital(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.Role != "Hospital")
                    return Json(new { success = false, message = "Hospital not found" });

                user.IsActive = true;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                await LogAdminAction($"Activated hospital account: {user.Email}");

                return Json(new { success = true, message = "Hospital activated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateHospital(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.Role != "Hospital")
                    return Json(new { success = false, message = "Hospital not found" });

                user.IsActive = false;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                await LogAdminAction($"Deactivated hospital account: {user.Email}");

                return Json(new { success = true, message = "Hospital deactivated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportHospitalsExcel()
        {
            var hospitals = await _context.Users
                .Where(u => u.Role == "Hospital")
                .Join(_context.HospitalProfiles, u => u.UserId, h => h.UserId, (u, h) => new { u, h })
                .OrderByDescending(x => x.u.CreatedAt)
                .Select(x => new HospitalViewModel
                {
                    UserId = x.u.UserId,
                    HospitalName = x.h.HospitalName,
                    Email = x.u.Email,
                    Phone = x.u.Phone,
                    LicenseNumber = x.h.LicenseNumber,
                    ContactPerson = x.h.ContactPerson,
                    City = x.h.City,
                    VerificationStatus = x.h.VerificationStatus ?? "Pending",
                    IsEmailVerified = x.u.IsEmailVerified,
                    IsActive = x.u.IsActive,
                    CreatedAt = x.u.CreatedAt
                }).ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Hospital ID,Hospital Name,Email,License Number,Contact Person,City,Verification Status,Email Verification,Account Status,Registration Date");
            foreach (var h in hospitals)
            {
                csv.AppendLine($"HSP-{h.UserId.ToString().PadLeft(4, '0')},{h.HospitalName},{h.Email},{h.LicenseNumber},{h.ContactPerson},{h.City},{h.VerificationStatus},{(h.IsEmailVerified ? "Verified" : "Not Verified")},{(h.IsActive ? "Active" : "Inactive")},{h.CreatedAt:dd-MMM-yyyy}");
            }
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "Hospitals.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportHospitalsPdf()
        {
            var hospitals = await _context.Users
                .Where(u => u.Role == "Hospital")
                .Join(_context.HospitalProfiles, u => u.UserId, h => h.UserId, (u, h) => new { u, h })
                .OrderByDescending(x => x.u.CreatedAt)
                .Select(x => new HospitalViewModel
                {
                    UserId = x.u.UserId,
                    HospitalName = x.h.HospitalName,
                    Email = x.u.Email,
                    Phone = x.u.Phone,
                    LicenseNumber = x.h.LicenseNumber,
                    ContactPerson = x.h.ContactPerson,
                    City = x.h.City,
                    VerificationStatus = x.h.VerificationStatus ?? "Pending",
                    IsActive = x.u.IsActive,
                    CreatedAt = x.u.CreatedAt
                }).ToListAsync();

            var html = new StringBuilder();
            html.AppendLine("<html><body><h2>Hospitals List</h2><table border='1' cellpadding='5' cellspacing='0'><tr><th>ID</th><th>Name</th><th>Email</th><th>License</th><th>Contact</th><th>City</th><th>Verification</th><th>Status</th></tr>");
            foreach (var h in hospitals)
            {
                html.AppendLine($"<tr><td>HSP-{h.UserId.ToString().PadLeft(4, '0')}</td><td>{h.HospitalName}</td><td>{h.Email}</td><td>{h.LicenseNumber}</td><td>{h.ContactPerson}</td><td>{h.City}</td><td>{h.VerificationStatus}</td><td>{(h.IsActive ? "Active" : "Inactive")}</td></tr>");
            }
            html.AppendLine("</table></body></html>");

            return File(Encoding.UTF8.GetBytes(html.ToString()), "application/pdf", "Hospitals.pdf");
        }

        // ==================== HELPER METHOD (ONLY ONCE) ====================

        private async Task LogAdminAction(string action)
        {
            try
            {
                var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(adminId, out int adminUserId))
                {
                    var log = new AdminLog
                    {
                        AdminId = adminUserId,
                        Action = action,
                        CreatedAt = DateTime.Now
                    };
                    _context.AdminLogs.Add(log);
                    await _context.SaveChangesAsync();
                }
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }
}