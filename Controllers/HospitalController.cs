using Khoon_e_Hayat.Data;
using Khoon_e_Hayat.Models.Entities;
using Khoon_e_Hayat.Services;
using Khoon_e_Hayat.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace Khoon_e_Hayat.Controllers
{
    [Authorize(Roles = "Hospital")]
    public class HospitalController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public HospitalController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ==========================================
        // DASHBOARD (100% UPDATED & FIXED)
        // ==========================================
        public async Task<IActionResult> Dashboard()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return RedirectToAction("Login", "Account");
            var hospitalId = hospitalProfile.HospitalId;

            // ✅ FIXED: Fetch ALL inventory items (not just "Available")
            var allInventory = await _context.BloodInventory
                .Where(i => i.HospitalId == hospitalId && i.Status != "Expired" && i.Status != "Used")
                .ToListAsync();

            // Calculate totals from ALL inventory
            var totalBloodUnits = allInventory.Sum(i => i.Quantity);
            var availableBloodGroups = allInventory.Where(i => i.Quantity > 0).Select(i => i.BloodGroup).Distinct().Count();

            var pendingRequests = await _context.BloodRequests.CountAsync(r => r.HospitalId == hospitalId && r.RequestStatus == "Pending");
            var emergencyRequests = await _context.BloodRequests.CountAsync(r => r.HospitalId == hospitalId && r.UrgencyLevel == "Critical" && r.RequestStatus == "Pending");
            var assignedDonors = await _context.DonorMatches.CountAsync(m => m.HospitalId == hospitalId && m.Status == "Accepted");

            // ✅ FIXED: Use real database status for each blood group
            var bloodGroups = new[] { "A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-" };
            var inventoryData = new List<BloodInventoryItem>();

            foreach (var group in bloodGroups)
            {
                var groupItems = allInventory.Where(i => i.BloodGroup == group).ToList();
                var totalUnits = groupItems.Sum(i => i.Quantity);

                var status = "Available";
                if (groupItems.Any(i => i.Status == "Critical" || i.Status == "Out of Stock"))
                    status = "Critical";
                else if (groupItems.Any(i => i.Status == "Low Stock" || i.Status == "LOW"))
                    status = "Low Stock";
                else if (totalUnits == 0)
                    status = "Critical";
                else if (totalUnits <= 5)
                    status = "Low Stock";

                inventoryData.Add(new BloodInventoryItem
                {
                    BloodGroup = group,
                    AvailableUnits = totalUnits,
                    Status = status
                });
            }

            var recentRequestsQuery = await _context.BloodRequests.Include(r => r.Receiver)
                .Where(r => r.HospitalId == hospitalId)
                .OrderByDescending(r => r.CreatedAt).Take(5).ToListAsync();

            var recentRequests = recentRequestsQuery.Select(r => new BloodRequestSummary
            {
                RequestId = r.RequestId,
                ReceiverName = r.PatientName ?? r.Receiver?.FullName ?? "Unknown",
                BloodGroup = r.BloodGroup ?? "Unknown",
                RequiredUnits = r.UnitsRequired ?? 1,
                Priority = r.UrgencyLevel ?? "Normal",
                RequestDate = r.CreatedAt,
                Status = r.RequestStatus
            }).ToList();

            var emergencyRequestsQuery = await _context.BloodRequests.Include(r => r.Receiver)
                .Where(r => r.HospitalId == hospitalId && r.UrgencyLevel == "Critical" && r.RequestStatus == "Pending")
                .OrderByDescending(r => r.CreatedAt).Take(3).ToListAsync();

            var emergencyRequestList = emergencyRequestsQuery.Select(r => new BloodRequestSummary
            {
                RequestId = r.RequestId,
                ReceiverName = r.PatientName ?? r.Receiver?.FullName ?? "Unknown",
                BloodGroup = r.BloodGroup ?? "Unknown",
                RequiredUnits = r.UnitsRequired ?? 1,
                Priority = r.UrgencyLevel ?? "Critical",
                RequestDate = r.CreatedAt,
                Status = r.RequestStatus
            }).ToList();

            var activities = new List<ActivityLog>();
            var recentInventoryUpdates = await _context.BloodInventory
                .Where(i => i.HospitalId == hospitalId)
                .OrderByDescending(i => i.UpdatedAt).Take(2).ToListAsync();

            foreach (var item in recentInventoryUpdates)
                activities.Add(new ActivityLog { Activity = $"Blood stock updated for {item.BloodGroup} ({item.Quantity} units)", Timestamp = item.UpdatedAt, Icon = "bi-droplet" });

            var recentEmergencyRequests = await _context.BloodRequests.Include(r => r.Receiver)
                .Where(r => r.HospitalId == hospitalId && r.UrgencyLevel == "Critical")
                .OrderByDescending(r => r.CreatedAt).Take(2).ToListAsync();

            foreach (var req in recentEmergencyRequests)
                activities.Add(new ActivityLog { Activity = $"New emergency request received from {req.PatientName ?? req.Receiver?.FullName ?? "Unknown"}", Timestamp = req.CreatedAt, Icon = "bi-exclamation-triangle" });

            var recentDonorAssignments = await _context.DonorMatches
                .Where(m => m.HospitalId == hospitalId && m.Status == "Accepted")
                .OrderByDescending(m => m.MatchDate).Include(m => m.Donor).Take(2).ToListAsync();

            foreach (var match in recentDonorAssignments)
                activities.Add(new ActivityLog { Activity = $"Donor {match.Donor?.FullName ?? "Unknown"} assigned successfully", Timestamp = match.MatchDate, Icon = "bi-person-check" });

            var recentBloodIssues = await _context.BloodInventory
                .Where(i => i.HospitalId == hospitalId && i.Status == "Used")
                .OrderByDescending(i => i.UpdatedAt).Take(2).ToListAsync();

            foreach (var issue in recentBloodIssues)
                activities.Add(new ActivityLog { Activity = $"Blood issued successfully ({issue.BloodGroup}, {issue.Quantity} units)", Timestamp = issue.UpdatedAt, Icon = "bi-check-circle" });

            var recentActivities = activities.OrderByDescending(a => a.Timestamp).Take(6).ToList();

            // ✅ FIXED: Blood group distribution from ALL inventory
            var bloodGroupDistribution = bloodGroups.ToDictionary(bg => bg, bg =>
                allInventory.Where(i => i.BloodGroup == bg).Sum(i => i.Quantity));


            // ==========================================
            // ✅ NEW: MONTHLY BLOOD USAGE (Stacked Bar Chart Data - REAL DATA)
            // ==========================================
            var bloodGroupsList = new[] { "A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-" };
            var monthlyBloodUsageByGroup = new Dictionary<string, Dictionary<string, int>>();
            var currentDate = DateTime.Now;

            // 1. Initialize dictionary for last 6 months
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = currentDate.AddMonths(-i);
                var monthKey = monthDate.ToString("MMM yyyy");
                monthlyBloodUsageByGroup[monthKey] = new Dictionary<string, int>();
                foreach (var bg in bloodGroupsList)
                {
                    monthlyBloodUsageByGroup[monthKey][bg] = 0;
                }
            }

            // 2. Fetch REAL usage data from BloodIssueHistory (Last 6 Months)
            var issueHistory = await _context.BloodIssueHistory
                .Where(bi => bi.HospitalId == hospitalId && bi.IssueDate >= currentDate.AddMonths(-6))
                .ToListAsync();

            foreach (var issue in issueHistory)
            {
                var monthKey = issue.IssueDate.ToString("MMM yyyy");
                if (monthlyBloodUsageByGroup.ContainsKey(monthKey) && monthlyBloodUsageByGroup[monthKey].ContainsKey(issue.BloodGroup))
                {
                    monthlyBloodUsageByGroup[monthKey][issue.BloodGroup] += issue.UnitsIssued;
                }
            }

            // 3. Also populate the simple MonthlyBloodUsage (Total per month) for backward compatibility
            var monthlyBloodUsage = new Dictionary<string, int>();
            foreach (var kvp in monthlyBloodUsageByGroup)
            {
                monthlyBloodUsage[kvp.Key] = kvp.Value.Values.Sum();
            }


            // ==========================================
            // VIEWMODEL INITIALIZATION
            // ==========================================
            var vm = new HospitalDashboardViewModel
            {
                HospitalName = hospitalProfile.HospitalName ?? "Hospital",
                HospitalImage = hospitalProfile.LogoUrl ?? "/assets/img/avatars/DefaultHospital.png",
                IsVerified = hospitalProfile.IsVerified,
                TotalBloodUnits = totalBloodUnits,
                AvailableBloodGroups = availableBloodGroups,
                PendingRequests = pendingRequests,
                EmergencyRequests = emergencyRequests,
                AssignedDonors = assignedDonors,
                BloodInventory = inventoryData,
                RecentRequests = recentRequests,
                EmergencyRequestList = emergencyRequestList,
                RecentActivities = recentActivities,
                BloodGroupDistribution = bloodGroupDistribution,
                MonthlyBloodUsage = monthlyBloodUsage,
                MonthlyBloodUsageByGroup = monthlyBloodUsageByGroup // ✅ YE NAYI PROPERTY HAI (Stacked Chart ke liye)
            };

            ViewBag.UnreadNotificationCount = await _context.HospitalNotifications
                .CountAsync(n => n.HospitalId == hospitalId && !n.IsRead);

            ViewData["Title"] = "Hospital Dashboard";
            return View(vm);
        }

        // ==========================================
        // BLOOD COLLECTION HISTORY
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> BloodCollectionHistory(CollectionFilters filters, int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return RedirectToAction("Login", "Account");
            var hospitalId = hospitalProfile.HospitalId;

            var query = _context.BloodInventory.Where(i => i.HospitalId == hospitalId);

            if (!string.IsNullOrEmpty(filters.SearchQuery)) query = query.Where(i => i.BloodGroup.Contains(filters.SearchQuery) || i.Status.Contains(filters.SearchQuery));
            if (filters.BloodGroup != "all" && !string.IsNullOrEmpty(filters.BloodGroup)) query = query.Where(i => i.BloodGroup == filters.BloodGroup);
            if (filters.Status != "all" && !string.IsNullOrEmpty(filters.Status)) query = query.Where(i => i.Status == filters.Status);
            if (filters.DateFrom.HasValue) query = query.Where(i => i.CollectionDate >= filters.DateFrom.Value);
            if (filters.DateTo.HasValue) query = query.Where(i => i.CollectionDate <= filters.DateTo.Value);

            query = filters.SortBy switch
            {
                "oldest" => query.OrderBy(i => i.CollectionDate),
                "bloodgroup" => query.OrderBy(i => i.BloodGroup),
                "units" => query.OrderByDescending(i => i.Quantity),
                _ => query.OrderByDescending(i => i.CollectionDate)
            };

            var totalCount = await query.CountAsync();
            var pageSize = 10;
            var collections = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var allCollections = await _context.BloodInventory.Where(i => i.HospitalId == hospitalId).ToListAsync();
            var totalCollections = allCollections.Count;
            var totalUnitsCollected = allCollections.Sum(i => i.Quantity);
            var todayCollections = allCollections.Count(i => i.CollectionDate.Date == DateTime.Today);
            var monthCollections = allCollections.Count(i => i.CollectionDate.Month == DateTime.Today.Month && i.CollectionDate.Year == DateTime.Today.Year);
            var thisWeekCollections = allCollections.Count(i => i.CollectionDate >= DateTime.Today.AddDays(-7));
            var mostCollectedBloodGroup = allCollections.GroupBy(i => i.BloodGroup).OrderByDescending(g => g.Sum(i => i.Quantity)).FirstOrDefault()?.Key ?? "N/A";

            var viewModel = new BloodCollectionHistoryViewModel
            {
                TotalCollections = totalCollections,
                TotalUnitsCollected = totalUnitsCollected,
                TodayCollections = todayCollections,
                MonthCollections = monthCollections,
                MostCollectedBloodGroup = mostCollectedBloodGroup,
                ThisWeekCollections = thisWeekCollections,
                Collections = collections,
                Filters = filters,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                TotalCount = totalCount,
                PageSize = pageSize
            };

            ViewData["Title"] = "Blood Collection History";
            return View(viewModel);
        }

        // ==========================================
        // BLOOD ISSUE HISTORY 
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> BloodIssueHistory(IssueHistoryFilters filters, int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return RedirectToAction("Login", "Account");
            var hospitalId = hospitalProfile.HospitalId;

            var query = _context.BloodIssueHistory.Where(i => i.HospitalId == hospitalId);

            if (!string.IsNullOrEmpty(filters.SearchQuery))
                query = query.Where(i => i.BloodGroup.Contains(filters.SearchQuery) || i.HospitalName.Contains(filters.SearchQuery) || i.IssuedBy.Contains(filters.SearchQuery));

            if (filters.BloodGroup != "all" && !string.IsNullOrEmpty(filters.BloodGroup))
                query = query.Where(i => i.BloodGroup == filters.BloodGroup);

            if (filters.DateFrom.HasValue) query = query.Where(i => i.IssueDate >= filters.DateFrom.Value);
            if (filters.DateTo.HasValue) query = query.Where(i => i.IssueDate <= filters.DateTo.Value);

            query = filters.SortBy switch
            {
                "oldest" => query.OrderBy(i => i.IssueDate),
                "bloodgroup" => query.OrderBy(i => i.BloodGroup),
                "units" => query.OrderByDescending(i => i.UnitsIssued),
                _ => query.OrderByDescending(i => i.IssueDate)
            };

            var totalCount = await query.CountAsync();
            var pageSize = 10;
            var issues = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var issueItems = issues.Select(i => new BloodIssueHistoryItem
            {
                IssueId = i.IssueId,
                BloodGroup = i.BloodGroup,
                UnitsIssued = i.UnitsIssued,
                IssueDate = i.IssueDate,
                IssuedBy = i.IssuedBy,
                HospitalName = i.HospitalName,
                Status = i.Status,
                Notes = i.Notes
            }).ToList();

            var allIssues = await _context.BloodIssueHistory.Where(i => i.HospitalId == hospitalId).ToListAsync();

            var viewModel = new BloodIssueHistoryViewModel
            {
                TotalIssues = allIssues.Count,
                TotalUnitsIssued = allIssues.Sum(i => i.UnitsIssued),
                TodayIssues = allIssues.Count(i => i.IssueDate.Date == DateTime.Today),
                MonthIssues = allIssues.Count(i => i.IssueDate.Month == DateTime.Today.Month && i.IssueDate.Year == DateTime.Today.Year),
                SuccessfulDeliveries = allIssues.Count(i => i.Status == "Completed"),
                MostIssuedBloodGroup = allIssues.GroupBy(i => i.BloodGroup).OrderByDescending(g => g.Sum(i => i.UnitsIssued)).FirstOrDefault()?.Key ?? "N/A",
                ThisWeekIssues = allIssues.Count(i => i.IssueDate >= DateTime.Today.AddDays(-7)),
                Issues = issueItems,
                Filters = filters,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                TotalCount = totalCount,
                PageSize = pageSize
            };

            ViewData["Title"] = "Blood Issue History";
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRequestCompleted(int requestId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });

            var request = await _context.BloodRequests.Include(r => r.Receiver).FirstOrDefaultAsync(r => r.RequestId == requestId && r.HospitalId == hospitalProfile.HospitalId);
            if (request == null) return Json(new { success = false, message = "Request not found." });

            if (request.RequestStatus != "Blood Issued")
                return Json(new { success = false, message = "Request must be in 'Blood Issued' status to complete." });

            // Update request status
            request.RequestStatus = "Completed";

            // Create notification for receiver
            _context.ReceiverNotifications.Add(new ReceiverNotification
            {
                ReceiverId = request.ReceiverId,
                RequestId = request.RequestId,
                Title = "Request Completed",
                Message = $"Your blood request REQ-{request.RequestId:D4} has been successfully completed. Thank you for using Khoon-e-Hayat.",
                Category = "Completed",
                Priority = "High",
                IsRead = false,
                CreatedDate = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Request marked as completed successfully!" });
        }


        // ==========================================
        // LOW STOCK MONITOR (Initial Page Load)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> LowStockMonitor(LowStockFilters filters)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return RedirectToAction("Login", "Account");

            ViewBag.HospitalName = hospitalProfile.HospitalName ?? "Khoon-e-Hayat Hospital";
            var data = await GetLowStockDataInternalAsync(hospitalProfile.HospitalId);

            var viewModel = new LowStockMonitorViewModel
            {
                TotalBloodGroups = data.Stats.TotalBloodGroups,
                HealthyStockGroups = data.Stats.Healthy,
                LowStockGroups = data.Stats.LowStock,
                CriticalStockGroups = data.Stats.Critical,
                OutOfStockGroups = data.Stats.OutOfStock,
                TotalAvailableUnits = data.Stats.TotalUnits,
                BloodGroupStock = new List<BloodGroupStockItem>(),
                Filters = filters ?? new LowStockFilters()
            };

            foreach (var item in data.BloodGroupStock)
            {
                if (DateTime.TryParse(item.LastUpdated, out DateTime lastUpdated))
                {
                    viewModel.BloodGroupStock.Add(new BloodGroupStockItem
                    {
                        BloodGroup = item.BloodGroup,
                        TotalUnits = item.TotalUnits,
                        ReorderLevel = item.ReorderLevel,
                        Status = item.Status,
                        StatusColorClass = item.Status == "Out of Stock" || item.Status == "Critical" ? "status-critical" : item.Status == "Low Stock" ? "status-low-stock" : "status-healthy",
                        RecommendedAction = item.RecommendedAction,
                        LastUpdated = lastUpdated,
                        PercentageRemaining = item.PercentageRemaining
                    });
                }
            }

            ViewData["Title"] = "Low Stock Monitor";
            return View(viewModel);
        }

        // ==========================================
        // LIGHTWEIGHT AJAX AUTO-REFRESH ENDPOINT
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetLowStockDataJson()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Unauthorized();

            var data = await GetLowStockDataInternalAsync(hospitalProfile.HospitalId);
            data.HospitalName = hospitalProfile.HospitalName ?? "Khoon-e-Hayat Hospital";

            return Json(data);
        }

        // ==========================================
        // REAL ANALYTICS ENDPOINT (For Modal)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetBloodGroupAnalytics(string bloodGroup)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Unauthorized();

            var hospitalId = hospitalProfile.HospitalId;
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);

            // 1. Real Collection Analytics
            var collectionStats = await _context.BloodInventory
                .Where(i => i.HospitalId == hospitalId && i.BloodGroup == bloodGroup && i.Status != "Expired" && i.Status != "Used")
                .Select(i => new { i.CollectionDate, i.Quantity })
                .ToListAsync();

            var lastCollection = collectionStats.OrderByDescending(i => i.CollectionDate).FirstOrDefault()?.CollectionDate;
            var collectionsLast30Days = collectionStats.Count(i => i.CollectionDate >= thirtyDaysAgo);
            var collectionFrequency = collectionsLast30Days > 0 ? $"{collectionsLast30Days} time(s) in last 30 days" : "No recent collections";

            // 2. Real Issue History Analytics (Consumption Rate)
            var issueStats = await _context.BloodIssueHistory
                .Where(i => i.HospitalId == hospitalId && i.BloodGroup == bloodGroup)
                .Select(i => new { i.IssueDate, i.UnitsIssued })
                .ToListAsync();

            var lastIssue = issueStats.OrderByDescending(i => i.IssueDate).FirstOrDefault()?.IssueDate;
            var totalIssued30Days = issueStats.Where(i => i.IssueDate >= thirtyDaysAgo).Sum(i => (int?)i.UnitsIssued) ?? 0;
            var avgDailyUsage = Math.Round(totalIssued30Days / 30.0, 1);

            // 3. Current Available Stock & Remaining Days
            var currentStock = await _context.BloodInventory
                .Where(i => i.HospitalId == hospitalId && i.BloodGroup == bloodGroup && i.Status != "Expired" && i.Status != "Used")
                .SumAsync(i => (int?)i.Quantity) ?? 0;

            var remainingDays = avgDailyUsage > 0 ? Math.Floor(currentStock / avgDailyUsage) : 999;

            return Json(new
            {
                success = true,
                avgDailyUsage = avgDailyUsage,
                remainingDays = remainingDays > 365 ? "N/A (High Stock)" : $"{remainingDays} days",
                collectionFrequency = collectionFrequency,
                lastCollectionDate = lastCollection.HasValue ? lastCollection.Value.ToString("dd-MMM-yyyy") : "N/A",
                lastIssueDate = lastIssue.HasValue ? lastIssue.Value.ToString("dd-MMM-yyyy") : "N/A",
                currentStock = currentStock
            });
        }

        // ==========================================
        // OPTIMIZED INTERNAL DATA FETCHER (GroupBy)
        // ==========================================
        private async Task<dynamic> GetLowStockDataInternalAsync(int hospitalId)
        {
            // Single optimized GroupBy query to prevent N+1 database calls
            var inventoryStats = await _context.BloodInventory
                .Where(i => i.HospitalId == hospitalId && i.Status != "Expired" && i.Status != "Used")
                .GroupBy(i => i.BloodGroup)
                .Select(g => new {
                    BloodGroup = g.Key,
                    TotalUnits = g.Sum(i => i.Quantity),
                    ReorderLevel = g.Max(i => i.ReorderLevel),
                    LastUpdated = g.Max(i => i.UpdatedAt)
                }).ToListAsync();

            var allBloodGroups = new[] { "A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-" };
            var bloodGroupStock = new List<object>();

            int healthyCount = 0, lowCount = 0, criticalCount = 0, outOfStockCount = 0, totalUnits = 0;

            foreach (var bg in allBloodGroups)
            {
                var stat = inventoryStats.FirstOrDefault(x => x.BloodGroup == bg);
                var units = stat?.TotalUnits ?? 0;
                var reorder = stat?.ReorderLevel ?? 10;
                var lastUpdated = stat?.LastUpdated ?? DateTime.Now;
                totalUnits += units;

                string status = "Healthy";
                int percentage = 100;
                string action = "No Action Required. Stock level is adequate.";

                if (units == 0)
                {
                    status = "Out of Stock";
                    outOfStockCount++;
                    percentage = 0;
                    action = "Immediate Emergency Action Required. Find Matching Donors & Create Emergency Blood Request.";
                }
                else if (units <= reorder * 0.3)
                {
                    status = "Critical";
                    criticalCount++;
                    percentage = (int)((units / (double)reorder) * 100);
                    action = "Start Smart Donor Search & Arrange Immediate Blood Collection.";
                }
                else if (units <= reorder)
                {
                    status = "Low Stock";
                    lowCount++;
                    percentage = (int)((units / (double)reorder) * 100);
                    action = "Schedule Blood Collection & Monitor Inventory Closely.";
                }
                else
                {
                    healthyCount++;
                }

                bloodGroupStock.Add(new
                {
                    BloodGroup = bg,
                    TotalUnits = units,
                    ReorderLevel = reorder,
                    Status = status,
                    PercentageRemaining = percentage,
                    LastUpdated = lastUpdated.ToString("dd-MMM-yyyy, hh:mm tt"),
                    LastUpdatedRelative = GetRelativeTimeString(lastUpdated),
                    RecommendedAction = action
                });
            }

            int healthPercentage = (healthyCount * 100) / 8;
            string healthLevel = healthPercentage >= 80 ? "Excellent" : healthPercentage >= 60 ? "Good" : healthPercentage >= 40 ? "Average" : "Critical";

            return new
            {
                HospitalName = "",
                Stats = new
                {
                    TotalBloodGroups = 8,
                    Healthy = healthyCount,
                    LowStock = lowCount,
                    Critical = criticalCount,
                    OutOfStock = outOfStockCount,
                    TotalUnits = totalUnits,
                    HealthPercentage = healthPercentage,
                    HealthLevel = healthLevel
                },
                BloodGroupStock = bloodGroupStock
            };
        }

        private string GetRelativeTimeString(DateTime date)
        {
            var span = DateTime.Now - date;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} hr ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} day(s) ago";
            return date.ToString("dd MMM yyyy");
        }

        // ==========================================
        // EXPIRING BLOOD
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ExpiringBlood(ExpiringBloodFilters filters, int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return RedirectToAction("Login", "Account");
            var hospitalId = hospitalProfile.HospitalId;
            var today = DateTime.Today;

            var allInventory = await _context.BloodInventory.Where(i => i.HospitalId == hospitalId).ToListAsync();

            var allItems = allInventory.Select(i =>
            {
                var daysRemaining = (i.ExpiryDate.Date - today).Days;
                var totalShelfLife = (i.ExpiryDate.Date - i.CollectionDate.Date).Days;
                var daysElapsed = totalShelfLife - daysRemaining;
                var expiryPercentage = totalShelfLife > 0 ? Math.Max(0, Math.Min(100, (int)((daysElapsed / (double)totalShelfLife) * 100))) : 0;

                string expiryStatus = daysRemaining < 0 ? "Expired" : daysRemaining <= 7 ? "Critical" : daysRemaining <= 15 ? "Expiring Soon" : "Safe";
                string statusColorClass = daysRemaining < 0 ? "status-expired" : daysRemaining <= 7 ? "status-critical" : daysRemaining <= 15 ? "status-expiring-soon" : "status-safe";

                return new ExpiringBloodItem
                {
                    InventoryId = i.InventoryId,
                    BloodGroup = i.BloodGroup,
                    AvailableUnits = i.Quantity,
                    CollectionDate = i.CollectionDate,
                    ExpiryDate = i.ExpiryDate,
                    DaysRemaining = Math.Max(0, daysRemaining),
                    ExpiryPercentage = expiryPercentage,
                    StorageLocation = "Blood Bank Storage",
                    ExpiryStatus = expiryStatus,
                    StatusColorClass = statusColorClass,
                    LastUpdated = i.UpdatedAt
                };
            }).ToList();

            if (!string.IsNullOrEmpty(filters.SearchQuery))
                allItems = allItems.Where(i => i.BloodGroup.Contains(filters.SearchQuery, StringComparison.OrdinalIgnoreCase) || i.StorageLocation.Contains(filters.SearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrEmpty(filters.ExpiryStatus) && filters.ExpiryStatus != "all")
            {
                allItems = filters.ExpiryStatus switch
                {
                    "7days" => allItems.Where(i => i.DaysRemaining >= 0 && i.DaysRemaining <= 7).ToList(),
                    "15days" => allItems.Where(i => i.DaysRemaining >= 0 && i.DaysRemaining <= 15).ToList(),
                    "30days" => allItems.Where(i => i.DaysRemaining >= 0 && i.DaysRemaining <= 30).ToList(),
                    "expired" => allItems.Where(i => i.ExpiryStatus == "Expired").ToList(),
                    "safe" => allItems.Where(i => i.ExpiryStatus == "Safe").ToList(),
                    _ => allItems
                };
            }

            allItems = filters.SortBy switch
            {
                "latest" => allItems.OrderByDescending(i => i.ExpiryDate).ToList(),
                "bloodgroup" => allItems.OrderBy(i => i.BloodGroup).ToList(),
                "units" => allItems.OrderByDescending(i => i.AvailableUnits).ToList(),
                _ => allItems.OrderBy(i => i.ExpiryDate).ToList()
            };

            var totalCount = allItems.Count;
            var pageSize = 10;
            var currentPage = page < 1 ? 1 : page;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var paginatedItems = allItems.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

            var viewModel = new ExpiringBloodViewModel
            {
                TotalBloodUnits = allInventory.Sum(i => i.Quantity),
                SafeUnits = allInventory.Where(i => (i.ExpiryDate.Date - today).Days > 15).Sum(i => i.Quantity),
                ExpiringSoonUnits = allInventory.Where(i => { var days = (i.ExpiryDate.Date - today).Days; return days >= 0 && days <= 15; }).Sum(i => i.Quantity),
                ExpiredUnits = allInventory.Where(i => i.ExpiryDate.Date < today).Sum(i => i.Quantity),
                ExpiringThisWeekUnits = allInventory.Where(i => { var days = (i.ExpiryDate.Date - today).Days; return days >= 0 && days <= 7; }).Sum(i => i.Quantity),
                ExpiringTodayUnits = allInventory.Where(i => i.ExpiryDate.Date == today).Sum(i => i.Quantity),
                ExpiringBlood = paginatedItems,
                Filters = filters,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize
            };

            ViewData["Title"] = "Expiring Blood";
            return View(viewModel);
        }

        // ==========================================
        // INCOMING REQUESTS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> IncomingRequests(IncomingRequestFilters filters, int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return RedirectToAction("Login", "Account");
            var hospitalId = hospitalProfile.HospitalId;

            var query = _context.BloodRequests.Include(r => r.Receiver).Where(r => r.HospitalId == hospitalId);

            if (!string.IsNullOrEmpty(filters.SearchQuery)) query = query.Where(r => r.PatientName.Contains(filters.SearchQuery) || r.BloodGroup.Contains(filters.SearchQuery) || r.RequestId.ToString().Contains(filters.SearchQuery));
            if (filters.Status != "all" && !string.IsNullOrEmpty(filters.Status)) query = query.Where(r => r.RequestStatus == filters.Status);
            if (filters.Priority != "all" && !string.IsNullOrEmpty(filters.Priority)) query = query.Where(r => r.UrgencyLevel == filters.Priority);
            if (filters.BloodGroup != "all" && !string.IsNullOrEmpty(filters.BloodGroup)) query = query.Where(r => r.BloodGroup == filters.BloodGroup);

            query = filters.SortBy switch
            {
                "oldest" => query.OrderBy(r => r.CreatedAt),
                "priority" => query.OrderByDescending(r => r.UrgencyLevel == "Critical").ThenByDescending(r => r.UrgencyLevel == "High").ThenByDescending(r => r.UrgencyLevel == "Normal"),
                "bloodgroup" => query.OrderBy(r => r.BloodGroup),
                _ => query.OrderByDescending(r => r.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var pageSize = 10;
            var requests = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var bloodInventory = await _context.BloodInventory.Where(i => i.HospitalId == hospitalId && i.Status == "Available").ToListAsync();

            var requestItems = requests.Select(r =>
            {
                var availableStock = bloodInventory.Where(i => i.BloodGroup == r.BloodGroup).Sum(i => i.Quantity);
                return new IncomingRequestItem
                {
                    RequestId = r.RequestId,
                    ReceiverName = r.PatientName ?? r.Receiver?.FullName ?? "Unknown",
                    BloodGroup = r.BloodGroup ?? "Unknown",
                    RequiredUnits = r.UnitsRequired ?? 1,
                    RequestDate = r.CreatedAt,
                    Priority = r.UrgencyLevel ?? "Normal",
                    Status = r.RequestStatus,
                    HospitalName = r.HospitalName ?? hospitalProfile?.HospitalName ?? "Hospital",
                    City = r.City ?? hospitalProfile?.City ?? "",
                    PatientName = r.PatientName ?? "Unknown",
                    Reason = r.Reason ?? "",
                    ContactNumber = r.HospitalContact ?? "",
                    IsBloodAvailable = availableStock >= (r.UnitsRequired ?? 1),
                    AvailableStock = availableStock
                };
            }).ToList();

            var emergencyRequests = await _context.BloodRequests.Include(r => r.Receiver).Where(r => r.HospitalId == hospitalId && r.UrgencyLevel == "Critical" && r.RequestStatus == "Pending").OrderByDescending(r => r.CreatedAt).Take(5).ToListAsync();
            var emergencyRequestList = emergencyRequests.Select(r => new IncomingRequestItem { RequestId = r.RequestId, ReceiverName = r.PatientName ?? r.Receiver?.FullName ?? "Unknown", BloodGroup = r.BloodGroup ?? "Unknown", RequiredUnits = r.UnitsRequired ?? 1, RequestDate = r.CreatedAt, Priority = r.UrgencyLevel ?? "Critical", Status = r.RequestStatus }).ToList();

            var allRequests = await _context.BloodRequests.Where(r => r.HospitalId == hospitalId).ToListAsync();

            var viewModel = new IncomingRequestsViewModel
            {
                TotalRequests = allRequests.Count,
                PendingRequests = allRequests.Count(r => r.RequestStatus == "Pending"),
                EmergencyRequests = allRequests.Count(r => r.UrgencyLevel == "Critical" && r.RequestStatus == "Pending"),
                ApprovedRequests = allRequests.Count(r => r.RequestStatus == "Approved"),
                CompletedRequests = allRequests.Count(r => r.RequestStatus == "Completed"),
                RejectedRequests = allRequests.Count(r => r.RequestStatus == "Rejected"),
                Requests = requestItems,
                EmergencyRequestList = emergencyRequestList,
                Filters = filters,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                TotalCount = totalCount,
                PageSize = pageSize
            };

            ViewData["Title"] = "Incoming Requests";
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });

            var request = await _context.BloodRequests.Include(r => r.Receiver).FirstOrDefaultAsync(r => r.RequestId == id && r.HospitalId == hospitalProfile.HospitalId);
            if (request == null) return Json(new { success = false, message = "Request not found." });
            if (request.RequestStatus != "Pending") return Json(new { success = false, message = "Request is not in pending status." });

            // Step 3: Approve Request
            request.RequestStatus = "Approved";

            // Step 3: Receiver Notification
            _context.ReceiverNotifications.Add(new ReceiverNotification
            {
                ReceiverId = request.ReceiverId,
                RequestId = request.RequestId,
                Title = "Blood Request Approved",
                Message = $"Your blood request has been approved by {hospitalProfile.HospitalName}. Our team is now checking blood availability.",
                Category = "BloodRequest",
                Priority = "High",
                IsRead = false,
                CreatedDate = DateTime.Now
            });

            await _context.SaveChangesAsync();

            // Step 3: Send Approval Email
            if (!string.IsNullOrEmpty(request.Receiver?.Email))
            {
                await _emailService.SendEmailAsync(request.Receiver.Email, "Blood Request Approved - Khoon-e-Hayat",
                    $"<p>Dear {request.Receiver.FullName},</p><p>Your blood request REQ-{request.RequestId:D4} has been approved by {hospitalProfile.HospitalName}. Our team is now checking blood availability.</p>", "BloodRequest");
            }

            // Step 4: Automatic Inventory Verification (Return data for UI to show correct button)
            var availableUnits = await _context.BloodInventory
                .Where(i => i.HospitalId == hospitalProfile.HospitalId && i.BloodGroup == request.BloodGroup && i.Status != "Expired" && i.Status != "Used")
                .SumAsync(i => (int?)i.Quantity) ?? 0;

            bool isInventorySufficient = availableUnits >= (request.UnitsRequired ?? 1);

            return Json(new
            {
                success = true,
                message = "Request approved successfully!",
                isInventorySufficient = isInventorySufficient,
                availableUnits = availableUnits
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueBlood(int requestId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });

            var request = await _context.BloodRequests.Include(r => r.Receiver).FirstOrDefaultAsync(r => r.RequestId == requestId && r.HospitalId == hospitalProfile.HospitalId);
            if (request == null) return Json(new { success = false, message = "Request not found." });
            if (request.RequestStatus != "Approved") return Json(new { success = false, message = "Request must be approved first." });

            int unitsToIssue = request.UnitsRequired ?? 1;

            // Step 5: Fetch Inventory (FIFO - Oldest Expiry First)
            var inventoryItems = await _context.BloodInventory
                .Where(i => i.HospitalId == hospitalProfile.HospitalId && i.BloodGroup == request.BloodGroup && i.Status != "Expired" && i.Status != "Used" && i.Quantity > 0)
                .OrderBy(i => i.ExpiryDate)
                .ToListAsync();

            int totalAvailable = inventoryItems.Sum(i => i.Quantity);
            if (totalAvailable < unitsToIssue)
            {
                return Json(new { success = false, message = $"Insufficient inventory. Available: {totalAvailable}, Required: {unitsToIssue}. Please find compatible donors." });
            }

            // Step 5: Deduct Inventory
            int remainingToDeduct = unitsToIssue;
            foreach (var item in inventoryItems)
            {
                if (remainingToDeduct <= 0) break;

                int deductAmount = Math.Min(item.Quantity, remainingToDeduct);
                item.Quantity -= deductAmount;
                remainingToDeduct -= deductAmount;
                item.UpdatedAt = DateTime.Now;

                // Step 5 (Action 4): Auto-update Status based on new quantity
                if (item.Quantity == 0) item.Status = "Critical";
                else if (item.Quantity <= 5) item.Status = "Low Stock";
                else item.Status = "Available";
            }

            // Step 5 (Action 2): Create Blood Issue History with BloodRequestId
            _context.BloodIssueHistory.Add(new BloodIssueHistory
            {
                BloodRequestId = request.RequestId,  // ✅ YE ADD KIYA HAI
                HospitalId = hospitalProfile.HospitalId,
                HospitalName = hospitalProfile.HospitalName,
                BloodGroup = request.BloodGroup,
                UnitsIssued = unitsToIssue,
                IssueDate = DateTime.Now,
                IssuedBy = User.FindFirstValue(ClaimTypes.Name) ?? "Hospital Admin",
                Status = "Completed",
                Notes = $"Issued for Request REQ-{request.RequestId:D4}",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });

            // Step 5 (Action 7): Update Request Status
            request.RequestStatus = "Blood Issued";

            // Step 5 (Action 5): Receiver Notification
            _context.ReceiverNotifications.Add(new ReceiverNotification
            {
                ReceiverId = request.ReceiverId,
                RequestId = request.RequestId,
                Title = "Blood Ready for Collection",
                Message = $"Your requested blood has been issued and is ready for collection from {hospitalProfile.HospitalName}.",
                Category = "BloodReady",
                Priority = "High",
                IsRead = false,
                CreatedDate = DateTime.Now
            });

            await _context.SaveChangesAsync();

            // Step 5 (Action 6): Send Email
            if (!string.IsNullOrEmpty(request.Receiver?.Email))
            {
                await _emailService.SendBloodReadyForCollectionEmailAsync(
                    request.Receiver.Email,
                    request.Receiver.FullName ?? "Receiver",
                    hospitalProfile.HospitalName ?? "Hospital",
                    request.BloodGroup ?? "Unknown",
                    unitsToIssue,
                    DateTime.Now,
                    request.RequestId
                );
            }

            return Json(new { success = true, message = "Blood issued successfully! Inventory updated and receiver notified." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int id, string reason)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });
            var hospitalId = hospitalProfile.HospitalId;

            var request = await _context.BloodRequests.FirstOrDefaultAsync(r => r.RequestId == id && r.HospitalId == hospitalId);
            if (request == null) return Json(new { success = false, message = "Request not found." });
            if (request.RequestStatus != "Pending") return Json(new { success = false, message = "Request is not in pending status." });

            request.RequestStatus = "Rejected";
            request.AdditionalNotes = reason ?? "Request rejected by hospital";
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Request rejected successfully!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRequest(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });
            var hospitalId = hospitalProfile.HospitalId;

            var request = await _context.BloodRequests.FirstOrDefaultAsync(r => r.RequestId == id && r.HospitalId == hospitalId);
            if (request == null) return Json(new { success = false, message = "Request not found." });
            if (request.RequestStatus != "Approved") return Json(new { success = false, message = "Request must be approved before completion." });

            request.RequestStatus = "Completed";
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Request marked as completed!" });
        }

        [HttpGet]
        public async Task<IActionResult> GetRequestDetails(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });
            var hospitalId = hospitalProfile.HospitalId;

            var request = await _context.BloodRequests.Include(r => r.Receiver).FirstOrDefaultAsync(r => r.RequestId == id && r.HospitalId == hospitalId);
            if (request == null) return Json(new { success = false, message = "Request not found." });

            var availableStock = await _context.BloodInventory.Where(i => i.HospitalId == hospitalId && i.BloodGroup == request.BloodGroup && i.Status == "Available").SumAsync(i => (int?)i.Quantity) ?? 0;

            return Json(new
            {
                success = true,
                data = new
                {
                    request.RequestId,
                    ReceiverName = request.PatientName ?? request.Receiver?.FullName ?? "Unknown",
                    request.BloodGroup,
                    UnitsRequired = request.UnitsRequired ?? 1,
                    request.HospitalName,
                    request.City,
                    request.PatientName,
                    request.Reason,
                    request.HospitalContact,
                    request.CreatedAt,
                    Priority = request.UrgencyLevel ?? "Normal",
                    request.RequestStatus,
                    AvailableStock = availableStock,
                    IsBloodAvailable = availableStock >= (request.UnitsRequired ?? 1)
                }
            });
        }

        // ==========================================
        // EMERGENCY REQUESTS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> EmergencyRequests(EmergencyRequestFilters filters, int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return RedirectToAction("Login", "Account");
            var hospitalId = hospitalProfile.HospitalId;

            var query = _context.BloodRequests.Include(r => r.Receiver).Where(r => r.HospitalId == hospitalId && (r.UrgencyLevel == "Critical" || r.UrgencyLevel == "Emergency" || r.UrgencyLevel == "High"));

            if (!string.IsNullOrEmpty(filters.SearchQuery)) query = query.Where(r => r.PatientName.Contains(filters.SearchQuery) || r.BloodGroup.Contains(filters.SearchQuery) || r.RequestId.ToString().Contains(filters.SearchQuery));
            if (filters.Status != "all" && !string.IsNullOrEmpty(filters.Status)) query = query.Where(r => r.RequestStatus == filters.Status);
            if (filters.Priority != "all" && !string.IsNullOrEmpty(filters.Priority)) query = query.Where(r => r.UrgencyLevel == filters.Priority);
            if (filters.BloodGroup != "all" && !string.IsNullOrEmpty(filters.BloodGroup)) query = query.Where(r => r.BloodGroup == filters.BloodGroup);
            if (filters.City != "all" && !string.IsNullOrEmpty(filters.City)) query = query.Where(r => r.City == filters.City);

            query = filters.SortBy switch
            {
                "oldest" => query.OrderBy(r => r.CreatedAt),
                "bloodgroup" => query.OrderBy(r => r.BloodGroup),
                _ => query.OrderByDescending(r => r.UrgencyLevel == "Critical").ThenByDescending(r => r.UrgencyLevel == "Emergency").ThenByDescending(r => r.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var pageSize = 10;
            var requests = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var bloodInventory = await _context.BloodInventory.Where(i => i.HospitalId == hospitalId && i.Status == "Available").ToListAsync();
            var cities = await _context.BloodRequests.Where(r => r.HospitalId == hospitalId).Select(r => r.City).Distinct().Where(c => c != null).Cast<string>().ToListAsync();

            var requestItems = requests.Select(r =>
            {
                var availableStock = bloodInventory.Where(i => i.BloodGroup == r.BloodGroup).Sum(i => i.Quantity);
                var requiredDate = r.RequiredDate ?? r.CreatedAt.AddHours(2);
                var minutesRemaining = (int)(requiredDate - DateTime.Now).TotalMinutes;

                string timerText;
                if (minutesRemaining <= 0) timerText = "Expired";
                else if (minutesRemaining < 60) timerText = $"{minutesRemaining} Min Left";
                else if (minutesRemaining < 1440) timerText = $"{minutesRemaining / 60} Hr {(minutesRemaining % 60)} Min Left";
                else timerText = $"{minutesRemaining / 1440} Days Left";

                return new EmergencyRequestItem
                {
                    RequestId = r.RequestId,
                    ReceiverName = r.PatientName ?? r.Receiver?.FullName ?? "Unknown",
                    BloodGroup = r.BloodGroup ?? "Unknown",
                    RequiredUnits = r.UnitsRequired ?? 1,
                    RequiredBefore = requiredDate,
                    City = r.City ?? hospitalProfile?.City ?? "",
                    Priority = r.UrgencyLevel ?? "Critical",
                    Status = r.RequestStatus,
                    IsBloodAvailable = availableStock >= (r.UnitsRequired ?? 1),
                    AvailableStock = availableStock,
                    MinutesRemaining = minutesRemaining,
                    TimerText = timerText,
                    Reason = r.Reason ?? "",
                    HospitalName = r.HospitalName ?? hospitalProfile?.HospitalName ?? "Hospital",
                    ContactNumber = r.HospitalContact ?? "",
                    RequestDate = r.CreatedAt
                };
            }).ToList();

            var allEmergency = await _context.BloodRequests.Where(r => r.HospitalId == hospitalId && (r.UrgencyLevel == "Critical" || r.UrgencyLevel == "Emergency" || r.UrgencyLevel == "High")).ToListAsync();
            var total = allEmergency.Count;
            var pending = allEmergency.Count(r => r.RequestStatus == "Pending");
            var approved = allEmergency.Count(r => r.RequestStatus == "Approved" || r.RequestStatus == "Blood Reserved");
            var completed = allEmergency.Count(r => r.RequestStatus == "Completed");
            var donorSearch = allEmergency.Count(r => r.RequestStatus == "Searching Donor" || r.RequestStatus == "Rejected");

            var viewModel = new EmergencyRequestsViewModel
            {
                TotalEmergencyRequests = total,
                PendingEmergencyRequests = pending,
                BloodAvailableRequests = allEmergency.Count(r => { var stock = bloodInventory.Where(i => i.BloodGroup == r.BloodGroup).Sum(i => i.Quantity); return stock >= (r.UnitsRequired ?? 1); }),
                DonorSearchRequired = allEmergency.Count(r => { var stock = bloodInventory.Where(i => i.BloodGroup == r.BloodGroup).Sum(i => i.Quantity); return stock < (r.UnitsRequired ?? 1); }),
                ApprovedEmergencyRequests = approved,
                CompletedEmergencyRequests = completed,
                PendingPercentage = total > 0 ? (pending * 100) / total : 0,
                ApprovedPercentage = total > 0 ? (approved * 100) / total : 0,
                CompletedPercentage = total > 0 ? (completed * 100) / total : 0,
                DonorSearchPercentage = total > 0 ? (donorSearch * 100) / total : 0,
                Requests = requestItems,
                Filters = filters,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                TotalCount = totalCount,
                PageSize = pageSize,
                AvailableCities = cities
            };

            ViewData["Title"] = "Emergency Requests";
            return View(viewModel);
        }

        // ==========================================
        // REQUEST TRACKING
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> RequestTracking(RequestTrackingFilters filters, int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return RedirectToAction("Login", "Account");
            var hospitalId = hospitalProfile.HospitalId;

            var query = _context.BloodRequests.Include(r => r.Receiver).Where(r => r.HospitalId == hospitalId &&
                (r.RequestStatus == "Approved" || r.RequestStatus == "In Progress" || r.RequestStatus == "Searching Donor" ||
                 r.RequestStatus == "Blood Reserved" || r.RequestStatus == "Blood Collected" || r.RequestStatus == "Inventory Updated" ||
                 r.RequestStatus == "Blood Ready" || r.RequestStatus == "Issued" || r.RequestStatus == "Completed"));

            if (!string.IsNullOrEmpty(filters.SearchQuery)) query = query.Where(r => r.PatientName.Contains(filters.SearchQuery) || r.BloodGroup.Contains(filters.SearchQuery) || r.RequestId.ToString().Contains(filters.SearchQuery));
            if (filters.Status != "all" && !string.IsNullOrEmpty(filters.Status)) query = query.Where(r => r.RequestStatus == filters.Status);
            if (filters.Priority != "all" && !string.IsNullOrEmpty(filters.Priority)) query = query.Where(r => r.UrgencyLevel == filters.Priority);
            if (filters.BloodGroup != "all" && !string.IsNullOrEmpty(filters.BloodGroup)) query = query.Where(r => r.BloodGroup == filters.BloodGroup);

            query = filters.SortBy switch
            {
                "oldest" => query.OrderBy(r => r.CreatedAt),
                "priority" => query.OrderByDescending(r => r.UrgencyLevel == "Critical").ThenByDescending(r => r.UrgencyLevel == "High").ThenByDescending(r => r.UrgencyLevel == "Normal"),
                "stage" => query.OrderBy(r => r.RequestStatus),
                _ => query.OrderByDescending(r => r.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var pageSize = 10;
            var currentPage = page < 1 ? 1 : page;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var requests = await query.Skip((currentPage - 1) * pageSize).Take(pageSize).ToListAsync();

            var requestItems = requests.Select(r => new BloodRequestViewModel
            {
                RequestId = r.RequestId,
                ReceiverId = r.ReceiverId,
                ReceiverName = r.PatientName ?? r.Receiver?.FullName ?? "Unknown",
                ReceiverEmail = r.Receiver?.Email ?? "",
                ReceiverPhone = r.HospitalContact ?? "",
                BloodGroup = r.BloodGroup ?? "Unknown",
                UnitsRequired = r.UnitsRequired ?? 1,
                HospitalName = r.HospitalName ?? hospitalProfile?.HospitalName ?? "Hospital",
                City = r.City ?? hospitalProfile?.City ?? "",
                UrgencyLevel = r.UrgencyLevel ?? "Normal",
                RequestStatus = r.RequestStatus,
                CreatedDate = r.CreatedAt,
                RequiredDate = r.RequiredDate
            }).ToList();

            var allTrackedRequests = await _context.BloodRequests.Where(r => r.HospitalId == hospitalId &&
                (r.RequestStatus == "Approved" || r.RequestStatus == "In Progress" || r.RequestStatus == "Searching Donor" ||
                 r.RequestStatus == "Blood Reserved" || r.RequestStatus == "Blood Collected" || r.RequestStatus == "Inventory Updated" ||
                 r.RequestStatus == "Blood Ready" || r.RequestStatus == "Issued" || r.RequestStatus == "Completed")).ToListAsync();

            var viewModel = new BloodRequestListViewModel
            {
                Requests = requestItems,
                TotalCount = totalCount,
                PendingCount = allTrackedRequests.Count(r => r.RequestStatus == "Approved" || r.RequestStatus == "In Progress"),
                FulfilledCount = allTrackedRequests.Count(r => r.RequestStatus == "Completed"),
                EmergencyCount = allTrackedRequests.Count(r => r.UrgencyLevel == "Critical"),
                ActiveCitiesCount = allTrackedRequests.Select(r => r.City).Distinct().Count(),
                AvailableCities = allTrackedRequests.Where(r => !string.IsNullOrEmpty(r.City)).Select(r => r.City!).Distinct().OrderBy(c => c).ToList(),
                CurrentPage = currentPage,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            ViewData["Title"] = "Request Tracking";
            return View(viewModel);
        }

        // ==========================================
        // SMART DONOR SEARCH (Workflow Improved)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> SmartDonorSearch(DonorSearchFilters filters, int? requestId, int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return RedirectToAction("Login", "Account");
            var hospitalId = hospitalProfile.HospitalId;

            // Auto-expire pending matches
            var expiredMatches = await _context.DonorMatches.Where(dm => dm.HospitalId == hospitalId && dm.Status == "Pending Response" && dm.ResponseDeadline < DateTime.Now).ToListAsync();
            foreach (var match in expiredMatches)
            {
                match.Status = "Expired";
                var exists = await _context.HospitalNotifications.AnyAsync(n => n.HospitalId == hospitalId && n.Message.Contains($"Donor ID {match.DonorId} expired"));
                if (!exists)
                {
                    _context.HospitalNotifications.Add(new HospitalNotification
                    {
                        HospitalId = hospitalId,
                        Title = "Assignment Expired",
                        Message = $"Assignment for Donor ID {match.DonorId} expired due to no response.",
                        Category = "Audit",
                        Priority = "Medium",
                        CreatedDate = DateTime.Now
                    });
                }
            }
            if (expiredMatches.Any()) await _context.SaveChangesAsync();

            BloodRequestSummary? selectedRequest = null;
            if (requestId.HasValue)
            {
                var req = await _context.BloodRequests.FirstOrDefaultAsync(r => r.RequestId == requestId.Value && (r.RequestStatus == "Pending" || r.RequestStatus == "Searching Donor" || r.RequestStatus == "Emergency"));
                if (req != null)
                {
                    selectedRequest = new BloodRequestSummary
                    {
                        RequestId = req.RequestId,
                        ReceiverName = req.PatientName ?? "Unknown",
                        BloodGroup = req.BloodGroup ?? "Unknown",
                        UnitsRequired = req.UnitsRequired ?? 1,
                        UrgencyLevel = req.UrgencyLevel ?? "Normal",
                        HospitalName = req.HospitalName ?? "Unknown",
                        RequestDate = req.CreatedAt
                    };
                    filters.BloodGroup = req.BloodGroup ?? "all";
                }
            }

            var targetBloodGroup = selectedRequest != null ? selectedRequest.BloodGroup : filters.BloodGroup;

            // 1. Inventory Validation Before Smart Donor Search (Using SumAsync for accurate total units)
            var totalInventoryUnits = await _context.BloodInventory
                .Where(i => i.HospitalId == hospitalId && i.BloodGroup == targetBloodGroup && i.Status != "Expired" && i.Status != "Used")
                .SumAsync(i => (int?)i.Quantity) ?? 0;

            bool isInventorySufficient = selectedRequest != null && totalInventoryUnits >= selectedRequest.UnitsRequired;

            var donorResults = new List<HospitalDonorResult>();
            int totalCount = 0;
            int totalRegisteredDonors = 0;

            // If inventory is NOT sufficient, proceed with donor search
            if (!isInventorySufficient)
            {
                // Performance Optimization: Pre-fetch data
                var donorProfiles = await _context.DonorProfiles.Include(dp => dp.User).Where(dp => dp.User != null && dp.User.IsActive).AsNoTracking().ToListAsync();
                totalRegisteredDonors = donorProfiles.Count;
                var donorIds = donorProfiles.Select(dp => dp.User.UserId).ToList();

                var lastDonations = await _context.Donations.Where(d => donorIds.Contains(d.DonorId) && d.Status == "Completed")
                    .GroupBy(d => d.DonorId).Select(g => g.OrderByDescending(d => d.DonationDate).FirstOrDefault())
                    .ToDictionaryAsync(d => d.DonorId, d => d);

                var totalDonationsCount = await _context.Donations.Where(d => donorIds.Contains(d.DonorId) && d.Status == "Completed")
                    .GroupBy(d => d.DonorId).Select(g => new { DonorId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.DonorId, x => x.Count);

                var donorMatchStats = await _context.DonorMatches.Where(dm => donorIds.Contains(dm.DonorId) && dm.HospitalId == hospitalId)
                    .GroupBy(dm => dm.DonorId).Select(g => new
                    {
                        DonorId = g.Key,
                        Total = g.Count(),
                        Accepted = g.Count(m => m.Status == "Accepted" || m.Status == "Completed"),
                        Rejected = g.Count(m => m.Status == "Rejected"),
                        Expired = g.Count(m => m.Status == "Expired"),
                        HospitalDonations = g.Count(m => m.Status == "Completed")
                    }).ToDictionaryAsync(x => x.DonorId);

                var activeMatches = await _context.DonorMatches
                    .Where(dm => (dm.Status == "Accepted" || dm.Status == "Pending Response" || dm.Status == "DonationScheduled"))
                    .Select(dm => dm.DonorId)
                    .ToListAsync();

                // ✅ PERFORMANCE FIX: Pre-fetch active matches to prevent N+1 queries
                var activeMatchesDict = await _context.DonorMatches
                    .Where(m => donorIds.Contains(m.DonorId) && m.HospitalId == hospitalId &&
                           (m.Status == "Accepted" || m.Status == "Pending Response" || m.Status == "DonationScheduled"))
                    .GroupBy(m => m.DonorId)
                    .ToDictionaryAsync(g => g.Key, g => g.OrderByDescending(m => m.MatchDate).FirstOrDefault());

                foreach (var dp in donorProfiles)
                {
                    var user = dp.User;

                    // Advanced Smart Search
                    if (!string.IsNullOrEmpty(filters.SearchQuery))
                    {
                        var q = filters.SearchQuery.ToLower();
                        bool match = (user.FullName != null && user.FullName.ToLower().Contains(q)) ||
                                     user.UserId.ToString().Contains(q) ||
                                     (dp.BloodGroup != null && dp.BloodGroup.ToLower().Contains(q)) ||
                                     (user.Phone != null && user.Phone.ToLower().Contains(q)) ||
                                     (user.Email != null && user.Email.ToLower().Contains(q)) ||
                                     (dp.City != null && dp.City.ToLower().Contains(q)) ||
                                     (dp.Area != null && dp.Area.ToLower().Contains(q));
                        if (!match) continue;
                    }

                    // Intelligent Blood Compatibility Matching
                    if (filters.BloodGroup != "all" && !string.IsNullOrEmpty(filters.BloodGroup))
                    {
                        if (!IsCompatibleDonor(dp.BloodGroup, filters.BloodGroup) && dp.BloodGroup != filters.BloodGroup) continue;
                    }
                    else if (selectedRequest != null && !string.IsNullOrEmpty(selectedRequest.BloodGroup))
                    {
                        if (!IsCompatibleDonor(dp.BloodGroup, selectedRequest.BloodGroup) && dp.BloodGroup != selectedRequest.BloodGroup) continue;
                    }

                    var lastDonation = lastDonations.TryGetValue(user.UserId, out var ld) ? ld : null;
                    bool isEligible = lastDonation == null || (DateTime.Now - lastDonation.DonationDate).Days >= 90;
                    int totalDonations = totalDonationsCount.TryGetValue(user.UserId, out var td) ? td : 0;
                    var stats = donorMatchStats.TryGetValue(user.UserId, out var st) ? st : new { DonorId = user.UserId, Total = 0, Accepted = 0, Rejected = 0, Expired = 0, HospitalDonations = 0 };

                    double accRate = stats.Total > 0 ? Math.Round((double)stats.Accepted / stats.Total * 100, 1) : 0.0;
                    var (distanceKm, travelTime) = CalculateDistanceAndTime(dp.Latitude, dp.Longitude, hospitalProfile.Latitude, hospitalProfile.Longitude);

                    // AI Match Score Calculation
                    int score = CalculateMatchScore(dp, selectedRequest?.BloodGroup, selectedRequest?.UrgencyLevel, totalDonations, stats.HospitalDonations, isEligible, dp.IsAvailable, distanceKm, accRate);

                    // Dynamic AI Match Reasons
                    var reasons = new List<string>();
                    if (!string.IsNullOrEmpty(selectedRequest?.BloodGroup))
                    {
                        if (dp.BloodGroup == selectedRequest.BloodGroup) reasons.Add("Exact Blood Group Match");
                        else if (IsCompatibleDonor(dp.BloodGroup, selectedRequest.BloodGroup)) reasons.Add("Compatible Blood Group");
                    }
                    if (isEligible && dp.IsAvailable) reasons.Add("Available & Eligible Today");
                    else if (isEligible) reasons.Add("Eligible (Cooldown Completed)");

                    if (distanceKm <= 5) reasons.Add($"Only {distanceKm} km Away");
                    else if (distanceKm <= 15) reasons.Add($"Nearby ({distanceKm} km)");

                    if (accRate >= 80) reasons.Add("High Acceptance Rate");
                    else if (accRate >= 50) reasons.Add("Good Acceptance Rate");

                    if (stats.HospitalDonations >= 2) reasons.Add("Trusted by This Hospital");
                    else if (stats.HospitalDonations >= 1) reasons.Add("Previously Donated Here");

                    if (totalDonations >= 5) reasons.Add("Highly Experienced Donor");
                    else if (totalDonations >= 2) reasons.Add("Experienced Donor");

                    if (selectedRequest?.UrgencyLevel == "Critical" || selectedRequest?.UrgencyLevel == "Emergency") reasons.Add("Emergency Priority Boost");

                    if (lastDonation != null && (DateTime.Now - lastDonation.DonationDate).Days <= 180) reasons.Add("Recently Active");

                    string quality = score >= 90 ? "Excellent Match" : score >= 75 ? "Very Good Match" : score >= 60 ? "Good Match" : "Fair Match";

                    string smartStatus = "Unavailable";
                    DateTime? responseDeadline = null;

                    if (activeMatchesDict.TryGetValue(user.UserId, out var activeMatch))
                    {
                        smartStatus = activeMatch.Status switch
                        {
                            "Pending Response" => "Pending Response",
                            "Accepted" => "Already Assigned",
                            "DonationScheduled" => "Already Assigned",
                            _ => "Already Assigned"
                        };
                        responseDeadline = activeMatch.ResponseDeadline;
                    }
                    else if (!isEligible)
                    {
                        smartStatus = "In Cooldown";
                    }
                    else if (dp.IsAvailable)
                    {
                        smartStatus = "Available Now";
                    }

                    donorResults.Add(new HospitalDonorResult
                    {
                        DonorId = user.UserId,
                        FullName = user.FullName ?? "Unknown",
                        ProfilePicture = user.ProfilePicture ?? "/assets/img/avatars/DefaultAvatar.png",
                        BloodGroup = dp.BloodGroup ?? "Unknown",
                        City = dp.City ?? "Unknown",
                        Area = dp.Area ?? "Unknown",
                        Gender = dp.Gender ?? "Unknown",
                        Age = dp.DateOfBirth.HasValue ? (int)(DateTime.Now.Year - dp.DateOfBirth.Value.Year) : (int?)null,
                        Phone = user.Phone ?? "",
                        Email = user.Email ?? "",
                        IsAvailable = dp.IsAvailable,
                        LastDonationDate = lastDonation?.DonationDate,
                        TotalDonations = totalDonations,
                        IsVerified = true,
                        IsEligibleToDonate = isEligible,
                        MatchScore = score,
                        MatchQuality = quality,
                        MatchReasons = reasons,
                        AvailabilityStatus = smartStatus,
                        RegistrationDate = user.CreatedAt,
                        Address = dp.Address ?? "",
                        PreviousHospitalDonations = stats.HospitalDonations,
                        CommunicationStatus = activeMatches.Contains(user.UserId) ? "Email & Notification Sent" : "None",
                        ResponseDeadline = null,
                        DistanceKm = distanceKm,
                        TravelTime = travelTime,
                        TotalRequestsReceived = stats.Total,
                        AcceptedRequests = stats.Accepted,
                        RejectedRequests = stats.Rejected,
                        ExpiredRequests = stats.Expired,
                        AcceptanceRate = accRate,
                        Weight = dp.Weight,
                        NextEligibleDate = lastDonation?.DonationDate.AddDays(90)
                    });
                }

                donorResults = ApplySorting(donorResults, filters.SortBy, selectedRequest?.UrgencyLevel);
                totalCount = donorResults.Count;

                // ✅ PERFORMANCE FIX: Use dictionary instead of loop queries
                foreach (var donor in donorResults)
                {
                    if (activeMatchesDict.TryGetValue(donor.DonorId, out var pendingMatch) && pendingMatch?.Status == "Pending Response")
                    {
                        donor.ResponseDeadline = pendingMatch.ResponseDeadline;
                    }
                }
            }

            var pageSize = 10;
            var currentPage = page < 1 ? 1 : page;
            var totalPages = isInventorySufficient ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
            var paginatedDonors = isInventorySufficient ? new List<HospitalDonorResult>() : donorResults.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

            var viewModel = new HospitalDonorSearchViewModel
            {
                SelectedRequest = selectedRequest,
                TotalRegisteredDonors = totalRegisteredDonors,
                AvailableDonors = donorResults.Count(d => d.IsAvailable && d.IsEligibleToDonate),
                MatchingDonors = donorResults.Count,
                AssignedDonors = await _context.DonorMatches.CountAsync(m => m.HospitalId == hospitalId && (m.Status == "Accepted" || m.Status == "Completed")),
                EmergencyMatches = await _context.DonorMatches.CountAsync(m => m.HospitalId == hospitalId && m.Status == "Accepted" && m.BloodRequest != null && m.BloodRequest.UrgencyLevel == "Critical"),
                EligibleDonors = donorResults.Count(d => d.IsEligibleToDonate),
                DonorsContacted = await _context.DonorMatches.CountAsync(m => m.HospitalId == hospitalId),
                Donors = paginatedDonors,
                Filters = filters,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize,
                IsBloodInventorySufficient = isInventorySufficient,
                CurrentStockUnits = totalInventoryUnits,
                RequiredBloodGroup = targetBloodGroup ?? ""
            };

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_DonorTablePartial", viewModel);
            }

            return View(viewModel);
        }

        // ==========================================
        // ASSIGN DONOR (Final Validation Added)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDonor(int donorId, int requestId)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Json(new { success = false, message = "User not authenticated." });

                var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
                if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });
                var hospitalId = hospitalProfile.HospitalId;

                var request = await _context.BloodRequests.FirstOrDefaultAsync(r => r.RequestId == requestId);
                if (request == null) return Json(new { success = false, message = "Request not found." });
                if (request.HospitalId.HasValue && request.HospitalId.Value != hospitalId)
                    return Json(new { success = false, message = "This request doesn't belong to your hospital." });
                if (request.RequestStatus == "Completed" || request.RequestStatus == "Cancelled" || request.RequestStatus == "Fulfilled" || request.RequestStatus == "Rejected")
                    return Json(new { success = false, message = "This request is no longer active." });

                // 1. Inventory Check (If sufficient, no need to assign donor)
                var totalInventoryUnits = await _context.BloodInventory
                    .Where(i => i.HospitalId == hospitalId && i.BloodGroup == request.BloodGroup && i.Status != "Expired" && i.Status != "Used")
                    .SumAsync(i => (int?)i.Quantity) ?? 0;

                if (totalInventoryUnits >= (request.UnitsRequired ?? 1))
                {
                    return Json(new
                    {
                        success = true,
                        isInventorySufficient = true,
                        message = $"✅ Inventory Sufficient: {totalInventoryUnits} units of {request.BloodGroup} are available. This request can be fulfilled directly from hospital inventory."
                    });
                }

                var donorProfile = await _context.DonorProfiles.Include(dp => dp.User).FirstOrDefaultAsync(dp => dp.UserId == donorId);
                if (donorProfile == null || donorProfile.User == null) return Json(new { success = false, message = "Donor profile not found." });
                if (!donorProfile.User.IsActive) return Json(new { success = false, message = "Donor account is inactive or blocked." });
                if (!donorProfile.IsAvailable) return Json(new { success = false, message = "Donor is currently marked as unavailable." });

                // 2. Check if donor is already assigned to another ACTIVE request
                var donorActiveMatch = await _context.DonorMatches
                    .Include(dm => dm.BloodRequest)
                    .FirstOrDefaultAsync(dm => dm.DonorId == donorId && (dm.Status == "Pending Response" || dm.Status == "Accepted" || dm.Status == "DonationScheduled"));

                if (donorActiveMatch != null)
                {
                    return Json(new { success = false, message = $"This donor is already assigned to Request REQ-{donorActiveMatch.BloodRequestId:D4}. Please choose another donor." });
                }

                // 3. Blood Compatibility Check
                if (!IsCompatibleDonor(donorProfile.BloodGroup, request.BloodGroup))
                    return Json(new { success = false, message = $"Blood group {donorProfile.BloodGroup} is not compatible with {request.BloodGroup}." });

                // 4. Cooldown Check
                var lastDonation = await _context.Donations.Where(d => d.DonorId == donorId && d.Status == "Completed").OrderByDescending(d => d.DonationDate).FirstOrDefaultAsync();
                if (lastDonation != null && (DateTime.Now - lastDonation.DonationDate).Days < 90)
                    return Json(new { success = false, message = "Donor is currently in the 90-day cooldown period." });

                // ✅ 5. REASSIGNMENT LOGIC: Cancel any existing PENDING assignment for this request
                var previousMatch = await _context.DonorMatches.FirstOrDefaultAsync(m => m.BloodRequestId == requestId && m.Status == "Pending Response");
                if (previousMatch != null && previousMatch.DonorId != donorId)
                {
                    previousMatch.Status = "Cancelled";
                    previousMatch.RejectionReason = "Reassigned by hospital to another donor";
                    previousMatch.CancelledDate = DateTime.Now;
                }

                // 6. Calculate Match Score
                var stats = await _context.DonorMatches.Where(dm => dm.DonorId == donorId && dm.HospitalId == hospitalId).GroupBy(dm => dm.DonorId).Select(g => new { Total = g.Count(), Accepted = g.Count(m => m.Status == "Accepted" || m.Status == "Completed"), HospitalDonations = g.Count(m => m.Status == "Completed") }).FirstOrDefaultAsync();
                int totalDonations = await _context.Donations.CountAsync(d => d.DonorId == donorId && d.Status == "Completed");
                int prevHospitalDonations = stats?.HospitalDonations ?? 0;
                double accRate = (stats?.Total ?? 0) > 0 ? Math.Round((double)(stats?.Accepted ?? 0) / (stats?.Total ?? 1) * 100, 1) : 0.0;
                bool isEligible = lastDonation == null || (DateTime.Now - lastDonation.DonationDate).Days >= 90;
                var (distanceKm, travelTime) = CalculateDistanceAndTime(donorProfile.Latitude, donorProfile.Longitude, hospitalProfile.Latitude, hospitalProfile.Longitude);
                int calculatedScore = CalculateMatchScore(donorProfile, request.BloodGroup, request.UrgencyLevel, totalDonations, prevHospitalDonations, isEligible, donorProfile.IsAvailable, distanceKm, accRate);

                // 7. Create New Assignment
                var donorMatch = new DonorMatch
                {
                    BloodRequestId = requestId,
                    DonorId = donorId,
                    HospitalId = hospitalId,
                    MatchScore = calculatedScore,
                    Status = "Pending Response",
                    MatchDate = DateTime.Now,
                    ResponseDeadline = DateTime.Now.AddHours(2),
                    DistanceKm = distanceKm,
                    TravelTime = travelTime
                };
                _context.DonorMatches.Add(donorMatch);

                if (request.RequestStatus == "Pending" || request.RequestStatus == "Approved")
                {
                    request.RequestStatus = "Searching Donor";
                }

                _context.HospitalNotifications.Add(new HospitalNotification
                {
                    HospitalId = hospitalId,
                    Title = "Donor Assigned",
                    Message = $"Donor {donorProfile.User.FullName} (ID: {donorId}) assigned to Request REQ-{request.RequestId:D4}.",
                    Category = "DonorResponse",
                    Priority = "High",
                    CreatedDate = DateTime.Now
                });

                await _context.SaveChangesAsync();

                // 8. Send Notification Email to Donor
                if (!string.IsNullOrEmpty(donorProfile.User.Email))
                {
                    try
                    {
                        await _emailService.SendDonationRequestEmailAsync(donorProfile.User.Email, donorProfile.User.FullName, request.RequestId, request.BloodGroup ?? "Unknown", request.HospitalName ?? "Our Hospital");
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Email error: {ex.Message}"); }
                }

                return Json(new { success = true, message = "Donor assigned successfully! Notification sent to the donor." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // Add these helper methods OUTSIDE of the SmartDonorSearch action (as private class methods)

        // Helper: Apply Sorting (Should be a private method of the class, NOT inside SmartDonorSearch)
        private List<HospitalDonorResult> ApplySorting(List<HospitalDonorResult> donors, string sortBy, string? urgencyLevel)
        {
            if (urgencyLevel == "Critical" || urgencyLevel == "Emergency")
            {
                return donors.OrderByDescending(d => d.MatchScore)
                    .ThenBy(d => d.DistanceKm)
                    .ThenBy(d => d.AvailabilityStatus == "Available Now" ? 0 : 1)
                    .ToList();
            }

            return sortBy switch
            {
                "nearest" => donors.OrderBy(d => d.DistanceKm).ThenByDescending(d => d.MatchScore).ToList(),
                "recentlyactive" => donors.OrderByDescending(d => d.LastDonationDate ?? DateTime.MinValue).ToList(),
                "highestacceptance" => donors.OrderByDescending(d => d.AcceptanceRate).ToList(),
                "mostdonations" => donors.OrderByDescending(d => d.TotalDonations).ToList(),
                "prevhospital" => donors.OrderByDescending(d => d.PreviousHospitalDonations).ThenByDescending(d => d.MatchScore).ToList(),
                "highestscore" => donors.OrderByDescending(d => d.MatchScore).ToList(),
                _ => donors.OrderByDescending(d => d.MatchScore).ThenBy(d => d.DistanceKm).ToList()
            };
        }

        // AI Match Score Calculation (Should be a private method of the class)
        private int CalculateMatchScore(DonorProfile dp, string? targetBloodGroup, string? urgencyLevel, int totalDonations, int prevHospitalDonations, bool isEligible, bool isAvailable, double distanceKm, double acceptanceRate)
        {
            int score = 0;

            if (!string.IsNullOrEmpty(targetBloodGroup))
            {
                if (dp.BloodGroup == targetBloodGroup) score += 40;
                else if (IsCompatibleDonor(dp.BloodGroup, targetBloodGroup)) score += 25;
            }
            else { score += 20; }

            if (distanceKm <= 5) score += 20;
            else if (distanceKm <= 15) score += 15;
            else if (distanceKm <= 30) score += 10;
            else if (distanceKm <= 50) score += 5;

            if (isEligible && isAvailable) score += 15;
            else if (isEligible) score += 10;
            else score += 5;

            if (acceptanceRate >= 80) score += 10;
            else if (acceptanceRate >= 50) score += 7;
            else if (acceptanceRate >= 20) score += 3;

            if (prevHospitalDonations >= 3) score += 10;
            else if (prevHospitalDonations >= 1) score += 7;

            if (totalDonations >= 5) score += 5;
            else if (totalDonations >= 2) score += 3;

            if (urgencyLevel == "Critical" || urgencyLevel == "Emergency") score += 5;

            return Math.Min(100, score);
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingRequests()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Json(new { success = false, message = "User not authenticated." });

                var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
                if (hospitalProfile == null)
                    return Json(new { success = false, message = "Hospital profile not found." });

                var hospitalId = hospitalProfile.HospitalId;
                var hospitalName = hospitalProfile.HospitalName ?? "Unknown";

                // ✅ UPDATED: Include "Approved" status and requests assigned to this hospital
                var requests = await _context.BloodRequests
                    .Where(r =>
                        (r.RequestStatus == "Pending" ||
                         r.RequestStatus == "Searching Donor" ||
                         r.RequestStatus == "Approved" ||  // ✅ YE ADD KIYA
                         r.UrgencyLevel == "Emergency") &&
                        (r.HospitalId == null || r.HospitalId == hospitalId))
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new
                    {
                        requestId = r.RequestId,
                        bloodGroup = r.BloodGroup != null ? r.BloodGroup : "Unknown",
                        hospitalName = r.HospitalName != null ? r.HospitalName : hospitalName,
                        urgencyLevel = r.UrgencyLevel != null ? r.UrgencyLevel : "Normal",
                        unitsRequired = r.UnitsRequired.GetValueOrDefault(1),
                        createdAt = r.CreatedAt,
                        requestStatus = r.RequestStatus != null ? r.RequestStatus : "Pending"
                    })
                    .ToListAsync();

                return Json(new { success = true, requests });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading requests: " + ex.Message });
            }
        }

        // Fix for line 1236 - Move optional parameters to the end
        [HttpGet]
        public async Task<IActionResult> ExportDonors(string format = "csv", [FromQuery] DonorSearchFilters filters = null, int? requestId = null)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Unauthorized();

            filters ??= new DonorSearchFilters();
            var donors = await GetMatchedDonorsForExportAsync(hospitalProfile.HospitalId, filters, requestId);

            if (format == "csv")
            {
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("DonorID,Name,BloodGroup,City,DistanceKm,MatchScore,Status,Phone,Email");
                foreach (var d in donors)
                {
                    csv.AppendLine($"{d.DonorId},\"{d.FullName}\",{d.BloodGroup},\"{d.City}\",{d.DistanceKm},{d.MatchScore},\"{d.AvailabilityStatus}\",\"{d.Phone}\",\"{d.Email}\"");
                }
                return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"SmartDonorMatches_{DateTime.Now:yyyyMMdd}.csv");
            }

            return BadRequest("Unsupported format");
        }

        // Complete fixed Helper method for GetMatchedDonorsForExportAsync
        private async Task<List<HospitalDonorResult>> GetMatchedDonorsForExportAsync(int hospitalId, DonorSearchFilters filters, int? requestId)
        {
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.HospitalId == hospitalId);
            var donorProfiles = await _context.DonorProfiles.Include(dp => dp.User).Where(dp => dp.User != null && dp.User.IsActive).AsNoTracking().Take(200).ToListAsync();
            var results = new List<HospitalDonorResult>();

            foreach (var dp in donorProfiles)
            {
                var user = dp.User;
                var (distanceKm, travelTime) = CalculateDistanceAndTime(dp.Latitude, dp.Longitude, hospitalProfile?.Latitude, hospitalProfile?.Longitude);
                results.Add(new HospitalDonorResult
                {
                    DonorId = user.UserId,
                    FullName = user.FullName ?? "Unknown",
                    BloodGroup = dp.BloodGroup ?? "Unknown",
                    City = dp.City ?? "Unknown",
                    DistanceKm = distanceKm,
                    MatchScore = CalculateMatchScore(dp, filters.BloodGroup != "all" ? filters.BloodGroup : null, null, 0, 0, true, dp.IsAvailable, distanceKm, 0),
                    AvailabilityStatus = dp.IsAvailable ? "Available Now" : "Unavailable",
                    Phone = user.Phone ?? "",
                    Email = user.Email ?? "",
                    IsVerified = true // Fixed: Use hardcoded value instead of user.IsVerified
                });
            }
            return results.OrderByDescending(d => d.MatchScore).Take(100).ToList();
        }

        private bool IsCompatibleDonor(string? donorBloodGroup, string? requiredBloodGroup)
        {
            if (string.IsNullOrEmpty(donorBloodGroup) || string.IsNullOrEmpty(requiredBloodGroup)) return false;

            var compatibility = new Dictionary<string, List<string>>
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
            return compatibility.ContainsKey(donorBloodGroup) && compatibility[donorBloodGroup].Contains(requiredBloodGroup);
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

        [HttpGet]
        public async Task<IActionResult> CheckDonorAvailability(int donorId)
        {
            var activeMatch = await _context.DonorMatches
                .FirstOrDefaultAsync(dm => dm.DonorId == donorId &&
                    (dm.Status == "Accepted" || dm.Status == "Pending Response" || dm.Status == "DonationScheduled"));

            if (activeMatch != null)
            {
                return Json(new
                {
                    isAvailable = false,
                    message = "This donor is already assigned to another request."
                });
            }

            var donorProfile = await _context.DonorProfiles
                .Include(dp => dp.User)
                .FirstOrDefaultAsync(dp => dp.UserId == donorId);

            if (donorProfile == null || !donorProfile.User.IsActive)
            {
                return Json(new
                {
                    isAvailable = false,
                    message = "Donor account is inactive."
                });
            }

            if (!donorProfile.IsAvailable)
            {
                return Json(new
                {
                    isAvailable = false,
                    message = "Donor is currently unavailable."
                });
            }

            var lastDonation = await _context.Donations
                .Where(d => d.DonorId == donorId && d.Status == "Completed")
                .OrderByDescending(d => d.DonationDate)
                .FirstOrDefaultAsync();

            if (lastDonation != null && (DateTime.Now - lastDonation.DonationDate).Days < 90)
            {
                return Json(new
                {
                    isAvailable = false,
                    message = "Donor is in 90-day cooldown period."
                });
            }

            return Json(new { isAvailable = true });
        }

        // ==========================================
        // ASSIGNED DONORS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> AssignedDonors(AssignedDonorFilters filters, int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return RedirectToAction("Login", "Account");
            var hospitalId = hospitalProfile.HospitalId;

            // ✅ FIXED: Added .ThenInclude(d => d.DonorProfile) to fetch real Blood Group
            var query = _context.DonorMatches
                .Include(dm => dm.Donor)
                    .ThenInclude(d => d.DonorProfile)
                .Include(dm => dm.BloodRequest)
                .Where(dm => dm.HospitalId == hospitalId);

            // Filtering
            if (!string.IsNullOrEmpty(filters.SearchQuery))
            {
                query = query.Where(dm =>
                    (dm.Donor != null && dm.Donor.FullName.Contains(filters.SearchQuery)) ||
                    dm.BloodRequestId.ToString().Contains(filters.SearchQuery) ||
                    (dm.BloodRequest != null && dm.BloodRequest.PatientName != null && dm.BloodRequest.PatientName.Contains(filters.SearchQuery))
                );
            }
            if (filters.Status != "all" && !string.IsNullOrEmpty(filters.Status))
                query = query.Where(dm => dm.Status == filters.Status);
            if (filters.BloodGroup != "all" && !string.IsNullOrEmpty(filters.BloodGroup))
                query = query.Where(dm => dm.Donor != null && dm.Donor.DonorProfile != null && dm.Donor.DonorProfile.BloodGroup == filters.BloodGroup);

            // Sorting
            query = filters.SortBy switch
            {
                "oldest" => query.OrderBy(dm => dm.MatchDate),
                "score" => query.OrderByDescending(dm => dm.MatchScore),
                _ => query.OrderByDescending(dm => dm.MatchDate)
            };

            var totalCount = await query.CountAsync();
            var pageSize = 10;
            var currentPage = page < 1 ? 1 : page;
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var matches = await query.Skip((currentPage - 1) * pageSize).Take(pageSize).ToListAsync();

            var assignedDonors = matches.Select(dm =>
            {
                var statusDisplay = dm.Status switch
                {
                    "Pending Response" => "Pending Response",
                    "Accepted" => "Accepted",
                    "DonationScheduled" => "Scheduled",
                    "Completed" => "Completed",
                    "Rejected" => "Rejected",
                    "Expired" => "Expired",
                    "Cancelled" => "Cancelled",
                    _ => dm.Status
                };

                var statusBadgeClass = dm.Status switch
                {
                    "Pending Response" => "badge-pending",
                    "Accepted" => "badge-approved",
                    "DonationScheduled" => "badge-reserved",
                    "Completed" => "badge-completed",
                    "Rejected" => "badge-rejected",
                    "Expired" => "badge-delayed",
                    "Cancelled" => "badge-rejected",
                    _ => "badge-normal"
                };

                return new AssignedDonorItemViewModel
                {
                    MatchId = dm.MatchId,
                    DonorId = dm.DonorId,
                    DonorName = dm.Donor?.FullName ?? "Unknown",
                    DonorBloodGroup = dm.Donor?.DonorProfile?.BloodGroup ?? "Unknown", // ✅ NOW THIS WILL WORK PERFECTLY
                    RequestId = dm.BloodRequestId,
                    PatientName = dm.BloodRequest?.PatientName ?? "Unknown",
                    UnitsRequired = dm.BloodRequest?.UnitsRequired ?? 1,
                    AssignedDate = dm.MatchDate,
                    ResponseDeadline = dm.ResponseDeadline,
                    Status = dm.Status,
                    StatusDisplay = statusDisplay,
                    StatusBadgeClass = statusBadgeClass,
                    MatchScore = dm.MatchScore,
                    DonationScheduledDate = dm.DonationScheduledDate,
                    Notes = dm.Notes
                };
            }).ToList();

            var allMatches = await _context.DonorMatches.Where(dm => dm.HospitalId == hospitalId).ToListAsync();

            var viewModel = new AssignedDonorsViewModel
            {
                TotalAssigned = allMatches.Count,
                PendingResponse = allMatches.Count(m => m.Status == "Pending Response"),
                AcceptedOrScheduled = allMatches.Count(m => m.Status == "Accepted" || m.Status == "DonationScheduled"),
                Completed = allMatches.Count(m => m.Status == "Completed"),
                RejectedOrExpired = allMatches.Count(m => m.Status == "Rejected" || m.Status == "Expired" || m.Status == "Cancelled"),
                Donors = assignedDonors,
                Filters = filters,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize
            };

            ViewData["Title"] = "Assigned Donors";
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAssignment(int matchId, string reason)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });

            var match = await _context.DonorMatches.FirstOrDefaultAsync(m => m.MatchId == matchId && m.HospitalId == hospitalProfile.HospitalId);
            if (match == null) return Json(new { success = false, message = "Assignment not found." });
            if (match.Status == "Completed" || match.Status == "Cancelled" || match.Status == "Rejected")
                return Json(new { success = false, message = "This assignment cannot be cancelled." });

            match.Status = "Cancelled";
            match.RejectionReason = reason ?? "Cancelled by hospital";
            match.CancelledDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Assignment cancelled successfully." });
        }

        // ==========================================
        // NOTIFICATIONS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Notifications([FromQuery] HospitalNotificationFiltersViewModel filters, [FromQuery] int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return RedirectToAction("Login", "Account");
            var hospitalId = hospitalProfile.HospitalId;

            filters ??= new HospitalNotificationFiltersViewModel();
            if (page < 1) page = 1;
            const int pageSize = 10;

            var allNotifications = await _context.HospitalNotifications
                .Where(n => n.HospitalId == hospitalId)
                .Select(n => new HospitalNotificationItemViewModel
                {
                    NotificationId = n.NotificationId,
                    Title = n.Title ?? "Notification",
                    Message = n.Message ?? "No message",
                    Category = n.Category ?? "System",
                    Priority = n.Priority ?? "Medium",
                    RequestId = n.RequestId,
                    DonorId = n.DonorId,
                    ActionUrl = n.ActionUrl,
                    IsRead = n.IsRead,
                    CreatedDate = n.CreatedDate
                })
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            var filteredList = allNotifications.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filters.SearchQuery))
            {
                var s = filters.SearchQuery.Trim().ToLower();
                filteredList = filteredList.Where(n =>
                    (n.Title != null && n.Title.ToLower().Contains(s)) ||
                    (n.Message != null && n.Message.ToLower().Contains(s)) ||
                    (n.Category != null && n.Category.ToLower().Contains(s)) ||
                    (n.Priority != null && n.Priority.ToLower().Contains(s)) ||
                    (n.RequestId.HasValue && n.RequestId.Value.ToString().Contains(filters.SearchQuery.Trim())));
            }

            if (filters.ReadStatus == "unread")
                filteredList = filteredList.Where(n => !n.IsRead);
            else if (filters.ReadStatus == "read")
                filteredList = filteredList.Where(n => n.IsRead);

            if (!string.IsNullOrWhiteSpace(filters.Category) && filters.Category != "all")
                filteredList = filteredList.Where(n => n.Category == filters.Category);

            if (!string.IsNullOrWhiteSpace(filters.Priority) && filters.Priority != "all")
                filteredList = filteredList.Where(n => n.Priority == filters.Priority);

            filteredList = filters.SortOrder == "oldest"
                ? filteredList.OrderBy(n => n.CreatedDate)
                : filteredList.OrderByDescending(n => n.CreatedDate);

            var filteredQuery = filteredList.ToList();

            var stats = new HospitalNotificationStatisticsViewModel
            {
                TotalNotifications = allNotifications.Count,
                UnreadNotifications = allNotifications.Count(n => !n.IsRead),
                ReadNotifications = allNotifications.Count(n => n.IsRead),
                EmergencyAlerts = allNotifications.Count(n => n.Priority == "Critical" || n.Category == "Emergency"),
                InventoryAlerts = allNotifications.Count(n => n.Category == "Inventory")
            };

            var totalCount = filteredQuery.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (page > totalPages) page = totalPages > 0 ? totalPages : 1;

            var notifications = filteredQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            foreach (var n in notifications)
            {
                var styleResult = GetHospitalCategoryStyle(n.Category ?? "System");
                n.CategoryIcon = styleResult.icon;
                n.CategoryColor = styleResult.color;
                n.RequestCode = n.RequestId.HasValue ? $"REQ-{n.RequestId.Value:D4}" : null;
                n.TimeAgo = FormatTimeAgo(n.CreatedDate);
                n.FormattedDate = n.CreatedDate.ToString("dd MMM yyyy");
                n.FormattedTime = n.CreatedDate.ToString("h:mm tt");
            }

            var vm = new HospitalNotificationsViewModel
            {
                Statistics = stats,
                Filters = filters,
                Notifications = notifications,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize
            };

            ViewData["Title"] = "Notifications";
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationsLive()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Json(new { success = false, data = new { total = 0, unread = 0, read = 0, emergency = 0, inventory = 0 } });
            var hospitalId = hospitalProfile.HospitalId;

            var baseQuery = _context.HospitalNotifications.Where(n => n.HospitalId == hospitalId);
            return Json(new { success = true, data = new { total = await baseQuery.CountAsync(), unread = await baseQuery.CountAsync(n => !n.IsRead), read = await baseQuery.CountAsync(n => n.IsRead), emergency = await baseQuery.CountAsync(n => n.Priority == "Critical" || n.Category == "Emergency"), inventory = await baseQuery.CountAsync(n => n.Category == "Inventory") } });
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadNotificationCount()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Json(new { unread = 0 });
            var hospitalId = hospitalProfile.HospitalId;

            var unreadCount = await _context.HospitalNotifications.CountAsync(n => n.HospitalId == hospitalId && !n.IsRead);
            return Json(new { unread = unreadCount });
        }

        [HttpGet]
        public async Task<IActionResult> GetLatestNotifications()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Json(new { success = false, message = "User not authenticated", notifications = new List<object>(), unreadCount = 0 });

                var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
                if (hospitalProfile == null)
                    return Json(new { success = false, message = "Hospital profile not found", notifications = new List<object>(), unreadCount = 0 });

                var hospitalId = hospitalProfile.HospitalId;

                var dbNotifications = await _context.HospitalNotifications
                    .Where(n => n.HospitalId == hospitalId)
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(5)
                    .Select(n => new {
                        n.NotificationId,
                        n.Title,
                        n.Message,
                        n.Category,
                        n.IsRead,
                        n.CreatedDate,
                        n.RequestId,
                        n.ActionUrl
                    })
                    .ToListAsync();

                var unreadCount = await _context.HospitalNotifications.CountAsync(n => n.HospitalId == hospitalId && !n.IsRead);

                var notifications = dbNotifications.Select(n => new {
                    n.NotificationId,
                    Title = string.IsNullOrEmpty(n.Title) ? "Notification" : n.Title,
                    Message = string.IsNullOrEmpty(n.Message) ? "No message" : n.Message,
                    Category = string.IsNullOrEmpty(n.Category) ? "System" : n.Category,
                    n.IsRead,
                    n.CreatedDate,
                    n.RequestId,
                    ActionUrl = n.ActionUrl ?? (n.RequestId.HasValue ? "/Hospital/IncomingRequests" : "/Hospital/Notifications"),
                    TimeAgo = GetTimeAgo(n.CreatedDate)
                }).ToList();

                return Json(new { success = true, notifications, unreadCount });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message, notifications = new List<object>(), unreadCount = 0 }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });
            var hospitalId = hospitalProfile.HospitalId;

            var notif = await _context.HospitalNotifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.HospitalId == hospitalId);
            if (notif == null) return Json(new { success = false, message = "Notification not found." });
            notif.IsRead = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Marked as read." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsUnread(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });
            var hospitalId = hospitalProfile.HospitalId;

            var notif = await _context.HospitalNotifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.HospitalId == hospitalId);
            if (notif == null) return Json(new { success = false, message = "Notification not found." });
            notif.IsRead = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Marked as unread." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });
            var hospitalId = hospitalProfile.HospitalId;

            var unread = await _context.HospitalNotifications.Where(n => n.HospitalId == hospitalId && !n.IsRead).ToListAsync();
            foreach (var n in unread) n.IsRead = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Marked {unread.Count} notifications as read." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });
            var hospitalId = hospitalProfile.HospitalId;

            var notif = await _context.HospitalNotifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.HospitalId == hospitalId);
            if (notif == null) return Json(new { success = false, message = "Notification not found." });
            _context.HospitalNotifications.Remove(notif);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Notification deleted." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAction(string action, [FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return Json(new { success = false, message = "No notifications selected." });
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });
            var hospitalId = hospitalProfile.HospitalId;

            var notifications = await _context.HospitalNotifications.Where(n => ids.Contains(n.NotificationId) && n.HospitalId == hospitalId).ToListAsync();
            if (!notifications.Any()) return Json(new { success = false, message = "No valid notifications found." });

            if (action == "markread") foreach (var n in notifications) n.IsRead = true;
            else if (action == "markunread") foreach (var n in notifications) n.IsRead = false;
            else if (action == "delete") _context.HospitalNotifications.RemoveRange(notifications);
            else return Json(new { success = false, message = "Invalid action." });

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Action '{action}' applied to {notifications.Count} notification(s)." });
        }

        // ==========================================
        // BLOOD INVENTORY MANAGEMENT
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> BloodInventory(InventoryFilters filters, int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return RedirectToAction("Login", "Account");
            var hospitalId = hospitalProfile.HospitalId;

            var query = _context.BloodInventory.Where(i => i.HospitalId == hospitalId);

            if (!string.IsNullOrEmpty(filters.SearchQuery)) query = query.Where(i => i.BloodGroup.Contains(filters.SearchQuery) || i.Status.Contains(filters.SearchQuery));
            if (filters.BloodGroup != "all" && !string.IsNullOrEmpty(filters.BloodGroup)) query = query.Where(i => i.BloodGroup == filters.BloodGroup);

            string normalizedStatusFilter = filters.Status == "Low Stock" ? "Low Stock" : filters.Status == "LOW" ? "LOW" : filters.Status;
            if (normalizedStatusFilter != "all" && !string.IsNullOrEmpty(normalizedStatusFilter))
                query = query.Where(i => i.Status == normalizedStatusFilter || (i.Status == "LOW" && normalizedStatusFilter == "Low Stock"));

            query = filters.SortBy switch
            {
                "bloodgroup" => query.OrderBy(i => i.BloodGroup),
                "quantity" => query.OrderByDescending(i => i.Quantity),
                "expiry" => query.OrderBy(i => i.ExpiryDate),
                "oldest" => query.OrderBy(i => i.CreatedAt),
                _ => query.OrderByDescending(i => i.CreatedAt)
            };

            int pageSize = 10;
            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var inventory = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var today = DateTime.Today;

            foreach (var item in inventory)
            {
                if (item.Status == "LOW") item.Status = "Low Stock";
            }

            var viewModel = new BloodInventoryViewModel
            {
                TotalUnits = inventory.Sum(i => i.Quantity),
                AvailableUnits = inventory.Where(i => i.Status == "Available").Sum(i => i.Quantity),
                ExpiredUnits = inventory.Where(i => i.Status == "Expired" || i.ExpiryDate < today).Sum(i => i.Quantity),
                LowStockUnits = inventory.Where(i => i.Status == "Low Stock").Sum(i => i.Quantity),
                OutOfStockUnits = inventory.Where(i => i.Quantity == 0).Count(),
                ExpiringSoonUnits = inventory.Where(i => i.ExpiryDate <= today.AddDays(7) && i.ExpiryDate >= today && i.Status != "Expired").Sum(i => i.Quantity),
                AvailableBloodGroups = inventory.Where(i => i.Quantity > 0).Select(i => i.BloodGroup).Distinct().Count(),
                Inventory = inventory,
                Filters = filters,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize
            };

            ViewData["Title"] = "Blood Inventory";
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBloodStock(BloodInventoryAddViewModel model)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId)) return Json(new { success = false, message = "User not authenticated." });

                var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
                if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });

                if (string.IsNullOrEmpty(model.BloodGroup)) return Json(new { success = false, message = "Please select a blood group." });
                if (model.Quantity <= 0) return Json(new { success = false, message = "Quantity must be greater than 0." });
                if (model.ExpiryDate <= model.CollectionDate) return Json(new { success = false, message = "Expiry date must be after collection date." });
                if (model.ExpiryDate < DateTime.Today) return Json(new { success = false, message = "Expiry date cannot be in the past." });

                string status = model.Quantity == 0 ? "Critical" : model.Quantity <= 5 ? "Low Stock" : "Available";

                var inventory = new BloodInventory
                {
                    HospitalId = hospitalProfile.HospitalId,
                    BloodGroup = model.BloodGroup,
                    Quantity = model.Quantity,
                    CollectionDate = model.CollectionDate,
                    ExpiryDate = model.ExpiryDate,
                    Status = status,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.BloodInventory.Add(inventory);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Blood stock added successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBloodStock(BloodInventoryAddViewModel model)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId)) return Json(new { success = false, message = "User not authenticated." });

                var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
                if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });

                var inventory = await _context.BloodInventory.FirstOrDefaultAsync(i => i.InventoryId == model.InventoryId && i.HospitalId == hospitalProfile.HospitalId);
                if (inventory == null) return Json(new { success = false, message = "Inventory item not found." });

                if (inventory.Status == "LOW") inventory.Status = "Low Stock";
                if (model.Quantity < 0) return Json(new { success = false, message = "Quantity cannot be negative." });
                if (model.ExpiryDate < DateTime.Today) return Json(new { success = false, message = "Expiry date cannot be in the past." });

                inventory.Quantity = model.Quantity;
                inventory.ExpiryDate = model.ExpiryDate;
                inventory.UpdatedAt = DateTime.Now;
                inventory.Status = inventory.ExpiryDate < DateTime.Today ? "Expired" : model.Quantity == 0 ? "Critical" : model.Quantity <= 5 ? "Low Stock" : "Available";

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Blood stock updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBloodStock(int id)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId)) return Json(new { success = false, message = "User not authenticated." });

                var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
                if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });

                var inventory = await _context.BloodInventory.FirstOrDefaultAsync(i => i.InventoryId == id && i.HospitalId == hospitalProfile.HospitalId);
                if (inventory == null) return Json(new { success = false, message = "Inventory item not found." });

                _context.BloodInventory.Remove(inventory);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Blood stock deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        // ==========================================
        // DONOR RESPONSES
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> DonorResponses(DonorResponseFilters filters, int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return RedirectToAction("Login", "Account");

            var hospitalId = hospitalProfile.HospitalId;

            // ✅ FIXED: Added .ThenInclude(d => d.DonorProfile) to load BloodGroup
            var query = _context.DonorMatches
                .Include(dm => dm.Donor)
                    .ThenInclude(d => d.DonorProfile)  // ✅ YE ADD KAREIN
                .Include(dm => dm.BloodRequest)
                    .ThenInclude(br => br.Receiver)
                .Where(dm => dm.HospitalId == hospitalId);

            // Filtering
            if (!string.IsNullOrEmpty(filters.SearchQuery))
            {
                query = query.Where(dm =>
                    (dm.Donor != null && dm.Donor.FullName.Contains(filters.SearchQuery)) ||
                    (dm.BloodRequest != null && dm.BloodRequest.PatientName.Contains(filters.SearchQuery)) ||
                    dm.BloodRequestId.ToString().Contains(filters.SearchQuery));
            }

            if (filters.Status != "all" && !string.IsNullOrEmpty(filters.Status))
                query = query.Where(dm => dm.Status == filters.Status);

            if (filters.BloodGroup != "all" && !string.IsNullOrEmpty(filters.BloodGroup))
                query = query.Where(dm =>
                    dm.Donor != null &&
                    dm.Donor.DonorProfile != null &&
                    dm.Donor.DonorProfile.BloodGroup == filters.BloodGroup);

            // Sorting
            query = filters.SortBy switch
            {
                "oldest" => query.OrderBy(dm => dm.MatchDate),
                "donationdate" => query.OrderByDescending(dm => dm.DonationCompletedDate ?? DateTime.MaxValue),
                _ => query.OrderByDescending(dm => dm.MatchDate)
            };

            var totalCount = await query.CountAsync();
            var pageSize = 10;
            var currentPage = page < 1 ? 1 : page;
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var matches = await query.Skip((currentPage - 1) * pageSize).Take(pageSize).ToListAsync();

            var responses = matches.Select(dm =>
            {
                var (statusDisplay, statusBadgeClass) = dm.Status switch
                {
                    "Pending Response" => ("Pending Response", "badge-pending"),
                    "Accepted" => ("Accepted", "badge-accepted"),
                    "DonationScheduled" => ("Scheduled", "badge-scheduled"),
                    "Completed" => ("Completed", "badge-completed"),
                    "Rejected" => ("Rejected", "badge-rejected"),
                    "Expired" => ("Expired", "badge-expired"),
                    "Cancelled" => ("Cancelled", "badge-expired"),
                    _ => (dm.Status, "badge-expired")
                };

                return new DonorResponseItem
                {
                    MatchId = dm.MatchId,
                    DonorId = dm.DonorId,
                    RequestId = dm.BloodRequestId,
                    DonorName = dm.Donor?.FullName ?? "Unknown",
                    DonorProfilePicture = dm.Donor?.ProfilePicture ?? "/assets/img/avatars/DefaultAvatar.png",
                    // ✅ FIXED: Properly access BloodGroup from DonorProfile
                    DonorBloodGroup = dm.Donor?.DonorProfile?.BloodGroup ?? "Unknown",
                    DonorPhone = dm.Donor?.Phone ?? "",
                    DonorEmail = dm.Donor?.Email ?? "",
                    ReceiverName = dm.BloodRequest?.Receiver?.FullName ?? "Unknown",
                    PatientName = dm.BloodRequest?.PatientName ?? "Unknown",
                    BloodGroupRequired = dm.BloodRequest?.BloodGroup ?? "Unknown",
                    UnitsRequired = dm.BloodRequest?.UnitsRequired ?? 0,
                    HospitalName = dm.BloodRequest?.HospitalName ?? "Hospital",
                    AssignedDate = dm.MatchDate,
                    ResponseDate = dm.Status != "Pending Response" ? dm.MatchDate : null,
                    DonationScheduledDate = dm.DonationScheduledDate,
                    DonationCompletedDate = dm.DonationCompletedDate,
                    Status = dm.Status,
                    StatusDisplay = statusDisplay,
                    StatusBadgeClass = statusBadgeClass,
                    Notes = dm.Notes ?? "",
                    RejectionReason = dm.RejectionReason ?? "",
                    EmailSent = dm.EmailSent ?? false,
                    SmsSent = dm.SmsSent ?? false,
                    RequestUrgency = dm.BloodRequest?.UrgencyLevel ?? "Normal",
                    MatchScore = dm.MatchScore,
                    ResponseDeadline = dm.ResponseDeadline
                };
            }).ToList();

            var viewModel = new HospitalDonorResponsesViewModel
            {
                TotalResponses = totalCount,
                PendingResponses = matches.Count(m => m.Status == "Pending Response"),
                AcceptedDonors = matches.Count(m => m.Status == "Accepted" || m.Status == "DonationScheduled"),
                RejectedResponses = matches.Count(m => m.Status == "Rejected"),
                CompletedDonations = matches.Count(m => m.Status == "Completed"),
                ScheduledDonations = matches.Count(m => m.Status == "DonationScheduled"),
                Responses = responses,
                Filters = filters,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize
            };

            // ✅ FIXED: Populate blood groups from database
            viewModel.AvailableBloodGroups = await _context.DonorProfiles
                .Where(d => d.BloodGroup != null)
                .Select(d => d.BloodGroup)
                .Distinct()
                .OrderBy(bg => bg)
                .ToListAsync();

            viewModel.AvailableStatuses = new List<string>
    {
        "Pending Response",
        "Accepted",
        "DonationScheduled",
        "Completed",
        "Rejected",
        "Expired",
        "Cancelled"
    };

            ViewData["Title"] = "Donor Responses";
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleDonation(int id, DateTime scheduledDate)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });
            var hospitalId = hospitalProfile.HospitalId;

            var match = await _context.DonorMatches.FirstOrDefaultAsync(m => m.MatchId == id && m.HospitalId == hospitalId);
            if (match == null) return Json(new { success = false, message = "Assignment not found." });
            if (match.Status != "Accepted") return Json(new { success = false, message = "Donor must accept before scheduling." });

            match.Status = "DonationScheduled";
            match.DonationScheduledDate = scheduledDate;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Donation scheduled successfully!" });
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================

        // Helper method for GetHospitalCategoryStyle - returns tuple with explicit types
        private (string icon, string color) GetHospitalCategoryStyle(string category)
        {
            return category switch
            {
                "BloodRequest" => ("bi-clipboard2-pulse", "#90151C"),
                "Emergency" => ("bi-exclamation-triangle-fill", "#dc3545"),
                "DonorMatch" => ("bi-person-check", "#198754"),
                "Inventory" => ("bi-box-seam", "#0dcaf0"),
                "Assignment" => ("bi-person-badge", "#5C88A8"),
                "BloodExpiry" => ("bi-clock-history", "#ffc107"),
                "System" => ("bi-gear", "#6c757d"),
                "DonorResponse" => ("bi-chat-dots", "#5C88A8"),
                "BloodCollection" => ("bi-box-arrow-in-down", "#198754"),
                _ => ("bi-bell", "#5C88A8")
            };
        }

        // Helper method for GetTimeAgo (static version for JSON serialization)
        private static string GetTimeAgo(DateTime date)
        {
            var span = DateTime.Now - date;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return date.ToString("dd MMM yyyy");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDonationCompleted(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
                if (hospitalProfile == null)
                    return Json(new { success = false, message = "Hospital profile not found." });

                var match = await _context.DonorMatches
                    .Include(m => m.Donor).ThenInclude(d => d.DonorProfile)
                    .Include(m => m.BloodRequest).ThenInclude(br => br.Receiver)
                    .Include(m => m.Hospital)
                    .FirstOrDefaultAsync(m => m.MatchId == id && m.HospitalId == hospitalProfile.HospitalId);

                if (match == null)
                    return Json(new { success = false, message = "Match record not found." });

                if (match.Status != "Accepted" && match.Status != "DonationScheduled" && match.Status != "Scheduled")
                    return Json(new { success = false, message = $"Donor must be in 'Accepted' or 'Scheduled' status to complete. Current status: {match.Status}" });

                // 1. Update Match Status
                match.Status = "Completed";
                match.DonationCompletedDate = DateTime.Now;

                // 2. Update Blood Request Status
                if (match.BloodRequest != null)
                {
                    match.BloodRequest.RequestStatus = "Fulfilled";

                    // 3. Update Hospital Blood Inventory
                    int unitsToAdd = match.BloodRequest.UnitsRequired ?? 1;
                    var inventory = await _context.BloodInventory.FirstOrDefaultAsync(i =>
                        i.HospitalId == hospitalProfile.HospitalId &&
                        i.BloodGroup == match.BloodRequest.BloodGroup &&
                        i.Status != "Expired" && i.Status != "Used");

                    if (inventory != null)
                    {
                        inventory.Quantity += unitsToAdd;
                        inventory.UpdatedAt = DateTime.Now;
                        inventory.Status = inventory.Quantity == 0 ? "Critical" : inventory.Quantity <= 5 ? "Low Stock" : "Available";
                    }
                    else
                    {
                        _context.BloodInventory.Add(new BloodInventory
                        {
                            HospitalId = hospitalProfile.HospitalId,
                            BloodGroup = match.BloodRequest.BloodGroup,
                            Quantity = unitsToAdd,
                            CollectionDate = DateTime.Now,
                            ExpiryDate = DateTime.Now.AddDays(35),
                            Status = "Available",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        });
                    }
                }

                // 4. Update Donor Profile
                if (match.Donor?.DonorProfile != null)
                {
                    match.Donor.DonorProfile.LastDonationDate = DateTime.Now;
                    match.Donor.DonorProfile.SuccessfulDonations = (match.Donor.DonorProfile.SuccessfulDonations ?? 0) + 1;
                    match.Donor.DonorProfile.IsAvailable = false;
                }

                // ✅ 5. CREATE DONATION RECORD (This is the key fix!)
                if (match.DonorId > 0 && match.BloodRequest != null)
                {
                    var donation = new Donation
                    {
                        DonorId = match.DonorId,
                        BloodGroup = match.BloodRequest.BloodGroup ?? "Unknown",
                        HospitalName = match.Hospital?.HospitalName ?? "Unknown",
                        DonationDate = DateTime.Now,
                        Status = "Completed",
                        // ✅ REMOVED: DonationType line that was causing error
                    };
                    _context.Donations.Add(donation);
                }

                await _context.SaveChangesAsync();

                // 6. Send Completion Emails
                if (match.Donor != null && !string.IsNullOrEmpty(match.Donor.Email))
                {
                    try
                    {
                        await _emailService.SendEmergencyDonationCompletedToDonorAsync(
                            match.Donor.Email,
                            match.Donor.FullName ?? "Donor",
                            match.BloodRequestId
                        );
                    }
                    catch (Exception emailEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Donor email error: {emailEx.Message}");
                    }
                }

                if (match.BloodRequest?.Receiver != null && !string.IsNullOrEmpty(match.BloodRequest.Receiver.Email))
                {
                    try
                    {
                        await _emailService.SendEmergencyDonationCompletedToReceiverAsync(
                            match.BloodRequest.Receiver.Email,
                            match.BloodRequest.Receiver.FullName ?? "Receiver",
                            match.BloodRequestId,
                            match.Donor?.FullName ?? "A Donor"
                        );
                    }
                    catch (Exception emailEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Receiver email error: {emailEx.Message}");
                    }
                }

                return Json(new
                {
                    success = true,
                    message = "Donation marked as completed! Inventory updated, donor history saved, and all parties notified."
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MarkDonationCompleted ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return Json(new
                {
                    success = false,
                    message = $"Server error: {ex.Message}. Please check logs for details."
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendReminder(int id, string customMessage = "")
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var hospitalProfile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });

            var hospitalId = hospitalProfile.HospitalId;

            // Get the donor match with donor and request details
            var match = await _context.DonorMatches
                .Include(m => m.Donor)
                .Include(m => m.BloodRequest)
                .FirstOrDefaultAsync(m => m.MatchId == id && m.HospitalId == hospitalId);

            if (match == null)
                return Json(new { success = false, message = "Assignment not found." });

            if (match.Donor == null)
                return Json(new { success = false, message = "Donor information not found." });

            if (string.IsNullOrEmpty(match.Donor.Email))
                return Json(new { success = false, message = "Donor email address not available." });

            // Get request details
            var request = match.BloodRequest;
            string patientName = request?.PatientName ?? "Unknown Patient";
            string bloodGroup = request?.BloodGroup ?? "Unknown";
            string hospitalName = hospitalProfile.HospitalName ?? "Our Hospital";
            string status = match.Status;

            try
            {
                // ✅ Professional Email Send karein
                await _emailService.SendDonationReminderEmailAsync(
                    match.Donor.Email,
                    match.Donor.FullName ?? "Donor",
                    request?.RequestId ?? 0,
                    bloodGroup,
                    patientName,
                    hospitalName,
                    status,
                    customMessage
                );

                // Update match record to track email was sent
                match.EmailSent = true;
                await _context.SaveChangesAsync();

                // Create notification for hospital
                _context.HospitalNotifications.Add(new HospitalNotification
                {
                    HospitalId = hospitalId,
                    Title = "Reminder Sent",
                    Message = $"Reminder email sent to {match.Donor.FullName} for Request REQ-{request?.RequestId:D4}",
                    Category = "DonorResponse",
                    Priority = "Low",
                    CreatedDate = DateTime.Now,
                    DonorId = match.DonorId,
                    RequestId = request?.RequestId
                });

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Reminder email sent successfully to {match.Donor.Email}"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Failed to send email: {ex.Message}. Please check email configuration."
                });
            }
        }

        // Helper method for FormatTimeAgo
        private string FormatTimeAgo(DateTime date)
        {
            var span = DateTime.Now - date;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} hr ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} day(s) ago";
            return date.ToString("dd MMM yyyy");
        }
    } // <-- Ye HospitalController class band kar raha hai

    // ==========================================
    // Helper DTO for Backup Queue 
    // (Namespace ke andar, lekin Controller class ke BAHAR)
    // ==========================================
    public class BackupDonorDto
    {
        public string Name { get; set; } = "";
        public string BloodGroup { get; set; } = "";
        public int Score { get; set; }
        public double DistanceKm { get; set; }
    }

} // <-- Ye Namespace (Khoon_e_Hayat.Controllers) band kar raha hai