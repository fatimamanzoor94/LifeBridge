using Khoon_e_Hayat.Data;
using Khoon_e_Hayat.Models.Entities;
using Khoon_e_Hayat.ViewModels;
using Khoon_e_Hayat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Khoon_e_Hayat.Controllers
{
    [Authorize(Roles = "Donor")]
    public class DonorController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public DonorController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            var donorProfile = await _context.DonorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (donorProfile != null)
            {
                await ProcessAutomaticNotifications(userId, donorProfile);
            }

            if (user == null || donorProfile == null) return RedirectToAction("Login", "Account");

            var vm = new DonorDashboardViewModel
            {
                BloodGroup = donorProfile.BloodGroup,
                IsAvailable = donorProfile.IsAvailable,
                LastDonationDate = donorProfile.LastDonationDate,
                TotalDonations = donorProfile.SuccessfulDonations ?? 0,
                SmartMatches = await _context.DonorMatches.CountAsync(m => m.DonorId == userId),
                ActiveBloodRequests = await _context.BloodRequests.CountAsync(r => r.BloodGroup == donorProfile.BloodGroup && r.RequestStatus == "Pending"),
                EmergencyAlerts = await _context.EmergencyAlerts.CountAsync(e => e.AlertStatus == "Active"),
                Notifications = await _context.NotificationLogs.CountAsync(n => n.DonorId == userId && n.Status == "Sent")
            };

            if (donorProfile.LastDonationDate.HasValue)
            {
                var daysSince = (DateTime.Now - donorProfile.LastDonationDate.Value).Days;
                if (daysSince < 90)
                {
                    vm.DaysUntilEligible = 90 - daysSince;
                    vm.EligibilityStatus = $"Eligible in {vm.DaysUntilEligible} days";
                }
                else
                {
                    vm.EligibilityStatus = "You are eligible to donate!";
                    vm.DaysUntilEligible = 0;
                }
            }
            else
            {
                vm.EligibilityStatus = "You are eligible to donate!";
                vm.DaysUntilEligible = 0;
            }

            var twelveMonthsAgo = DateTime.Now.AddMonths(-11);
            var startOfMonth = new DateTime(twelveMonthsAgo.Year, twelveMonthsAgo.Month, 1);

            var monthlyDonations = await _context.DonorMatches
                .Where(m => m.DonorId == userId &&
                            (m.Status == "Fulfilled" || m.Status == "DonationCompleted" || m.Status == "Accepted") &&
                            m.MatchDate >= startOfMonth)
                .GroupBy(m => new { Year = m.MatchDate.Year, Month = m.MatchDate.Month })
                .Select(g => new {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count(),
                    FirstDate = g.Min(m => m.MatchDate)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            for (int i = 0; i < 12; i++)
            {
                var date = DateTime.Now.AddMonths(-11 + i);
                var monthData = monthlyDonations.FirstOrDefault(m => m.Year == date.Year && m.Month == date.Month);

                vm.MonthlyDonationTrend.Add(new ChartData
                {
                    Label = date.ToString("MMM yyyy"),
                    Value = monthData?.Count ?? 0,
                    Color = "#2F6E9B"
                });
            }

            var statusCounts = await _context.DonorMatches
                .Where(m => m.DonorId == userId)
                .GroupBy(m => m.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var allStatuses = new[] { "Matched", "Fulfilled", "Rejected", "Cancelled" };
            vm.DonationStatus = allStatuses.Select(s => new ChartData
            {
                Label = s,
                Value = statusCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0,
                Color = s switch
                {
                    "Matched" => "#0dcaf0",
                    "Fulfilled" => "#198754",
                    "Rejected" => "#dc3545",
                    "Cancelled" => "#6c757d",
                    _ => "#6c757d"
                }
            }).ToList();

            vm.RecentEmergencyAlerts = await _context.EmergencyAlerts
                .Join(_context.BloodRequests, ea => ea.RequestId, br => br.RequestId, (ea, br) => new { ea, br })
                .Where(x => x.br.BloodGroup == donorProfile.BloodGroup && x.ea.AlertStatus == "Active")
                .OrderByDescending(x => x.ea.CreatedAt)
                .Take(5)
                .Select(x => new RecentEmergencyAlertItem
                {
                    AlertId = x.ea.AlertId,
                    BloodGroup = x.br.BloodGroup,
                    HospitalName = x.br.HospitalName,
                    City = x.br.City,
                    UrgencyLevel = x.ea.PriorityLevel,
                    CreatedDate = x.ea.CreatedAt
                }).ToListAsync();

            vm.RecentSmartMatches = await _context.DonorMatches
                .Join(_context.BloodRequests, m => m.BloodRequestId, br => br.RequestId, (m, br) => new { m, br })
                .Where(x => x.m.DonorId == userId)
                .OrderByDescending(x => x.m.MatchDate)
                .Take(5)
                .Select(x => new RecentSmartMatchItem
                {
                    MatchId = x.m.MatchId,
                    BloodGroup = x.br.BloodGroup,
                    HospitalName = x.br.HospitalName,
                    City = x.br.City,
                    Status = x.m.Status,
                    MatchDate = x.m.MatchDate
                }).ToListAsync();

            vm.RecentNotifications = await _context.NotificationLogs
                .Where(n => n.DonorId == userId)
                .OrderByDescending(n => n.SentAt)
                .Take(5)
                .Select(n => new RecentNotificationItem
                {
                    LogId = n.LogId,
                    Category = n.Category,
                    Subject = n.Subject ?? "System Notification",
                    Status = n.Status,
                    SentAt = n.SentAt
                }).ToListAsync();

            ViewData["Title"] = "Donor Dashboard";
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> MyDonations()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = await _context.Users.FindAsync(userId);
                var donorProfile = await _context.DonorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);

                if (user == null) return RedirectToAction("Login", "Account");

                // ✅ CORRECTED QUERY - DonorProfile se join karein
                var donations = await _context.Donations
                    .Include(d => d.Donor)  // DonorProfile include karein
                    .Where(d => d.Donor.UserId == userId)  // ✅ UserId se match karein
                    .OrderByDescending(d => d.DonationDate)
                    .Select(d => new DonorDonationHistoryItem
                    {
                        DonationId = d.DonationId,
                        DonorName = user.FullName,
                        BloodGroup = d.BloodGroup,
                        HospitalName = d.HospitalName,
                        ReceiverName = "Anonymous",
                        DonationDate = d.DonationDate,
                        Status = "Completed",
                        Location = d.HospitalName,
                        DonationType = "Voluntary",
                        BloodQuantity = 1,
                        ResponseTime = "N/A",
                        MedicalNotes = ""
                    })
                    .ToListAsync();

                // Debugging ke liye
                System.Diagnostics.Debug.WriteLine($"=== MyDonations Debug ===");
                System.Diagnostics.Debug.WriteLine($"User ID: {userId}");
                System.Diagnostics.Debug.WriteLine($"DonorProfile Found: {donorProfile != null}");
                if (donorProfile != null)
                {
                    System.Diagnostics.Debug.WriteLine($"DonorProfile.DonorId: {donorProfile.DonorId}");
                }
                System.Diagnostics.Debug.WriteLine($"Donations Found: {donations.Count}");

                DateTime? nextEligibleDate = null;
                int daysUntilEligible = 0;
                bool isEligibleToday = true;
                string eligibilityStatus = "You are eligible to donate!";

                if (donorProfile?.LastDonationDate.HasValue == true)
                {
                    var daysSince = (DateTime.Now - donorProfile.LastDonationDate.Value).Days;
                    if (daysSince < 90)
                    {
                        nextEligibleDate = donorProfile.LastDonationDate.Value.AddDays(90);
                        daysUntilEligible = 90 - daysSince;
                        isEligibleToday = false;
                        eligibilityStatus = $"Eligible in {daysUntilEligible} days";
                    }
                }

                string donorStatus = "New";
                var totalDonations = donations.Count;
                if (totalDonations >= 50) donorStatus = "Platinum";
                else if (totalDonations >= 25) donorStatus = "Hero";
                else if (totalDonations >= 10) donorStatus = "Regular";
                else if (totalDonations >= 3) donorStatus = "Active";

                int livesSaved = donations.Count * 3;

                var availableHospitals = donations
                    .Where(d => !string.IsNullOrEmpty(d.HospitalName))
                    .Select(d => d.HospitalName)
                    .Distinct()
                    .OrderBy(h => h)
                    .ToList();

                var availableBloodGroups = donations
                    .Select(d => d.BloodGroup)
                    .Distinct()
                    .OrderBy(bg => bg)
                    .ToList();

                var monthlyDonations = donations
                    .GroupBy(d => new { d.DonationDate.Year, d.DonationDate.Month })
                    .Select(g => new ChartData
                    {
                        Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                        Value = g.Count(),
                        Color = "#dc3545"
                    })
                    .OrderBy(x => DateTime.Parse(x.Label + " 01"))
                    .ToList();

                var yearlyDonations = donations
                    .GroupBy(d => d.DonationDate.Year)
                    .Select(g => new ChartData
                    {
                        Label = g.Key.ToString(),
                        Value = g.Count(),
                        Color = "#0d6efd"
                    })
                    .OrderBy(x => x.Label)
                    .ToList();

                var donationTypeDistribution = donations
                    .GroupBy(d => d.DonationType)
                    .Select(g => new ChartData
                    {
                        Label = g.Key,
                        Value = g.Count(),
                        Color = g.Key == "Emergency" ? "#dc3545" : "#28a745"
                    })
                    .ToList();

                var vm = new DonorDonationViewModel
                {
                    TotalDonations = totalDonations,
                    SuccessfulDonations = donations.Count(d => d.Status == "Completed"),
                    PendingDonations = donations.Count(d => d.Status == "Pending"),
                    EstimatedLivesSaved = livesSaved,
                    LastDonationDate = donorProfile?.LastDonationDate,
                    NextEligibleDate = nextEligibleDate,
                    DaysUntilEligible = daysUntilEligible,
                    IsEligibleToday = isEligibleToday,
                    EligibilityStatus = eligibilityStatus,
                    DonorStatus = donorStatus,
                    Donations = donations,
                    MonthlyDonations = monthlyDonations,
                    YearlyDonations = yearlyDonations,
                    DonationTypeDistribution = donationTypeDistribution,
                    AvailableHospitals = availableHospitals,
                    AvailableBloodGroups = availableBloodGroups
                };

                ViewData["Title"] = "My Donations";
                return View(vm);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in MyDonations: {ex.Message}");
                return RedirectToAction("Dashboard", "Donor");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EmergencyAlerts()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            var donorProfile = await _context.DonorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);

            if (user == null || donorProfile == null) return RedirectToAction("Login", "Account");

            bool isEligible = true;
            DateTime? nextEligibleDate = null;
            int daysUntilEligible = 0;

            if (donorProfile.LastDonationDate.HasValue)
            {
                var daysSince = (DateTime.Now - donorProfile.LastDonationDate.Value).Days;
                if (daysSince < 90)
                {
                    isEligible = false;
                    daysUntilEligible = 90 - daysSince;
                    nextEligibleDate = donorProfile.LastDonationDate.Value.AddDays(90);
                }
            }
            bool isInCooldown = !isEligible || !donorProfile.IsAvailable;

            var compatDict = GetCompatibleRecipientGroups();
            var compatibleGroups = compatDict.ContainsKey(donorProfile.BloodGroup ?? "")
                ? compatDict[donorProfile.BloodGroup]
                : new List<string>();

            var requestsWithMatches = await _context.BloodRequests
                .Where(br => compatibleGroups.Contains(br.BloodGroup))
                .Select(br => new
                {
                    Request = br,
                    MatchCount = _context.DonorMatches.Count(dm => dm.BloodRequestId == br.RequestId),
                    MyMatch = _context.DonorMatches.FirstOrDefault(dm => dm.BloodRequestId == br.RequestId && dm.DonorId == userId)
                })
                .ToListAsync();

            var alerts = new List<DonorEmergencyAlertItem>();
            int myResponses = 0;

            foreach (var item in requestsWithMatches)
            {
                var br = item.Request;
                if (item.MyMatch != null) myResponses++;

                string status = br.RequestStatus switch
                {
                    "Pending" => "Open",
                    "Pending Confirmation" => "Pending Confirmation",
                    "Matched" => "Accepted",
                    "Fulfilled" => "Fulfilled",
                    "Cancelled" => "Cancelled",
                    _ => "Expired"
                };

                if (item.MyMatch != null)
                {
                    if (item.MyMatch.Status == "PendingConfirmation") status = "Pending Confirmation";
                    else if (item.MyMatch.Status == "Accepted" || item.MyMatch.Status == "Matched") status = "Accepted";
                    else if (status == "Open") status = "Pending Confirmation";
                }

                double distanceKm = (br.City == donorProfile.City) ? 3.5 : 15.0;
                int travelTime = (br.City == donorProfile.City) ? 12 : 35;

                var alertItem = new DonorEmergencyAlertItem
                {
                    AlertId = br.RequestId,
                    BloodGroup = br.BloodGroup,
                    HospitalName = br.HospitalName,
                    HospitalAddress = br.City + " General Hospital",
                    HospitalContact = br.HospitalContact ?? "+92-300-1234907",
                    City = br.City,
                    UrgencyLevel = br.UrgencyLevel ?? "Normal",
                    UnitsRequired = br.UnitsRequired ?? 1,
                    RequestDate = br.CreatedAt,
                    RequiredBefore = br.RequiredDate ?? br.CreatedAt.AddHours(4),
                    Status = status,
                    DistanceKm = distanceKm,
                    TravelTimeMins = travelTime,
                    RespondedDonors = item.MatchCount,
                    EmergencyDescription = "Urgent blood requirement for emergency surgery.",
                    SpecialInstructions = "Please bring CNIC and previous donation records if any.",
                    PatientAge = br.PatientAge ?? 30
                };

                alertItem.UrgencyBorderColor = alertItem.UrgencyLevel switch
                {
                    "Critical" => "danger",
                    "High" => "warning",
                    "Medium" => "primary",
                    _ => "success"
                };
                alertItem.UrgencyBadgeColor = alertItem.UrgencyBorderColor;

                alertItem.StatusColor = alertItem.Status switch
                {
                    "Open" => "danger",
                    "Pending Confirmation" => "info",
                    "Accepted" => "warning",
                    "Fulfilled" => "success",
                    _ => "secondary"
                };

                var postedDiff = DateTime.Now - alertItem.RequestDate;
                alertItem.PostedTimeText = postedDiff.TotalMinutes < 1 ? "Just now" :
                                           postedDiff.TotalMinutes < 60 ? $"Posted {(int)postedDiff.TotalMinutes} mins ago" :
                                           postedDiff.TotalHours < 24 ? $"Posted {(int)postedDiff.TotalHours} hours ago" :
                                           $"Posted {(int)postedDiff.TotalDays} days ago";

                alertItem.ProgressPercent = alertItem.UnitsRequired > 0 ? (int)((alertItem.RespondedDonors / (double)alertItem.UnitsRequired) * 100) : 0;
                if (alertItem.ProgressPercent > 100) alertItem.ProgressPercent = 100;
                alertItem.ProgressColor = alertItem.ProgressPercent >= 100 ? "bg-success" : (alertItem.ProgressPercent >= 50 ? "bg-warning" : "bg-danger");

                alertItem.RequiredBeforeDisplay = alertItem.RequiredBefore.ToString("dd MMM, hh:mm tt");
                alertItem.HospitalNameLower = alertItem.HospitalName?.ToLower() ?? "";
                alertItem.CityLower = alertItem.City?.ToLower() ?? "";
                alertItem.RequiredBeforeIso = alertItem.RequiredBefore.ToString("yyyy-MM-ddTHH:mm:ss");
                alertItem.RequestDateIso = alertItem.RequestDate.ToString("yyyy-MM-ddTHH:mm:ss");

                alerts.Add(alertItem);
            }

            alerts = alerts
                .OrderBy(a => a.UrgencyLevel switch
                {
                    "Critical" => 1,
                    "High" => 2,
                    "Medium" => 3,
                    _ => 4
                })
                .ThenBy(a => a.RequiredBefore)
                .ToList();

            int expiringSoon = alerts.Count(a => (a.Status == "Open" || a.Status == "Accepted" || a.Status == "Pending Confirmation") &&
                                                 a.RequiredBefore <= DateTime.Now.AddHours(24) &&
                                                 a.RequiredBefore > DateTime.Now);

            var vm = new DonorEmergencyAlertViewModel
            {
                TotalAlerts = alerts.Count,
                ActiveAlerts = alerts.Count(r => r.Status == "Open" || r.Status == "Accepted" || r.Status == "Pending Confirmation"),
                CriticalCases = alerts.Count(r => r.UrgencyLevel == "Critical"),
                ExpiringSoon = expiringSoon,
                NearbyEmergencies = alerts.Count(r => r.DistanceKm <= 20),
                MyEmergencyResponses = myResponses,
                AverageResponseTime = "12 mins",

                IsInCooldown = isInCooldown,
                NextEligibleDate = nextEligibleDate,
                DaysUntilEligible = daysUntilEligible,
                NextEligibleDateDisplay = nextEligibleDate?.ToString("dd MMM yyyy"),
                CurrentDateDisplay = DateTime.Now.ToString("dd MMM yyyy"),

                Alerts = alerts,
                AvailableCities = alerts.Select(r => r.City).Distinct().Where(c => c != null).OrderBy(c => c).ToList(),
                AvailableBloodGroups = alerts.Select(r => r.BloodGroup).Distinct().OrderBy(bg => bg).ToList()
            };

            ViewData["Title"] = "Emergency Alerts";
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondToEmergency(int alertId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            var donorProfile = await _context.DonorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);

            if (user == null || donorProfile == null)
                return Json(new { success = false, message = "User profile not found." });

            if (donorProfile.LastDonationDate.HasValue)
            {
                var daysSince = (DateTime.Now - donorProfile.LastDonationDate.Value).Days;
                if (daysSince < 90)
                    return Json(new { success = false, message = "You are currently in the mandatory donation cooldown period." });
            }

            var existingMatch = await _context.DonorMatches.FirstOrDefaultAsync(m => m.DonorId == userId && m.BloodRequestId == alertId);
            if (existingMatch != null)
                return Json(new { success = false, message = "You have already responded to this emergency." });

            var bloodRequest = await _context.BloodRequests.FindAsync(alertId);
            if (bloodRequest == null || (bloodRequest.RequestStatus != "Pending" && bloodRequest.RequestStatus != "Pending Confirmation"))
                return Json(new { success = false, message = "This emergency is no longer accepting responses." });

            double distanceKm = (bloodRequest.City == donorProfile.City) ? 3.5 : 15.0;
            string travelTime = (bloodRequest.City == donorProfile.City) ? "12 mins" : "35 mins";

            var match = new DonorMatch
            {
                DonorId = userId,
                BloodRequestId = alertId,
                MatchDate = DateTime.Now,
                Status = "PendingConfirmation",
                MatchScore = 100,
                DistanceKm = distanceKm,
                TravelTime = travelTime
            };
            _context.DonorMatches.Add(match);

            if (bloodRequest.RequestStatus == "Pending")
                bloodRequest.RequestStatus = "Pending Confirmation";

            await _context.SaveChangesAsync();

            bool notifExists = await _context.DonorNotifications.AnyAsync(n => n.DonorId == userId && n.Category == "EmergencyAlert" && n.ReferenceId == alertId);
            if (!notifExists)
            {
                _context.DonorNotifications.Add(new DonorNotification
                {
                    DonorId = userId,
                    Title = "Emergency Alert Responded",
                    Message = $"You have successfully responded to the emergency blood request for {bloodRequest.BloodGroup} at {bloodRequest.HospitalName}.",
                    Category = "EmergencyAlert",
                    ReferenceId = alertId,
                    ActionUrl = "/Donor/EmergencyAlerts",
                    IsRead = false,
                    CreatedDate = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            try
            {
                await _emailService.SendEmergencyVolunteerConfirmationToDonorAsync(user.Email, user.FullName ?? "Donor", alertId, bloodRequest.BloodGroup, bloodRequest.HospitalName);
                var hospital = await _context.Users.FindAsync(bloodRequest.ReceiverId);
                if (hospital != null && !string.IsNullOrEmpty(hospital.Email))
                {
                    await _emailService.SendNewVolunteerNotificationToHospitalAsync(hospital.Email, hospital.FullName ?? "Hospital Admin", alertId, bloodRequest.BloodGroup, user.FullName ?? "A Donor", user.Phone ?? "N/A", distanceKm, travelTime);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Notification error: {ex.Message}"); }

            return Json(new { success = true, message = "Thank you! Your response has been received." });
        }

        [HttpGet]
        public async Task<IActionResult> Notifications(
    string SearchQuery = "",
    string Category = "all",
    string Status = "all",
    string SortOrder = "newest",
    int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            const int pageSize = 5;  

            var query = _context.DonorNotifications
                .Where(n => n.DonorId == userId)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(SearchQuery))
            {
                query = query.Where(n => n.Title.Contains(SearchQuery) || n.Message.Contains(SearchQuery));
            }

            if (Category != "all")
            {
                query = query.Where(n => n.Category == Category);
            }

            if (Status == "unread")
            {
                query = query.Where(n => !n.IsRead);
            }
            else if (Status == "read")
            {
                query = query.Where(n => n.IsRead);
            }

            // Apply sorting
            query = SortOrder switch
            {
                "oldest" => query.OrderBy(n => n.CreatedDate),
                _ => query.OrderByDescending(n => n.CreatedDate)
            };

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var notifications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new DonorNotificationItem
                {
                    NotificationId = n.NotificationId,
                    Category = n.Category,
                    Title = n.Title,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedDate = n.CreatedDate,
                    ActionUrl = n.ActionUrl,
                    ReferenceId = n.ReferenceId
                })
                .ToListAsync();

            var vm = new DonorNotificationViewModel
            {
                TotalNotifications = await _context.DonorNotifications.CountAsync(n => n.DonorId == userId),
                UnreadNotifications = await _context.DonorNotifications.CountAsync(n => n.DonorId == userId && !n.IsRead),
                EmergencyAlerts = await _context.DonorNotifications.CountAsync(n => n.DonorId == userId && n.Category == "EmergencyAlert"),
                SmartMatches = await _context.DonorNotifications.CountAsync(n => n.DonorId == userId && n.Category == "SmartMatch"),
                Donations = await _context.DonorNotifications.CountAsync(n => n.DonorId == userId && n.Category == "DonationUpdate"),

                // Pagination
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = page,
                Notifications = notifications
            };

            ViewData["Title"] = "Notifications";
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var notif = await _context.DonorNotifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.DonorId == userId);

            if (notif != null)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Notification marked as read successfully." });
            }
            return Json(new { success = false, message = "Notification not found." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsUnread(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var notif = await _context.DonorNotifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.DonorId == userId);

            if (notif != null)
            {
                notif.IsRead = false;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Notification marked as unread successfully." });
            }
            return Json(new { success = false, message = "Notification not found." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var notif = await _context.DonorNotifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.DonorId == userId);

            if (notif != null)
            {
                _context.DonorNotifications.Remove(notif);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Notification deleted successfully." });
            }
            return Json(new { success = false, message = "Notification not found." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAction(string action, List<int> ids)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var notifications = await _context.DonorNotifications
                .Where(n => n.DonorId == userId && ids.Contains(n.NotificationId))
                .ToListAsync();

            foreach (var notif in notifications)
            {
                switch (action)
                {
                    case "markread":
                        notif.IsRead = true;
                        break;
                    case "markunread":
                        notif.IsRead = false;
                        break;
                    case "delete":
                        _context.DonorNotifications.Remove(notif);
                        break;
                }
            }

            await _context.SaveChangesAsync();

            string message = action switch
            {
                "markread" => $"{notifications.Count} notification(s) marked as read.",
                "markunread" => $"{notifications.Count} notification(s) marked as unread.",
                "delete" => $"{notifications.Count} notification(s) deleted.",
                _ => "Action completed successfully."
            };

            return Json(new { success = true, message = message });
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationsLive()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var total = await _context.DonorNotifications.CountAsync(n => n.DonorId == userId);
            var unread = await _context.DonorNotifications.CountAsync(n => n.DonorId == userId && !n.IsRead);
            var emergency = await _context.DonorNotifications.CountAsync(n => n.DonorId == userId && n.Category == "EmergencyAlert");
            var smartMatches = await _context.DonorNotifications.CountAsync(n => n.DonorId == userId && n.Category == "SmartMatch");

            return Json(new
            {
                success = true,
                data = new
                {
                    total,
                    unread,
                    emergency,
                    smartMatches
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadNotificationCount()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var count = await _context.DonorNotifications.CountAsync(n => n.DonorId == userId && !n.IsRead);
            return Json(new { unread = count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptSmartMatch(int requestId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            var donorProfile = await _context.DonorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);

            if (user == null || donorProfile == null)
                return Json(new { success = false, message = "User profile not found." });

            if (donorProfile.LastDonationDate.HasValue)
            {
                var daysSince = (DateTime.Now - donorProfile.LastDonationDate.Value).Days;
                if (daysSince < 90)
                    return Json(new { success = false, message = "You are currently in the mandatory donation cooldown period." });
            }

            if (!donorProfile.IsAvailable)
                return Json(new { success = false, message = "Your donor profile is currently marked as unavailable." });

            var bloodRequest = await _context.BloodRequests.FindAsync(requestId);
            if (bloodRequest == null)
                return Json(new { success = false, message = "Blood request not found." });

            if (bloodRequest.RequestStatus != "Pending" && bloodRequest.RequestStatus != "Approved" && bloodRequest.RequestStatus != "Searching Donor")
                return Json(new { success = false, message = "This request is no longer accepting responses." });

            var existingMatch = await _context.DonorMatches.FirstOrDefaultAsync(m => m.DonorId == userId && m.BloodRequestId == requestId);

            if (existingMatch != null)
            {
                if (existingMatch.Status == "Accepted" || existingMatch.Status == "Matched" || existingMatch.Status == "Fulfilled" || existingMatch.Status == "Completed")
                    return Json(new { success = false, message = "You have already accepted or fulfilled this match." });

                existingMatch.Status = "Accepted";
                existingMatch.MatchDate = DateTime.Now;

                if (!existingMatch.HospitalId.HasValue && bloodRequest.HospitalId.HasValue)
                {
                    existingMatch.HospitalId = bloodRequest.HospitalId.Value;
                }
            }
            else
            {
                double distanceKm = (bloodRequest.City == donorProfile.City) ? 3.5 : 15.0;
                var newMatch = new DonorMatch
                {
                    DonorId = userId,
                    BloodRequestId = requestId,
                    HospitalId = bloodRequest.HospitalId,
                    MatchDate = DateTime.Now,
                    Status = "Accepted",
                    MatchScore = 100,
                    DistanceKm = distanceKm,
                    TravelTime = (bloodRequest.City == donorProfile.City) ? "12 mins" : "35 mins"
                };
                _context.DonorMatches.Add(newMatch);
            }

            if (bloodRequest.RequestStatus == "Pending" || bloodRequest.RequestStatus == "Searching Donor")
            {
                bloodRequest.RequestStatus = "Matched";
            }

            await _context.SaveChangesAsync();

            // ✅ DONOR Notification
            bool notifExists = await _context.DonorNotifications.AnyAsync(n => n.DonorId == userId && n.Category == "SmartMatch" && n.ReferenceId == requestId);
            if (!notifExists)
            {
                _context.DonorNotifications.Add(new DonorNotification
                {
                    DonorId = userId,
                    Title = "Smart Match Accepted",
                    Message = $"You have accepted a smart match for {bloodRequest.BloodGroup} at {bloodRequest.HospitalName}. The hospital has been notified.",
                    Category = "SmartMatch",
                    ReferenceId = requestId,
                    ActionUrl = "/Donor/SmartMatches",
                    IsRead = false,
                    CreatedDate = DateTime.Now
                });
            }

            // ✅ Hospital Notification
            if (bloodRequest.HospitalId.HasValue)
            {
                _context.HospitalNotifications.Add(new HospitalNotification
                {
                    HospitalId = bloodRequest.HospitalId.Value,
                    Title = "New Donor Response",
                    Message = $"A donor has accepted the request for {bloodRequest.BloodGroup} (Req #{requestId}). Check Donor Responses page.",
                    Category = "DonorResponse",
                    Priority = "High",
                    RequestId = requestId,
                    DonorId = userId,
                    CreatedDate = DateTime.Now
                });
            }

            // ✅ RECEIVER Notification (Donor ne accept kar liya)
            if (bloodRequest.ReceiverId != null)
            {
                _context.ReceiverNotifications.Add(new ReceiverNotification
                {
                    ReceiverId = bloodRequest.ReceiverId,
                    RequestId = requestId,
                    Title = "Donor Accepted Your Request",
                    Message = $"Great news! A compatible donor has accepted your blood request REQ-{requestId:D4}. The hospital will contact you shortly.",
                    Category = "DonorMatch",
                    Priority = "High",
                    HospitalName = bloodRequest.HospitalName,
                    BloodGroup = bloodRequest.BloodGroup,
                    RequestStatus = "Donor Accepted",
                    IsRead = false,
                    CreatedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            // ✅ EMAILS - CORRECT RECIPIENTS
            try
            {
                // 1. DONOR ko confirmation email bhejo
                if (!string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendDonationRequestEmailAsync(
                        user.Email,
                        user.FullName ?? "Donor",
                        requestId,
                        bloodRequest.BloodGroup,
                        bloodRequest.HospitalName ?? "Hospital"
                    );
                }

                // 2. RECEIVER/HOSPITAL ko notification email bhejo
                var receiver = await _context.Users.FindAsync(bloodRequest.ReceiverId);
                if (receiver != null && !string.IsNullOrEmpty(receiver.Email))
                {
                    // Receiver ko alag email - Donor accepted
                    await _emailService.SendEmailAsync(
                        receiver.Email,
                        $"Donor Accepted Your Request REQ-{requestId:D4}",
                        $@"<html><body>
                    <h2>Good News!</h2>
                    <p>Dear {receiver.FullName},</p>
                    <p>A compatible donor has accepted your blood request <strong>REQ-{requestId:D4}</strong>.</p>
                    <p><strong>Blood Group:</strong> {bloodRequest.BloodGroup}</p>
                    <p><strong>Hospital:</strong> {bloodRequest.HospitalName}</p>
                    <p>The hospital team will contact you shortly with further instructions.</p>
                    <p>Thank you for using Khoon-e-Hayat.</p>
                    </body></html>",
                        "DonorMatch"
                    );
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Notification error: {ex.Message}"); }

            return Json(new { success = true, message = "Match accepted successfully! The hospital has been notified." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineSmartMatch(int requestId, string reason = "Not specified")
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            var existingMatch = await _context.DonorMatches
                .Include(m => m.BloodRequest)
                .FirstOrDefaultAsync(m => m.BloodRequestId == requestId && m.DonorId == userId);

            if (existingMatch != null)
            {
                if (existingMatch.Status == "Declined" || existingMatch.Status == "Completed")
                    return Json(new { success = false, message = "You have already responded to this request." });

                existingMatch.Status = "Declined";
                existingMatch.RejectionReason = reason;
            }
            else
            {
                var bloodRequest = await _context.BloodRequests.FindAsync(requestId);
                if (bloodRequest != null)
                {
                    _context.DonorMatches.Add(new DonorMatch
                    {
                        DonorId = userId,
                        BloodRequestId = requestId,
                        HospitalId = bloodRequest.HospitalId,
                        Status = "Declined",
                        RejectionReason = reason,
                        MatchDate = DateTime.Now,
                        MatchScore = 0
                    });
                }
            }

            await _context.SaveChangesAsync();

            var request = await _context.BloodRequests.Include(r => r.Receiver).FirstOrDefaultAsync(r => r.RequestId == requestId);
            if (request != null && request.HospitalId.HasValue)
            {
                _context.HospitalNotifications.Add(new HospitalNotification
                {
                    HospitalId = request.HospitalId.Value,
                    Title = "Donor Declined Request",
                    Message = $"A donor declined the request for {request.BloodGroup} (REQ-{requestId:D4}). Reason: {reason}.",
                    Category = "DonorResponse",
                    Priority = "Medium",
                    RequestId = requestId,
                    DonorId = userId,
                    CreatedDate = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Request declined. Thank you for letting us know." });
        }

        [HttpGet]
        public async Task<IActionResult> SmartMatches()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            var donorProfile = await _context.DonorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);

            if (user == null || donorProfile == null) return RedirectToAction("Login", "Account");

            bool isEligible = true;
            DateTime? nextEligibleDate = null;
            int daysUntilEligible = 0;

            if (donorProfile.LastDonationDate.HasValue)
            {
                var daysSince = (DateTime.Now - donorProfile.LastDonationDate.Value).Days;
                if (daysSince < 90)
                {
                    isEligible = false;
                    daysUntilEligible = 90 - daysSince;
                    nextEligibleDate = donorProfile.LastDonationDate.Value.AddDays(90);
                }
            }

            bool isAvailable = donorProfile.IsAvailable;

            // ✅ NEW LOGIC: Fetch ONLY requests officially assigned to THIS donor by the Hospital
            var allAssignedMatches = await _context.DonorMatches
                .Include(m => m.BloodRequest)
                .ThenInclude(br => br.Receiver)
                .Include(m => m.Hospital)
                .Where(m => m.DonorId == userId)
                .OrderByDescending(m => m.MatchDate)
                .ToListAsync();

            var matchItems = new List<DonorSmartMatchItem>();

            foreach (var match in allAssignedMatches)
            {
                var br = match.BloodRequest;
                if (br == null) continue;

                // Map DB status to UI status
                string uiStatus = match.Status switch
                {
                    "Pending Response" => "Available", // View expects "Available" to show Accept/Decline buttons
                    "Accepted" => "Accepted",
                    "Declined" => "Cancelled",
                    "Completed" => "Completed",
                    "Expired" => "Cancelled",
                    "Cancelled" => "Cancelled",
                    _ => match.Status
                };

                int score = match.MatchScore;
                string badge = score >= 90 ? "Excellent Match" : score >= 75 ? "Good Match" : "Fair Match";

                matchItems.Add(new DonorSmartMatchItem
                {
                    MatchId = match.MatchId,
                    RequestId = br.RequestId,
                    BloodGroup = br.BloodGroup,
                    ReceiverName = br.Receiver?.FullName ?? "Anonymous",
                    HospitalName = br.HospitalName ?? match.Hospital?.HospitalName ?? "Unknown Hospital",
                    HospitalAddress = $"{br.HospitalName}, {br.City}",
                    City = br.City,
                    DistanceKm = match.DistanceKm,
                    UnitsRequired = br.UnitsRequired ?? 1,
                    UrgencyLevel = br.UrgencyLevel ?? "Normal",
                    MatchScore = score,
                    CompatibilityBadge = badge,
                    RequestDate = br.CreatedAt,
                    RequiredBeforeTime = br.RequiredDate,
                    Status = uiStatus,
                    Compatibility = br.BloodGroup == donorProfile.BloodGroup ? "Exact Match" : "Compatible",
                    EligibilityStatus = isEligible ? "Eligible" : "Not Eligible",
                    TravelTime = match.TravelTime ?? "N/A",
                    PriorityLevel = br.UrgencyLevel == "Critical" ? "High" : "Normal",
                    MatchReasons = new List<string> {
                "Assigned by Hospital via AI Smart Search",
                br.BloodGroup == donorProfile.BloodGroup ? "Exact Blood Group Match" : "Compatible Blood Group"
            },
                    AdditionalNotes = match.Notes ?? "Please bring your donor ID and maintain hydration before donation."
                });
            }

            var activeMatches = matchItems.Where(m => m.Status == "Available").OrderByDescending(m => m.MatchScore).ToList();
            var historicalMatches = matchItems.Where(m => m.Status != "Available").ToList();

            var vm = new DonorSmartMatchViewModel
            {
                IsInCooldown = !isEligible || !isAvailable,
                NextEligibleDate = nextEligibleDate,
                DaysUntilEligible = daysUntilEligible,

                TotalMatches = activeMatches.Count,
                PerfectMatches = activeMatches.Count(m => m.MatchScore >= 90),
                NearbyRequests = activeMatches.Count(m => m.DistanceKm <= 20),

                AcceptedMatches = historicalMatches.Count(m => m.Status == "Accepted"),
                CompletedDonations = historicalMatches.Count(m => m.Status == "Completed"),
                AverageCompatibilityScore = matchItems.Any() ? (int)matchItems.Average(m => m.MatchScore) : 0,
                MatchSuccessRate = historicalMatches.Any() ? (historicalMatches.Count(m => m.Status == "Completed" || m.Status == "Accepted") * 100 / historicalMatches.Count) : 0,

                Matches = matchItems, // Pass ALL assigned matches, JS will filter by status
                AvailableCities = matchItems.Select(m => m.City).Where(c => c != null).Distinct().OrderBy(c => c).ToList(),
                AvailableHospitals = matchItems.Select(m => m.HospitalName).Where(h => h != null).Distinct().OrderBy(h => h).ToList()
            };

            ViewData["Title"] = "Smart Matches";
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateAvailability()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var donorProfile = await _context.DonorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (donorProfile == null) return RedirectToAction("Login", "Account");

            var areas = await _context.BloodRequests
                .Where(r => r.City != null)
                .Select(r => r.City)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var vm = new DonorAvailabilityViewModel
            {
                IsAvailable = donorProfile.IsAvailable,
                LastDonationDate = donorProfile.LastDonationDate,
                PreferredArea = donorProfile.PreferredArea,
                AvailableAreas = areas
            };

            ViewData["Title"] = "Update Availability";
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAvailability(DonorAvailabilityViewModel model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var donorProfile = await _context.DonorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (donorProfile == null) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                donorProfile.IsAvailable = model.IsAvailable;
                donorProfile.LastDonationDate = model.LastDonationDate;
                donorProfile.PreferredArea = model.PreferredArea;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your availability and preferences have been updated successfully!";
                return RedirectToAction(nameof(UpdateAvailability));
            }

            model.AvailableAreas = await _context.BloodRequests
                .Where(r => r.City != null)
                .Select(r => r.City)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> BloodRequests()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var donorProfile = await _context.DonorProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
            if (donorProfile == null) return RedirectToAction("Login", "Account");

            var requests = await _context.BloodRequests
                .Select(br => new DonorBloodRequestItem
                {
                    RequestId = br.RequestId,
                    BloodGroup = br.BloodGroup,
                    PatientName = br.Receiver != null ? br.Receiver.FullName : "Anonymous",
                    HospitalName = br.HospitalName,
                    City = br.City,
                    ContactNumber = br.Receiver != null ? br.Receiver.Phone : "N/A",
                    UnitsRequired = br.UnitsRequired ?? 1,
                    RequiredDate = br.RequiredDate ?? br.CreatedAt,
                    UrgencyLevel = br.UrgencyLevel ?? "Normal",
                    Status = br.RequestStatus ?? "Pending",
                    PostedDate = br.CreatedAt,
                    IsMatchingBloodGroup = br.BloodGroup == donorProfile.BloodGroup,
                    Distance = "N/A",
                    Eligibility = "Eligible"
                })
                .OrderByDescending(r => r.UrgencyLevel == "Critical" ? 1 : (r.UrgencyLevel == "High" ? 2 : 3))
                .ThenByDescending(r => r.PostedDate)
                .ToListAsync();

            var vm = new DonorBloodRequestViewModel
            {
                TotalRequests = requests.Count,
                ActiveRequests = requests.Count(r => r.Status == "Pending" || r.Status == "Approved"),
                EmergencyRequests = requests.Count(r => r.UrgencyLevel == "Critical"),
                MatchingRequests = requests.Count(r => r.IsMatchingBloodGroup),
                Requests = requests
            };

            ViewData["Title"] = "Blood Requests";
            return View(vm);
        }

        [HttpGet]
        public IActionResult BloodCompatibility()
        {
            var vm = new DonorBloodCompatibilityViewModel
            {
                BloodGroups = new List<BloodGroupCompatibilityItem>
                {
                    new BloodGroupCompatibilityItem
                    {
                        BloodGroup = "O-",
                        CanDonateTo = new List<string>{"O-","O+","A-","A+","B-","B+","AB-","AB+"},
                        CanReceiveFrom = new List<string>{"O-"},
                        ColorTheme = "danger",
                        SpecialRole = "Universal Donor"
                    },
                    new BloodGroupCompatibilityItem
                    {
                        BloodGroup = "O+",
                        CanDonateTo = new List<string>{"O+","A+","B+","AB+"},
                        CanReceiveFrom = new List<string>{"O-","O+"},
                        ColorTheme = "danger",
                        SpecialRole = ""
                    },
                    new BloodGroupCompatibilityItem
                    {
                        BloodGroup = "A-",
                        CanDonateTo = new List<string>{"A-","A+","AB-","AB+"},
                        CanReceiveFrom = new List<string>{"O-","A-"},
                        ColorTheme = "primary",
                        SpecialRole = ""
                    },
                    new BloodGroupCompatibilityItem
                    {
                        BloodGroup = "A+",
                        CanDonateTo = new List<string>{"A+","AB+"},
                        CanReceiveFrom = new List<string>{"O-","O+","A-","A+"},
                        ColorTheme = "primary",
                        SpecialRole = ""
                    },
                    new BloodGroupCompatibilityItem
                    {
                        BloodGroup = "B-",
                        CanDonateTo = new List<string>{"B-","B+","AB-","AB+"},
                        CanReceiveFrom = new List<string>{"O-","B-"},
                        ColorTheme = "success",
                        SpecialRole = ""
                    },
                    new BloodGroupCompatibilityItem
                    {
                        BloodGroup = "B+",
                        CanDonateTo = new List<string>{"B+","AB+"},
                        CanReceiveFrom = new List<string>{"O-","O+","B-","B+"},
                        ColorTheme = "success",
                        SpecialRole = ""
                    },
                    new BloodGroupCompatibilityItem
                    {
                        BloodGroup = "AB-",
                        CanDonateTo = new List<string>{"AB-","AB+"},
                        CanReceiveFrom = new List<string>{"O-","A-","B-","AB-"},
                        ColorTheme = "info",
                        SpecialRole = ""
                    },
                    new BloodGroupCompatibilityItem
                    {
                        BloodGroup = "AB+",
                        CanDonateTo = new List<string>{"AB+"},
                        CanReceiveFrom = new List<string>{"O-","O+","A-","A+","B-","B+","AB-","AB+"},
                        ColorTheme = "info",
                        SpecialRole = "Universal Receiver"
                    }
                }
            };

            ViewData["Title"] = "Blood Compatibility";
            return View(vm);
        }

        [HttpGet]
        public IActionResult BloodInformation()
        {
            ViewData["Title"] = "Blood Information";
            return View();
        }

        private Dictionary<string, List<string>> GetCompatibleRecipientGroups()
        {
            return new Dictionary<string, List<string>>
            {
                { "O-", new List<string> { "O-", "O+", "A-", "A+", "B-", "B+", "AB-", "AB+" } },
                { "O+", new List<string> { "O+", "A+", "B+", "AB+" } },
                { "A-", new List<string> { "A-", "A+", "AB-", "AB+" } },
                { "A+", new List<string> { "A+", "AB+" } },
                { "B-", new List<string> { "B-", "B+", "AB-", "AB+" } },
                { "B+", new List<string> { "B+", "AB+" } },
                { "AB-", new List<string> { "AB-", "AB+" } },
                { "AB+", new List<string> { "AB+" } }
            };
        }

        // =====================================================================
        // AUTOMATIC NOTIFICATION PROCESSING (Enhancement)
        // =====================================================================
        private async Task ProcessAutomaticNotifications(int userId, DonorProfile donorProfile)
        {
            await ProcessNewBloodRequestNotifications(userId, donorProfile);
            await ProcessEligibilityNotifications(userId, donorProfile);
            await ProcessDonationCompletedNotifications(userId);
            await ProcessAdminAnnouncements(userId);
        }

        private async Task ProcessNewBloodRequestNotifications(int userId, DonorProfile donorProfile)
        {
            // Only notify if donor is available and not in cooldown
            if (!donorProfile.IsAvailable) return;
            if (donorProfile.LastDonationDate.HasValue && (DateTime.Now - donorProfile.LastDonationDate.Value).Days < 90) return;

            var compatDict = GetCompatibleRecipientGroups();
            var compatibleGroups = compatDict.ContainsKey(donorProfile.BloodGroup ?? "") ? compatDict[donorProfile.BloodGroup] : new List<string>();

            // Check requests created in the last 24 hours to avoid spamming old requests
            var recentRequests = await _context.BloodRequests
                .Where(br => compatibleGroups.Contains(br.BloodGroup)
                             && br.RequestStatus == "Pending"
                             && br.CreatedAt >= DateTime.Now.AddHours(-24))
                .ToListAsync();

            bool hasNew = false;
            foreach (var req in recentRequests)
            {
                bool exists = await _context.DonorNotifications.AnyAsync(n =>
                    n.DonorId == userId && n.Category == "NewBloodRequest" && n.ReferenceId == req.RequestId);

                if (!exists)
                {
                    _context.DonorNotifications.Add(new DonorNotification
                    {
                        DonorId = userId,
                        Title = "New Blood Request Available",
                        Message = $"A new blood request matching your profile has been posted in {req.City}.",
                        Category = "NewBloodRequest",
                        ReferenceId = req.RequestId,
                        ActionUrl = "/Donor/BloodRequests",
                        IsRead = false,
                        CreatedDate = DateTime.Now
                    });
                    hasNew = true;
                }
            }
            if (hasNew) await _context.SaveChangesAsync();
        }

        private async Task ProcessEligibilityNotifications(int userId, DonorProfile donorProfile)
        {
            if (donorProfile.LastDonationDate.HasValue)
            {
                var daysSince = (DateTime.Now - donorProfile.LastDonationDate.Value).Days;
                if (daysSince >= 90)
                {
                    // Check if we already sent an eligibility notification for this specific cooldown cycle
                    bool exists = await _context.DonorNotifications.AnyAsync(n =>
                        n.DonorId == userId && n.Category == "Eligibility" && n.CreatedDate > donorProfile.LastDonationDate.Value);

                    if (!exists)
                    {
                        _context.DonorNotifications.Add(new DonorNotification
                        {
                            DonorId = userId,
                            Title = "You're Eligible to Donate Again",
                            Message = "Your recovery period has ended. You can now help save another life.",
                            Category = "Eligibility",
                            ReferenceId = donorProfile.DonorId,
                            ActionUrl = "/Donor/SmartMatches",
                            IsRead = false,
                            CreatedDate = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        private async Task ProcessDonationCompletedNotifications(int userId)
        {
            var donations = await _context.Donations.Where(d => d.DonorId == userId).ToListAsync();
            bool hasNew = false;

            foreach (var don in donations)
            {
                bool exists = await _context.DonorNotifications.AnyAsync(n =>
                    n.DonorId == userId && n.Category == "DonationUpdate" && n.ReferenceId == don.DonationId);

                if (!exists)
                {
                    _context.DonorNotifications.Add(new DonorNotification
                    {
                        DonorId = userId,
                        Title = "Thank You for Saving a Life ❤️",
                        Message = $"Your blood donation on {don.DonationDate:dd MMM yyyy} at {don.HospitalName} has been successfully recorded. Thank you for making a difference.",
                        Category = "DonationUpdate",
                        ReferenceId = don.DonationId,
                        ActionUrl = "/Donor/MyDonations",
                        IsRead = false,
                        CreatedDate = don.DonationDate
                    });
                    hasNew = true;
                }
            }
            if (hasNew) await _context.SaveChangesAsync();
        }

        private async Task ProcessAdminAnnouncements(int userId)
        {
            var announcements = await _context.AdminAnnouncements.ToListAsync();
            bool hasNew = false;

            foreach (var ann in announcements)
            {
                bool exists = await _context.DonorNotifications.AnyAsync(n =>
                    n.DonorId == userId && n.Category == "AdminMessage" && n.ReferenceId == ann.AnnouncementId);

                if (!exists)
                {
                    _context.DonorNotifications.Add(new DonorNotification
                    {
                        DonorId = userId,
                        Title = "Message from Admin",
                        Message = ann.Message,
                        Category = "AdminMessage",
                        ReferenceId = ann.AnnouncementId,
                        ActionUrl = ann.ActionUrl,
                        IsRead = false,
                        CreatedDate = ann.CreatedDate
                    });
                    hasNew = true;
                }
            }
            if (hasNew) await _context.SaveChangesAsync();
        }

        // GET: Donor/GetLatestNotifications
        [HttpGet]
        public async Task<IActionResult> GetLatestNotifications()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                System.Diagnostics.Debug.WriteLine($"=== Donor GetLatestNotifications Called ===");
                System.Diagnostics.Debug.WriteLine($"User ID Claim: {userIdClaim}");

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: User not authenticated");
                    return Json(new { success = false, message = "User not authenticated", notifications = new List<object>(), unreadCount = 0 });
                }

                if (!int.TryParse(userIdClaim, out int userId))
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: Invalid user ID - {userIdClaim}");
                    return Json(new { success = false, message = "Invalid user ID", notifications = new List<object>(), unreadCount = 0 });
                }

                System.Diagnostics.Debug.WriteLine($"User ID parsed: {userId}");

                var totalCount = await _context.DonorNotifications.CountAsync(n => n.DonorId == userId);
                System.Diagnostics.Debug.WriteLine($"Total notifications in DB: {totalCount}");

                var notifications = await _context.DonorNotifications
                    .Where(n => n.DonorId == userId)
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(5)
                    .Select(n => new
                    {
                        n.NotificationId,
                        n.Title,
                        n.Message,
                        n.Category,
                        IsRead = n.IsRead,
                        n.CreatedDate,
                        ReferenceId = n.ReferenceId,
                        ActionUrl = n.ActionUrl ?? (n.Category == "EmergencyAlert" ? "/Donor/EmergencyAlerts" :
                                                   n.Category == "SmartMatch" ? "/Donor/SmartMatches" :
                                                   n.Category == "DonationUpdate" ? "/Donor/MyDonations" : "/Donor/Notifications"),
                        TimeAgo = GetTimeAgo(n.CreatedDate)
                    })
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"Notifications returned: {notifications.Count}");

                var unreadCount = await _context.DonorNotifications
                    .CountAsync(n => n.DonorId == userId && !n.IsRead);

                System.Diagnostics.Debug.WriteLine($"Unread count: {unreadCount}");

                return Json(new
                {
                    success = true,
                    notifications,
                    unreadCount
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== EXCEPTION ===");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    notifications = new List<object>(),
                    unreadCount = 0
                });
            }
        }

        // Helper method for time ago format
        private static string GetTimeAgo(DateTime date)
        {
            var span = DateTime.Now - date;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return date.ToString("dd MMM yyyy");
        }

        private (double distanceKm, string travelTime) CalculateDistanceAndTime(double? lat1, double? lon1, double? lat2, double? lon2)
        {
            if (!lat1.HasValue || !lon1.HasValue || !lat2.HasValue || !lon2.HasValue) return (999, "N/A");
            double R = 6371;
            double dLat = (lat2.Value - lat1.Value) * Math.PI / 180;
            double dLon = (lon2.Value - lon1.Value) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1.Value * Math.PI / 180) * Math.Cos(lat2.Value * Math.PI / 180) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = R * c;
            int timeMinutes = (int)((distance / 30.0) * 60);
            string travelTime = timeMinutes < 60 ? $"{timeMinutes} mins" : $"{timeMinutes / 60}h {timeMinutes % 60}m";
            return (Math.Round(distance, 1), travelTime);
        }
    }
}

