using Khoon_e_Hayat.Data;
using Khoon_e_Hayat.Models.Entities;
using Khoon_e_Hayat.ViewModels;
using Khoon_e_Hayat.Services;
using Khoon_e_Hayat.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Text;

namespace Khoon_e_Hayat.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmailService _emailService;
        private readonly IWhatsAppService _whatsAppService; // CHANGED: ISmsService -> IWhatsAppService
        private readonly IGoogleMapsService _googleMapsService;
        private readonly IHubContext<NotificationHub> _hubContext;

        // UPDATED CONSTRUCTOR
        public AdminController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IEmailService emailService,
            IWhatsAppService whatsAppService, // CHANGED
            IGoogleMapsService googleMapsService,
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _emailService = emailService;
            _whatsAppService = whatsAppService; // CHANGED
            _googleMapsService = googleMapsService;
            _hubContext = hubContext;
        }

        // ==================== DASHBOARD & VERIFICATION (UNCHANGED) ====================

        public async Task<IActionResult> Dashboard()
        {
            ViewData["Title"] = "Dashboard";
            var vm = new DashboardViewModel();

            vm.TotalUsers = await _context.Users.CountAsync();
            vm.TotalDonors = await _context.Users.CountAsync(u => u.Role == "Donor");
            vm.TotalReceivers = await _context.Users.CountAsync(u => u.Role == "Receiver");
            vm.TotalHospitals = await _context.Users.CountAsync(u => u.Role == "Hospital");
            vm.PendingHospitalApprovals = await _context.HospitalProfiles.CountAsync(h => h.VerificationStatus == "Pending");
            vm.ActiveBloodRequests = await _context.BloodRequests.CountAsync(r => r.RequestStatus == "Pending");
            vm.EmergencyRequests = await _context.BloodRequests.CountAsync(r => r.UrgencyLevel == "Critical");
            vm.ContactMessages = await _context.ContactMessages.CountAsync();

            vm.UserDistributionData = new List<ChartData>
            {
                new ChartData { Label = "Donors", Value = vm.TotalDonors, Color = "#0d6efd" },
                new ChartData { Label = "Receivers", Value = vm.TotalReceivers, Color = "#198754" },
                new ChartData { Label = "Hospitals", Value = vm.TotalHospitals, Color = "#ffc107" }
            };

            var statusCounts = await _context.BloodRequests.GroupBy(r => r.RequestStatus).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();
            var allStatuses = new[] { "Pending", "Approved", "Fulfilled", "Rejected" };
            vm.BloodRequestsByStatusData = allStatuses.Select(s => new ChartData
            {
                Label = s,
                Value = statusCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0,
                Color = s switch { "Pending" => "#ffc107", "Approved" => "#0dcaf0", "Fulfilled" => "#198754", "Rejected" => "#dc3545", _ => "#6c757d" }
            }).ToList();

            var bgCounts = await _context.BloodRequests.GroupBy(r => r.BloodGroup).Select(g => new { BG = g.Key, Count = g.Count() }).ToListAsync();
            var allBloodGroups = new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
            vm.BloodRequestsByBloodGroupData = allBloodGroups.Select(bg => new ChartData
            {
                Label = bg,
                Value = bgCounts.FirstOrDefault(x => x.BG == bg)?.Count ?? 0,
                Color = "#025f67"
            }).ToList();

            int currentYear = DateTime.Now.Year;

            var monthlyRegs = await _context.Users
                .Where(u => u.CreatedAt.Year == currentYear)
                .GroupBy(u => u.CreatedAt.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            vm.MonthlyRegistrationsData.Clear();

            for (int month = 1; month <= 12; month++)
            {
                var monthData = monthlyRegs.FirstOrDefault(m => m.Month == month);

                vm.MonthlyRegistrationsData.Add(new MonthlyRegistrationsData
                {
                    Month = new DateTime(currentYear, month, 1).ToString("MMM"),
                    Count = monthData?.Count ?? 0
                });
            }

            vm.RecentBloodRequests = await (from r in _context.BloodRequests
                                            join u in _context.Users on r.ReceiverId equals u.UserId
                                            orderby r.CreatedAt descending
                                            select new RecentBloodRequestItem
                                            {
                                                RequestId = r.RequestId,
                                                ReceiverName = u.FullName,
                                                BloodGroup = r.BloodGroup,
                                                UnitsRequired = r.UnitsRequired ?? 0,
                                                HospitalName = r.HospitalName,
                                                City = r.City,
                                                UrgencyLevel = r.UrgencyLevel,
                                                Status = r.RequestStatus,
                                                CreatedDate = r.CreatedAt
                                            }).Take(5).ToListAsync();

            var recentUsersList = await (from u in _context.Users
                                         orderby u.CreatedAt descending
                                         select new
                                         {
                                             u.UserId,
                                             FullName = u.FullName ?? "Unknown",
                                             Email = u.Email ?? "N/A",
                                             Role = u.Role ?? "Unknown",
                                             u.IsActive,
                                             u.CreatedAt
                                         })
                                         .Take(5)
                                         .ToListAsync();

            var userIds = recentUsersList.Select(u => u.UserId).ToList();

            var donorProfiles = await _context.DonorProfiles.Where(d => userIds.Contains(d.UserId)).Select(d => new { d.UserId, City = d.City ?? "N/A" }).ToDictionaryAsync(d => d.UserId, d => d.City);
            var receiverProfiles = await _context.ReceiverProfiles.Where(r => userIds.Contains(r.UserId)).Select(r => new { r.UserId, City = r.City ?? "N/A" }).ToDictionaryAsync(r => r.UserId, r => r.City);
            var hospitalProfiles = await _context.HospitalProfiles.Where(h => userIds.Contains(h.UserId)).Select(h => new { h.UserId, City = h.City ?? "N/A" }).ToDictionaryAsync(h => h.UserId, h => h.City);

            foreach (var u in recentUsersList)
            {
                string city = "N/A";
                if (u.Role == "Donor") donorProfiles.TryGetValue(u.UserId, out city);
                else if (u.Role == "Receiver") receiverProfiles.TryGetValue(u.UserId, out city);
                else if (u.Role == "Hospital") hospitalProfiles.TryGetValue(u.UserId, out city);

                vm.RecentUsers.Add(new RecentUserItem
                {
                    UserId = u.UserId,
                    Name = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    City = city ?? "N/A",
                    Status = u.IsActive ? "Active" : "Inactive",
                    RegistrationDate = u.CreatedAt
                });
            }

            vm.PendingHospitals = await (from h in _context.HospitalProfiles join u in _context.Users on h.UserId equals u.UserId where h.VerificationStatus == "Pending" orderby u.CreatedAt descending select new PendingHospitalItem { HospitalId = h.HospitalId, UserId = h.UserId, HospitalName = h.HospitalName, LicenseNumber = h.LicenseNumber, City = h.City, ContactPerson = h.ContactPerson, RegistrationDate = u.CreatedAt }).Take(5).ToListAsync();
            vm.RecentContactMessages = await _context.ContactMessages.OrderByDescending(m => m.CreatedAt).Take(5).Select(m => new RecentContactMessageItem { MessageId = m.MessageId, Name = m.FullName, Email = m.Email, Subject = m.Subject, Status = m.Status, Date = m.CreatedAt }).ToListAsync();

            ViewBag.AdminName = User.FindFirstValue(ClaimTypes.Name) ?? "System Admin";
            ViewBag.AdminEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";
            ViewBag.AdminRole = User.FindFirstValue(ClaimTypes.Role) ?? "Admin";
            ViewBag.ProfilePicture = User.FindFirstValue("ProfilePicture") ?? "default.png";
            ViewBag.UnreadMessageCount = await _context.ContactMessages.CountAsync(m => m.Status == "New");

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> HospitalVerification()
        {
            var requests = await _context.Users.Where(u => u.Role == "Hospital").Join(_context.HospitalProfiles, u => u.UserId, h => h.UserId, (u, h) => new HospitalVerificationViewModel { UserId = u.UserId, HospitalName = h.HospitalName, Email = u.Email, LicenseNumber = h.LicenseNumber, ContactPerson = h.ContactPerson, City = h.City, SubmittedDate = u.CreatedAt, VerificationStatus = h.VerificationStatus ?? "Pending" }).OrderByDescending(r => r.SubmittedDate).ToListAsync();
            var today = DateTime.Today;
            var viewModel = new HospitalVerificationListViewModel { Requests = requests, TotalCount = requests.Count, PendingCount = requests.Count(r => r.VerificationStatus == "Pending"), ApprovedCount = requests.Count(r => r.VerificationStatus == "Approved"), RejectedCount = requests.Count(r => r.VerificationStatus == "Rejected"), TodayCount = requests.Count(r => r.SubmittedDate.Date == today), AvailableCities = requests.Where(r => !string.IsNullOrEmpty(r.City)).Select(r => r.City).Distinct().OrderBy(c => c).ToList() };
            ViewBag.UnreadMessageCount = await _context.ContactMessages.CountAsync(m => m.Status == "New");
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetHospitalVerificationDetails(int userId)
        {
            var details = await _context.Users.Where(u => u.UserId == userId && u.Role == "Hospital").Join(_context.HospitalProfiles, u => u.UserId, h => h.UserId, (u, h) => new HospitalVerificationDetailsViewModel { UserId = u.UserId, HospitalName = h.HospitalName, Email = u.Email, Phone = u.Phone, LicenseNumber = h.LicenseNumber, ContactPerson = h.ContactPerson, Address = h.Address, City = h.City, SubmittedDate = u.CreatedAt, VerificationStatus = h.VerificationStatus ?? "Pending", LicenseDocumentPath = h.LicenseDocumentPath, RejectionReason = h.RejectionReason }).FirstOrDefaultAsync();
            if (details == null) return NotFound();
            return Json(details);
        }

        [HttpGet]
        public async Task<IActionResult> ViewLicense(int userId)
        {
            var profile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (profile == null || string.IsNullOrEmpty(profile.LicenseDocumentPath)) return NotFound();
            var relativePath = profile.LicenseDocumentPath.TrimStart('/', '\\');
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
            if (!System.IO.File.Exists(filePath)) return NotFound();
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var contentType = extension switch { ".pdf" => "application/pdf", ".jpg" => "image/jpeg", ".jpeg" => "image/jpeg", ".png" => "image/png", ".gif" => "image/gif", _ => "application/octet-stream" };
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, contentType);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadLicense(int userId)
        {
            var profile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
            if (profile == null || string.IsNullOrEmpty(profile.LicenseDocumentPath)) return NotFound();
            var relativePath = profile.LicenseDocumentPath.TrimStart('/', '\\');
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
            if (!System.IO.File.Exists(filePath)) return NotFound();
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var contentType = extension switch { ".pdf" => "application/pdf", ".jpg" => "image/jpeg", ".jpeg" => "image/jpeg", ".png" => "image/png", ".gif" => "image/gif", _ => "application/octet-stream" };
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, contentType, $"{profile.HospitalName}_License{extension}");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveHospital(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                var profile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
                if (user == null || profile == null || user.Role != "Hospital") return Json(new { success = false, message = "Hospital not found" });
                profile.VerificationStatus = "Approved"; profile.RejectionReason = null;
                user.IsApproved = true; user.IsActive = true; user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                await LogAdminAction($"Approved Hospital: {user.Email}");
                try { await _emailService.SendHospitalApprovalEmailAsync(user.Email, profile.HospitalName); } catch { }
                return Json(new { success = true, message = "Hospital approved successfully." });
            }
            catch (Exception ex) { return Json(new { success = false, message = $"Error: {ex.Message}" }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectHospital(int userId, string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason)) return Json(new { success = false, message = "Rejection reason is required" });
                var user = await _context.Users.FindAsync(userId);
                var profile = await _context.HospitalProfiles.FirstOrDefaultAsync(h => h.UserId == userId);
                if (user == null || profile == null || user.Role != "Hospital") return Json(new { success = false, message = "Hospital not found" });
                profile.VerificationStatus = "Rejected"; profile.RejectionReason = reason;
                user.IsApproved = false; user.IsActive = false; user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                await LogAdminAction($"Rejected Hospital: {user.Email}. Reason: {reason}");
                try { await _emailService.SendHospitalRejectionEmailAsync(user.Email, profile.HospitalName, reason); } catch { }
                return Json(new { success = true, message = "Hospital rejected successfully." });
            }
            catch (Exception ex) { return Json(new { success = false, message = $"Error: {ex.Message}" }); }
        }

        [HttpGet]
        public async Task<IActionResult> ExportVerificationExcel()
        {
            var data = await _context.Users.Where(u => u.Role == "Hospital").Join(_context.HospitalProfiles, u => u.UserId, h => h.UserId, (u, h) => new HospitalVerificationViewModel { UserId = u.UserId, HospitalName = h.HospitalName, Email = u.Email, LicenseNumber = h.LicenseNumber, ContactPerson = h.ContactPerson, City = h.City, SubmittedDate = u.CreatedAt, VerificationStatus = h.VerificationStatus ?? "Pending" }).OrderByDescending(r => r.SubmittedDate).ToListAsync();
            var csv = new StringBuilder(); csv.AppendLine("Request ID,Hospital Name,Email,License Number,Contact Person,City,Submitted Date,Status");
            foreach (var r in data) csv.AppendLine($"VER-{r.UserId.ToString().PadLeft(4, '0')},{r.HospitalName},{r.Email},{r.LicenseNumber},{r.ContactPerson},{r.City},{r.SubmittedDate:dd-MMM-yyyy},{r.VerificationStatus}");
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "HospitalVerification.csv");
        }

        // ==========================================
        // ADMIN: BLOOD BANK OVERVIEW
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> BloodBankOverview()
        {
            var totalHospitals = await _context.HospitalProfiles.CountAsync();
            var allInventory = await _context.BloodInventory.ToListAsync();

            var totalBloodUnits = allInventory.Where(i => i.Status != "Expired" && i.Status != "Used").Sum(i => i.Quantity);
            var totalRequests = await _context.BloodRequests.CountAsync();
            var emergencyRequests = await _context.BloodRequests.CountAsync(r => r.UrgencyLevel == "Critical" && r.RequestStatus == "Pending");

            var lowStockHospitals = await _context.HospitalProfiles.CountAsync(h =>
                _context.BloodInventory.Any(i => i.HospitalId == h.HospitalId && (i.Status == "Low Stock" || i.Status == "Critical" || i.Status == "LOW"))
            );

            var expiredUnits = allInventory.Where(i => i.Status == "Expired" || i.ExpiryDate < DateTime.Today).Sum(i => i.Quantity);

            // Blood Group Distribution for Chart
            var bloodGroups = new[] { "A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-" };
            var bgDistribution = new List<ChartData>();
            var colors = new[] { "#0d6efd", "#198754", "#ffc107", "#dc3545", "#0dcaf0", "#6f42c1", "#fd7e14", "#20c997" };
            int colorIdx = 0;

            foreach (var bg in bloodGroups)
            {
                var units = allInventory.Where(i => i.BloodGroup == bg && i.Status != "Expired" && i.Status != "Used").Sum(i => i.Quantity);
                bgDistribution.Add(new ChartData { Label = bg, Value = units, Color = colors[colorIdx++] });
            }

            // Hospital Stock Summary (Top 5 by Total Units)
            var hospitalStocks = await _context.HospitalProfiles
                .Select(h => new HospitalStockSummary
                {
                    HospitalName = h.HospitalName,
                    TotalUnits = _context.BloodInventory.Where(i => i.HospitalId == h.HospitalId && i.Status != "Expired" && i.Status != "Used").Sum(i => i.Quantity),
                    LowStockCount = _context.BloodInventory.Count(i => i.HospitalId == h.HospitalId && (i.Status == "Low Stock" || i.Status == "Critical" || i.Status == "LOW"))
                })
                .OrderByDescending(h => h.TotalUnits)
                .Take(5)
                .ToListAsync();

            foreach (var h in hospitalStocks)
            {
                if (h.LowStockCount > 2) h.Status = "Critical";
                else if (h.LowStockCount > 0) h.Status = "Warning";
                else h.Status = "Healthy";
            }

            // Recent Activities
            var recentActivities = new List<RecentActivityItem>();
            var recentIssues = await _context.BloodIssueHistory.OrderByDescending(i => i.IssueDate).Take(3).ToListAsync();
            foreach (var issue in recentIssues)
            {
                recentActivities.Add(new RecentActivityItem
                {
                    Action = $"{issue.UnitsIssued} units of {issue.BloodGroup} issued by {issue.HospitalName}",
                    TimeAgo = GetTimeAgo(issue.IssueDate),
                    Icon = "bi-box-arrow-up-right",
                    Color = "#5C88A8"
                });
            }

            var recentCollections = await _context.BloodInventory
                .Where(i => i.Status != "Expired" && i.Status != "Used")
                .OrderByDescending(i => i.CollectionDate)
                .Take(2)
                .ToListAsync();

            foreach (var coll in recentCollections)
            {
                var hospName = await _context.HospitalProfiles.Where(h => h.HospitalId == coll.HospitalId).Select(h => h.HospitalName).FirstOrDefaultAsync() ?? "Unknown Hospital";
                recentActivities.Add(new RecentActivityItem
                {
                    Action = $"{coll.Quantity} units of {coll.BloodGroup} collected at {hospName}",
                    TimeAgo = GetTimeAgo(coll.CollectionDate),
                    Icon = "bi-box-arrow-in-down",
                    Color = "#198754"
                });
            }

            var viewModel = new BloodBankOverviewViewModel
            {
                TotalHospitals = totalHospitals,
                TotalBloodUnits = totalBloodUnits,
                TotalRequests = totalRequests,
                EmergencyRequests = emergencyRequests,
                LowStockHospitals = lowStockHospitals,
                ExpiredUnits = expiredUnits,
                BloodGroupDistribution = bgDistribution,
                HospitalStocks = hospitalStocks,
                RecentActivities = recentActivities.OrderByDescending(a => a.TimeAgo).Take(5).ToList()
            };

            ViewData["Title"] = "Blood Bank Overview";
            return View(viewModel);
        }

        // ==========================================
        // ADMIN: BLOOD BANK INVENTORY MANAGEMENT
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> BloodBankInventory(InventoryFilters filters, int page = 1)
        {
            var query = _context.BloodInventory.Include(i => i.Hospital).AsQueryable();

            if (!string.IsNullOrEmpty(filters.SearchQuery))
            {
                query = query.Where(i =>
                    i.BloodGroup.Contains(filters.SearchQuery) ||
                    i.Status.Contains(filters.SearchQuery) ||
                    (i.Hospital != null && i.Hospital.HospitalName.Contains(filters.SearchQuery)));
            }

            if (filters.BloodGroup != "all" && !string.IsNullOrEmpty(filters.BloodGroup))
            {
                query = query.Where(i => i.BloodGroup == filters.BloodGroup);
            }

            if (filters.Status != "all" && !string.IsNullOrEmpty(filters.Status))
            {
                var statusFilter = filters.Status == "Low Stock" ? "Low Stock" : filters.Status;
                query = query.Where(i => i.Status == statusFilter || (i.Status == "LOW" && statusFilter == "Low Stock"));
            }

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

            foreach (var item in inventory)
            {
                if (item.Status == "LOW") item.Status = "Low Stock";
            }

            var allInventory = await _context.BloodInventory.ToListAsync();
            var today = DateTime.Today;

            var viewModel = new BloodInventoryViewModel
            {
                TotalUnits = allInventory.Sum(i => i.Quantity),
                AvailableUnits = allInventory.Where(i => i.Status == "Available").Sum(i => i.Quantity),
                ExpiredUnits = allInventory.Where(i => i.Status == "Expired" || i.ExpiryDate < today).Sum(i => i.Quantity),
                LowStockUnits = allInventory.Where(i => i.Status == "Low Stock" || i.Status == "LOW").Sum(i => i.Quantity),
                OutOfStockUnits = allInventory.Count(i => i.Quantity == 0),
                ExpiringSoonUnits = allInventory.Where(i => i.ExpiryDate <= today.AddDays(7) && i.ExpiryDate >= today && i.Status != "Expired").Sum(i => i.Quantity),
                AvailableBloodGroups = allInventory.Where(i => i.Quantity > 0).Select(i => i.BloodGroup).Distinct().Count(),
                AvailableHospitals = await _context.HospitalProfiles.ToListAsync(),
                Inventory = inventory,
                Filters = filters,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize
            };

            ViewData["Title"] = "Blood Bank Inventory";
            return View(viewModel);
        }
 
        // ==========================================
        // ADMIN: BLOOD COLLECTION LOG
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> BloodCollectionLog(CollectionFilters filters, int page = 1)
        {
            // ✅ Admin sees ALL collections across ALL hospitals
            var query = _context.BloodInventory.Include(i => i.Hospital).AsQueryable();

            // 1. Apply Filters
            if (!string.IsNullOrEmpty(filters.SearchQuery))
            {
                query = query.Where(i =>
                    i.BloodGroup.Contains(filters.SearchQuery) ||
                    i.Status.Contains(filters.SearchQuery) ||
                    (i.Hospital != null && i.Hospital.HospitalName.Contains(filters.SearchQuery)));
            }

            if (filters.BloodGroup != "all" && !string.IsNullOrEmpty(filters.BloodGroup))
            {
                query = query.Where(i => i.BloodGroup == filters.BloodGroup);
            }

            if (filters.Status != "all" && !string.IsNullOrEmpty(filters.Status))
            {
                var statusFilter = filters.Status == "Low Stock" ? "Low Stock" : filters.Status;
                query = query.Where(i => i.Status == statusFilter || (i.Status == "LOW" && statusFilter == "Low Stock"));
            }

            if (filters.DateFrom.HasValue) query = query.Where(i => i.CollectionDate >= filters.DateFrom.Value);
            if (filters.DateTo.HasValue) query = query.Where(i => i.CollectionDate <= filters.DateTo.Value);

            // 2. Apply Sorting
            query = filters.SortBy switch
            {
                "oldest" => query.OrderBy(i => i.CollectionDate),
                "bloodgroup" => query.OrderBy(i => i.BloodGroup),
                "units" => query.OrderByDescending(i => i.Quantity),
                _ => query.OrderByDescending(i => i.CollectionDate)
            };

            // 3. Pagination Setup
            var totalCount = await query.CountAsync();
            var pageSize = 10;
            var collections = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // 4. Calculate System-Wide Statistics
            var allCollections = await _context.BloodInventory.ToListAsync();
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

            ViewData["Title"] = "Blood Collection Log";
            return View(viewModel);
        }

        // ==========================================
        // ADMIN: BLOOD ISSUE LOG
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> BloodIssueLog(IssueHistoryFilters filters, int page = 1)
        {
            // ✅ Admin sees ALL issue history across ALL hospitals
            var query = _context.BloodIssueHistory.AsQueryable();

            // 1. Apply Filters
            if (!string.IsNullOrEmpty(filters.SearchQuery))
            {
                query = query.Where(i =>
                    i.BloodGroup.Contains(filters.SearchQuery) ||
                    i.HospitalName.Contains(filters.SearchQuery) ||
                    i.IssuedBy.Contains(filters.SearchQuery) ||
                    i.IssueId.ToString().Contains(filters.SearchQuery));
            }

            if (filters.BloodGroup != "all" && !string.IsNullOrEmpty(filters.BloodGroup))
            {
                query = query.Where(i => i.BloodGroup == filters.BloodGroup);
            }

            if (filters.DateFrom.HasValue) query = query.Where(i => i.IssueDate >= filters.DateFrom.Value);
            if (filters.DateTo.HasValue) query = query.Where(i => i.IssueDate <= filters.DateTo.Value);

            // 2. Apply Sorting
            query = filters.SortBy switch
            {
                "oldest" => query.OrderBy(i => i.IssueDate),
                "bloodgroup" => query.OrderBy(i => i.BloodGroup),
                "units" => query.OrderByDescending(i => i.UnitsIssued),
                _ => query.OrderByDescending(i => i.IssueDate)
            };

            // 3. Pagination Setup
            var totalCount = await query.CountAsync();
            var pageSize = 10;
            var issues = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var issueItems = issues.Select(i => new BloodIssueHistoryItem
            {
                IssueId = i.IssueId,
                RequestId = null, // Not directly linked in entity
                ReceiverName = "N/A",
                BloodGroup = i.BloodGroup,
                UnitsIssued = i.UnitsIssued,
                IssueDate = i.IssueDate,
                IssuedBy = i.IssuedBy,
                HospitalName = i.HospitalName,
                Status = i.Status,
                Notes = i.Notes
            }).ToList();

            // 4. Calculate System-Wide Statistics
            var allIssues = await _context.BloodIssueHistory.ToListAsync();

            var viewModel = new BloodIssueHistoryViewModel
            {
                TotalIssues = allIssues.Count,
                TotalUnitsIssued = allIssues.Sum(i => i.UnitsIssued),
                TodayIssues = allIssues.Count(i => i.IssueDate.Date == DateTime.Today),
                MonthIssues = allIssues.Count(i => i.IssueDate.Month == DateTime.Today.Month && i.IssueDate.Year == DateTime.Today.Year),
                SuccessfulDeliveries = allIssues.Count(i => i.Status == "Completed" || i.Status == "Delivered"),
                MostIssuedBloodGroup = allIssues.GroupBy(i => i.BloodGroup).OrderByDescending(g => g.Sum(i => i.UnitsIssued)).FirstOrDefault()?.Key ?? "N/A",
                ThisWeekIssues = allIssues.Count(i => i.IssueDate >= DateTime.Today.AddDays(-7)),
                Issues = issueItems,
                Filters = filters,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                TotalCount = totalCount,
                PageSize = pageSize
            };

            ViewData["Title"] = "Blood Issue Log";
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBloodStock(BloodInventoryAddViewModel model)
        {
            try
            {
                // ✅ Admin-specific validation: Must select a hospital
                if (model.HospitalId <= 0)
                    return Json(new { success = false, message = "Please select a hospital." });

                if (string.IsNullOrEmpty(model.BloodGroup))
                    return Json(new { success = false, message = "Please select a blood group." });

                if (model.Quantity <= 0)
                    return Json(new { success = false, message = "Quantity must be greater than 0." });

                if (model.ExpiryDate <= model.CollectionDate)
                    return Json(new { success = false, message = "Expiry date must be after collection date." });

                if (model.ExpiryDate < DateTime.Today)
                    return Json(new { success = false, message = "Expiry date cannot be in the past." });

                // Determine initial status
                string status = model.Quantity == 0 ? "Critical" : model.Quantity <= 5 ? "Low Stock" : "Available";

                var inventory = new BloodInventory
                {
                    HospitalId = model.HospitalId,
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

                await LogAdminAction($"Added {model.Quantity} units of {model.BloodGroup} to Hospital ID: {model.HospitalId}");

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
                var inventory = await _context.BloodInventory.FirstOrDefaultAsync(i => i.InventoryId == model.InventoryId);
                if (inventory == null)
                    return Json(new { success = false, message = "Inventory item not found." });

                if (inventory.Status == "LOW") inventory.Status = "Low Stock";

                if (model.Quantity < 0)
                    return Json(new { success = false, message = "Quantity cannot be negative." });

                if (model.ExpiryDate < DateTime.Today)
                    return Json(new { success = false, message = "Expiry date cannot be in the past." });

                // Update fields
                inventory.Quantity = model.Quantity;
                inventory.ExpiryDate = model.ExpiryDate;
                inventory.UpdatedAt = DateTime.Now;

                // Recalculate status based on new values
                inventory.Status = inventory.ExpiryDate < DateTime.Today ? "Expired" :
                                   model.Quantity == 0 ? "Critical" :
                                   model.Quantity <= 5 ? "Low Stock" : "Available";

                await _context.SaveChangesAsync();

                await LogAdminAction($"Edited Blood Stock ID: {model.InventoryId}. New Quantity: {model.Quantity}");

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
                var inventory = await _context.BloodInventory.FirstOrDefaultAsync(i => i.InventoryId == id);
                if (inventory == null)
                    return Json(new { success = false, message = "Inventory item not found." });

                _context.BloodInventory.Remove(inventory);
                await _context.SaveChangesAsync();

                await LogAdminAction($"Deleted Blood Stock ID: {id} (Blood Group: {inventory.BloodGroup}, Qty: {inventory.Quantity})");

                return Json(new { success = true, message = "Blood stock deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        // ==================== BLOOD REQUESTS (UNCHANGED) ====================

        [HttpGet]
        public async Task<IActionResult> BloodRequests()
        {
            var requestsQuery = _context.BloodRequests.Join(_context.Users, br => br.ReceiverId, u => u.UserId, (br, u) => new BloodRequestViewModel
            {
                RequestId = br.RequestId,
                ReceiverId = br.ReceiverId,
                ReceiverName = u.FullName,
                ReceiverEmail = u.Email,
                ReceiverPhone = u.Phone,
                BloodGroup = br.BloodGroup,
                UnitsRequired = br.UnitsRequired ?? 0,
                HospitalName = br.HospitalName,
                City = br.City,
                UrgencyLevel = br.UrgencyLevel,
                RequestStatus = br.RequestStatus,
                CreatedDate = br.CreatedAt,
                RequiredDate = br.RequiredDate
            });

            var requests = await requestsQuery.OrderByDescending(r => r.CreatedDate).ToListAsync();

            var viewModel = new BloodRequestListViewModel
            {
                Requests = requests,
                TotalCount = requests.Count,
                PendingCount = requests.Count(r => r.RequestStatus == "Pending"),
                // ✅ Count both Fulfilled and Completed
                FulfilledCount = requests.Count(r => r.RequestStatus == "Fulfilled" || r.RequestStatus == "Completed"),
                EmergencyCount = requests.Count(r => r.UrgencyLevel == "Critical"),
                ActiveCitiesCount = requests.Where(r => !string.IsNullOrEmpty(r.City)).Select(r => r.City).Distinct().Count(),
                AvailableCities = requests.Where(r => !string.IsNullOrEmpty(r.City)).Select(r => r.City).Distinct().OrderBy(c => c).ToList()
            };

            ViewBag.UnreadMessageCount = await _context.ContactMessages.CountAsync(m => m.Status == "New");
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetRequestDetails(int requestId)
        {
            var details = await _context.BloodRequests.Where(br => br.RequestId == requestId).Join(_context.Users, br => br.ReceiverId, u => u.UserId, (br, u) => new BloodRequestDetailsViewModel { RequestId = br.RequestId, ReceiverName = u.FullName, ReceiverEmail = u.Email, ReceiverPhone = u.Phone, BloodGroup = br.BloodGroup, UnitsRequired = br.UnitsRequired ?? 0, UrgencyLevel = br.UrgencyLevel, RequiredDate = br.RequiredDate, HospitalName = br.HospitalName, City = br.City, RequestStatus = br.RequestStatus, CreatedDate = br.CreatedAt }).FirstOrDefaultAsync();
            if (details == null) return NotFound();
            return Json(details);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRequestStatus(int requestId, string status)
        {
            try
            {
                var request = await _context.BloodRequests
                    .Include(r => r.Receiver)
                    .FirstOrDefaultAsync(r => r.RequestId == requestId);

                if (request == null)
                    return Json(new { success = false, message = "Request not found" });

                var oldStatus = request.RequestStatus;

                // ✅ UPDATED: Allow all valid status transitions
                bool isValid = (oldStatus == "Pending" && (status == "Matched" || status == "Fulfilled" || status == "Cancelled" || status == "Approved"))
                            || (oldStatus == "Matched" && (status == "Fulfilled" || status == "Completed"))
                            || (oldStatus == "Approved" && (status == "Completed" || status == "Blood Issued"))
                            || (oldStatus == "Blood Issued" && status == "Completed");

                if (!isValid)
                    return Json(new { success = false, message = "Invalid status transition." });

                request.RequestStatus = status;

                // ============ AUTO-CREATE NOTIFICATION FOR RECEIVER ============
                var notificationData = GetNotificationForStatus(status, request);

                if (notificationData.HasValue)
                {
                    var notification = new ReceiverNotification
                    {
                        ReceiverId = request.ReceiverId,
                        RequestId = requestId,
                        Title = notificationData.Value.Title,
                        Message = notificationData.Value.Message,
                        Category = notificationData.Value.Category,
                        Priority = notificationData.Value.Priority,
                        HospitalName = request.HospitalName,
                        BloodGroup = request.BloodGroup,
                        RequestStatus = status,
                        IsRead = false,
                        CreatedDate = DateTime.Now
                    };

                    _context.ReceiverNotifications.Add(notification);
                }

                await _context.SaveChangesAsync();
                await LogAdminAction($"Updated Blood Request REQ-{requestId:D4} status from {oldStatus} to {status}");

                return Json(new { success = true, message = $"Request status updated to {status} successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // ============ HELPER: Get Notification Data Based on Status ============
        private (string Title, string Message, string Category, string Priority)? GetNotificationForStatus(string status, BloodRequest request)
        {
            if (string.IsNullOrEmpty(status))
                return null;

            return status.ToLower() switch
            {
                "matched" => (
                    "Donor Matched",
                    $"A compatible donor has been found for your blood request REQ-{request.RequestId:D4}.",
                    "DonorMatch",
                    "High"
                ),
                "approved" => (
                    "Request Approved",
                    $"Your blood request REQ-{request.RequestId:D4} has been approved by the hospital.",
                    "BloodRequest",
                    "High"
                ),
                "blood issued" => (
                    "Blood Issued",
                    $"Blood has been issued for your request REQ-{request.RequestId:D4}. Please collect it from the hospital.",
                    "BloodRequest",
                    "High"
                ),
                "fulfilled" or "completed" => (
                    "Request Completed",
                    $"Your blood request REQ-{request.RequestId:D4} has been successfully fulfilled. Thank you!",
                    "BloodRequest",
                    "High"
                ),
                "cancelled" => (
                    "Request Cancelled",
                    $"Your blood request REQ-{request.RequestId:D4} has been cancelled.",
                    "BloodRequest",
                    "Medium"
                ),
                _ => null
            };
        }

        // ==================== EMERGENCY ALERTS (UNCHANGED) ====================

        [HttpGet]
        public async Task<IActionResult> EmergencyAlerts()
        {
            var alertsQuery = _context.EmergencyAlerts.Join(_context.BloodRequests, ea => ea.RequestId, br => br.RequestId, (ea, br) => new { ea, br }).Join(_context.Users, x => x.br.ReceiverId, u => u.UserId, (x, u) => new { x.ea, x.br, u }).Select(x => new EmergencyAlertViewModel { AlertId = x.ea.AlertId, RequestId = x.br.RequestId, ReceiverName = x.u.FullName, BloodGroup = x.br.BloodGroup, HospitalName = x.br.HospitalName, City = x.br.City, AlertMessage = x.ea.AlertMessage, PriorityLevel = x.ea.PriorityLevel, AlertStatus = x.ea.AlertStatus, CreatedDate = x.ea.CreatedAt });
            var alerts = await alertsQuery.OrderByDescending(a => a.CreatedDate).ToListAsync();
            var today = DateTime.Today;
            var viewModel = new EmergencyAlertListViewModel { Alerts = alerts, TotalCount = alerts.Count, ActiveCount = alerts.Count(a => a.AlertStatus == "Active"), ResolvedCount = alerts.Count(a => a.AlertStatus == "Resolved"), CriticalCount = alerts.Count(a => a.PriorityLevel == "Critical"), HospitalAlertsCount = alerts.Count(a => !string.IsNullOrEmpty(a.HospitalName)), TodayCount = alerts.Count(a => a.CreatedDate.Date == today), AvailableCities = alerts.Where(a => !string.IsNullOrEmpty(a.City)).Select(a => a.City).Distinct().OrderBy(c => c).ToList() };
            ViewBag.UnreadMessageCount = await _context.ContactMessages.CountAsync(m => m.Status == "New");
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetAlertDetails(int alertId)
        {
            var details = await _context.EmergencyAlerts.Where(ea => ea.AlertId == alertId).Join(_context.BloodRequests, ea => ea.RequestId, br => br.RequestId, (ea, br) => new { ea, br }).Join(_context.Users, x => x.br.ReceiverId, u => u.UserId, (x, u) => new EmergencyAlertDetailsViewModel { AlertId = x.ea.AlertId, AlertMessage = x.ea.AlertMessage, PriorityLevel = x.ea.PriorityLevel, AlertStatus = x.ea.AlertStatus, CreatedDate = x.ea.CreatedAt, ReceiverName = u.FullName, ReceiverEmail = u.Email, ReceiverPhone = u.Phone, BloodGroup = x.br.BloodGroup, UnitsRequired = x.br.UnitsRequired ?? 0, UrgencyLevel = x.br.UrgencyLevel, RequiredDate = x.br.RequiredDate, HospitalName = x.br.HospitalName, City = x.br.City, RequestId = x.br.RequestId, RequestStatus = x.br.RequestStatus, RequestCreatedDate = x.br.CreatedAt }).FirstOrDefaultAsync();
            if (details == null) return NotFound();
            return Json(details);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FindMatchingDonors(int alertId)
        {
            try
            {
                var alertData = await _context.EmergencyAlerts.Where(ea => ea.AlertId == alertId).Join(_context.BloodRequests, ea => ea.RequestId, br => br.RequestId, (ea, br) => new { ea, br }).FirstOrDefaultAsync();
                if (alertData == null) return Json(new { success = false, message = "Alert or associated blood request not found." });
                var bloodGroup = alertData.br.BloodGroup; var city = alertData.br.City;
                if (string.IsNullOrEmpty(bloodGroup) || string.IsNullOrEmpty(city)) return Json(new { success = false, message = "Blood group or city information is missing." });
                var matchingDonors = await _context.DonorProfiles.Where(d => d.BloodGroup == bloodGroup && d.City == city && d.IsAvailable == true).Join(_context.Users, d => d.UserId, u => u.UserId, (d, u) => new { d, u }).Where(x => x.u.IsActive && x.u.IsApproved && x.u.Role == "Donor").Select(x => new { x.u.UserId, x.u.FullName, x.u.Email, x.u.Phone, x.d.BloodGroup, x.d.City, x.d.IsAvailable }).ToListAsync();
                var donorList = matchingDonors.Select(d => new { donorId = d.UserId, fullName = d.FullName, email = d.Email, phone = d.Phone, bloodGroup = d.BloodGroup, city = d.City, availability = d.IsAvailable ? "Available" : "Unavailable" }).ToList();
                await LogAdminAction($"Triggered Smart Matching for Emergency Alert ALT-{alertId:D4}. Found {matchingDonors.Count} eligible donors in {city}.");
                return Json(new { success = true, message = $"Smart matching completed. Found {matchingDonors.Count} eligible donors.", totalDonors = matchingDonors.Count, eligibleDonors = matchingDonors.Count, matchPercentage = 100, donors = donorList });
            }
            catch (Exception ex) { return Json(new { success = false, message = $"Error: {ex.Message}" }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendAlert(int alertId)
        {
            try
            {
                var alertData = await _context.EmergencyAlerts.Where(ea => ea.AlertId == alertId).Join(_context.BloodRequests, ea => ea.RequestId, br => br.RequestId, (ea, br) => new { ea, br }).FirstOrDefaultAsync();
                if (alertData == null) return Json(new { success = false, message = "Alert not found." });
                var matchingDonors = await _context.DonorProfiles.Where(d => d.BloodGroup == alertData.br.BloodGroup && d.City == alertData.br.City && d.IsAvailable == true).Join(_context.Users, d => d.UserId, u => u.UserId, (d, u) => new { d, u }).Where(x => x.u.IsActive && x.u.IsApproved && x.u.Role == "Donor").Select(x => new { x.u.Email, x.u.FullName }).ToListAsync();
                int successCount = 0, failedCount = 0;
                foreach (var donor in matchingDonors) { try { await _emailService.SendEmergencyDonorNotificationAsync(donor.Email, donor.FullName, alertData.br.BloodGroup, alertData.br.HospitalName, alertData.br.City, alertData.br.RequiredDate, alertData.br.UrgencyLevel, alertData.ea.AlertMessage ?? "Urgent blood requirement."); successCount++; } catch { failedCount++; } }
                await LogAdminAction($"Resent Emergency Alert ALT-{alertId:D4}. Sent: {successCount}, Failed: {failedCount}.");
                return Json(new { success = true, message = "Emergency notifications processed.", totalDonors = matchingDonors.Count, successCount, failedCount });
            }
            catch (Exception ex) { return Json(new { success = false, message = $"Error: {ex.Message}" }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NotifyHospital(int alertId)
        {
            try
            {
                var alertData = await _context.EmergencyAlerts.Where(ea => ea.AlertId == alertId).Join(_context.BloodRequests, ea => ea.RequestId, br => br.RequestId, (ea, br) => new { ea, br }).Join(_context.Users, x => x.br.ReceiverId, u => u.UserId, (x, u) => new { x.ea, x.br, ReceiverName = u.FullName }).FirstOrDefaultAsync();
                if (alertData == null) return Json(new { success = false, message = "Alert not found." });
                var hospitalProfile = await _context.HospitalProfiles.Where(h => h.HospitalName == alertData.br.HospitalName && h.City == alertData.br.City).Join(_context.Users, h => h.UserId, u => u.UserId, (h, u) => new { h, u.Email, u.FullName }).FirstOrDefaultAsync();
                if (hospitalProfile == null) return Json(new { success = false, message = "Hospital profile not found." });
                await _emailService.SendHospitalEmergencyNotificationAsync(hospitalProfile.Email, hospitalProfile.h.HospitalName, alertId, alertData.br.BloodGroup, alertData.br.UnitsRequired ?? 0, alertData.ReceiverName, alertData.br.UrgencyLevel, alertData.br.RequiredDate);
                await LogAdminAction($"Notified Hospital '{hospitalProfile.h.HospitalName}' for Emergency Alert ALT-{alertId:D4}.");
                return Json(new { success = true, message = "Hospital notified successfully." });
            }
            catch (Exception ex) { return Json(new { success = false, message = $"Error: {ex.Message}" }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAlertStatus(int alertId, string status)
        {
            try
            {
                var alert = await _context.EmergencyAlerts.FindAsync(alertId);
                if (alert == null) return Json(new { success = false, message = "Alert not found" });
                var currentStatus = alert.AlertStatus;
                bool isValid = (currentStatus == "Active" && (status == "In Progress" || status == "Resolved" || status == "Ignored")) || (currentStatus == "In Progress" && status == "Resolved");
                if (!isValid) return Json(new { success = false, message = $"Invalid status transition from '{currentStatus}' to '{status}'." });
                alert.AlertStatus = status;
                await _context.SaveChangesAsync();
                await LogAdminAction($"Updated Emergency Alert ALT-{alertId:D4} status from '{currentStatus}' to '{status}'.");
                return Json(new { success = true, message = $"Alert status updated to '{status}' successfully." });
            }
            catch (Exception ex) { return Json(new { success = false, message = $"Error: {ex.Message}" }); }
        }

        // ==================== SMART MATCH CENTER (UNCHANGED EXCEPT WHERE NOTED) ====================

        [HttpGet]
        public async Task<IActionResult> SmartMatch(int? alertId)
        {
            var vm = new SmartMatchViewModel
            {
                AvailableCities = await _context.DonorProfiles.Where(d => !string.IsNullOrEmpty(d.City)).Select(d => d.City).Distinct().OrderBy(c => c).ToListAsync(),
                MatchedDonors = new List<DonorMatchResultViewModel>()
            };

            vm.TotalMatchRequests = await _context.BloodRequests.CountAsync();
            vm.TodayMatches = await _context.DonorMatches.CountAsync(m => m.MatchDate.Date == DateTime.Today);
            vm.ActiveMatches = await _context.DonorMatches.CountAsync(m => m.Status == "Matched" || m.Status == "DonationScheduled");
            vm.SuccessfulMatches = await _context.DonorMatches.CountAsync(m => m.Status == "Fulfilled" || m.Status == "DonationCompleted");
            vm.FailedMatches = await _context.DonorMatches.CountAsync(m => m.Status == "Rejected" || m.Status == "Cancelled");
            vm.PendingRequests = await _context.BloodRequests.CountAsync(r => r.RequestStatus == "Pending");

            vm.AvgMatchScore = await _context.DonorMatches.AnyAsync() ? (int)await _context.DonorMatches.AverageAsync(m => m.MatchScore) : 0;
            vm.AvgDistance = await _context.DonorMatches.AnyAsync() ? Math.Round(await _context.DonorMatches.AverageAsync(m => m.DistanceKm), 1) : 0;

            vm.AvailableDonors = await _context.DonorProfiles.CountAsync(d => d.IsAvailable);
            vm.BusyDonors = await _context.DonorProfiles.CountAsync(d => !d.IsAvailable);
            vm.EmergencyMatches = await _context.BloodRequests.CountAsync(r => r.UrgencyLevel == "Critical");

            int contacted = await _context.DonorMatches.CountAsync(m => m.Status != "PotentialMatch");
            int accepted = await _context.DonorMatches.CountAsync(m => m.Status == "Accepted" || m.Status == "Matched" || m.Status == "Fulfilled");
            vm.ResponseRate = contacted > 0 ? (int)((double)accepted / contacted * 100) : 0;
            vm.AcceptanceRate = await _context.DonorMatches.AnyAsync() ? (int)((double)accepted / await _context.DonorMatches.CountAsync() * 100) : 0;
            vm.CompletionRate = await _context.BloodRequests.AnyAsync() ? (int)((double)vm.SuccessfulMatches / await _context.BloodRequests.CountAsync() * 100) : 0;

            if (alertId.HasValue)
            {
                var alert = await _context.EmergencyAlerts.Where(a => a.AlertId == alertId.Value).Join(_context.BloodRequests, a => a.RequestId, br => br.RequestId, (a, br) => new { a, br }).FirstOrDefaultAsync();
                if (alert != null)
                {
                    vm.IsEmergencyMode = true;
                    vm.EmergencyAlertId = alertId.Value;
                    vm.EmergencyBloodGroup = alert.br.BloodGroup;
                    vm.EmergencyCity = alert.br.City;
                    vm.EmergencyHospital = alert.br.HospitalName;
                    vm.EmergencyUrgency = alert.br.UrgencyLevel;
                    vm.EmergencyUnits = alert.br.UnitsRequired ?? 1;
                    vm.EmergencyRequiredDate = alert.br.RequiredDate;
                }
            }

            ViewBag.UnreadMessageCount = await _context.ContactMessages.CountAsync(m => m.Status == "New");
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FindDonors(SmartMatchInputViewModel input)
        {
            try
            {
                if (string.IsNullOrEmpty(input.BloodGroupRequired) || string.IsNullOrEmpty(input.City))
                    return Json(new { success = false, message = "Blood Group and City are required." });

                var compatibleGroups = GetCompatibleDonorBloodGroups(input.BloodGroupRequired);
                double reqLat = 0, reqLng = 0;
                if (input.BloodRequestId.HasValue && input.BloodRequestId.Value > 0)
                {
                    var reqCoords = await _context.BloodRequests.Where(r => r.RequestId == input.BloodRequestId.Value).Select(r => new { r.Latitude, r.Longitude }).FirstOrDefaultAsync();
                    if (reqCoords != null) { reqLat = reqCoords.Latitude ?? 0; reqLng = reqCoords.Longitude ?? 0; }
                }

                var donors = await _context.DonorProfiles
    .Join(_context.Users, d => d.UserId, u => u.UserId, (d, u) => new { d, u })
    .Where(x => x.u.Role == "Donor" && x.u.IsActive && x.u.IsApproved && compatibleGroups.Contains(x.d.BloodGroup))
    .Select(x => new
    {
        x.u.UserId,
        x.u.FullName,
        x.u.ProfilePicture,
        x.d.DateOfBirth,
        x.d.Gender,
        x.d.BloodGroup,
        x.d.City,
        x.d.Address,
        x.d.IsAvailable,
        x.d.LastDonationDate,
        x.d.Latitude,
        x.d.Longitude,
        x.d.OnlineStatus,
        x.d.SuccessfulDonations,
        x.d.AcceptanceRate,
        x.d.ResponseRate,
        x.u.Email,
        x.u.Phone,
        x.u.CreatedAt,
        x.u.IsApproved,        // ✅ ADD THIS
        x.u.IsEmailVerified    // ✅ ADD THIS
    }).ToListAsync();

                var donorIds = donors.Select(d => d.UserId).ToList();
                var donationCounts = await _context.Donations.Where(d => donorIds.Contains(d.DonorId)).GroupBy(d => d.DonorId).Select(g => new { DonorId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.DonorId, x => x.Count);
                var lastMatches = await _context.DonorMatches.Where(m => donorIds.Contains(m.DonorId) && m.BloodRequestId == input.BloodRequestId).ToDictionaryAsync(m => m.DonorId, m => m);

                var distanceTasks = donors.Select(async d => {
                    var (dist, duration, success) = await _googleMapsService.GetDistanceAndDurationAsync(d.Latitude ?? 0, d.Longitude ?? 0, reqLat, reqLng);
                    if ((d.Latitude == null || d.Longitude == null || reqLat == 0 || reqLng == 0) && d.City?.Equals(input.City, StringComparison.OrdinalIgnoreCase) == true) { dist = 2.0; duration = "5 mins"; }
                    else if (dist == 999) { dist = 50.0; duration = "1 hr"; }
                    return new { DonorId = d.UserId, Distance = dist, Duration = duration };
                }).ToList();

                var distanceResults = await Task.WhenAll(distanceTasks);
                var distanceDict = distanceResults.ToDictionary(d => d.DonorId, d => (Distance: d.Distance, Duration: d.Duration));

                var results = new List<DonorMatchResultViewModel>();
                var requestDummy = new BloodRequest { BloodGroup = input.BloodGroupRequired };

                foreach (var d in donors)
                {
                    var distData = distanceDict[d.UserId];
                    double distance = distData.Distance;
                    string travelTimeStr = distData.Duration;

                    var (score, breakdown, explanation) = CalculateAdvancedMatchScore(new DonorProfile { BloodGroup = d.BloodGroup, IsAvailable = d.IsAvailable, LastDonationDate = d.LastDonationDate, SuccessfulDonations = d.SuccessfulDonations, AcceptanceRate = d.AcceptanceRate, ResponseRate = d.ResponseRate, OnlineStatus = d.OnlineStatus }, requestDummy, distance);

                    int age = d.DateOfBirth.HasValue ? DateTime.Now.Year - d.DateOfBirth.Value.Year : 0;
                    int totalDonations = donationCounts.GetValueOrDefault(d.UserId, 0);
                    var lastMatch = lastMatches.GetValueOrDefault(d.UserId);

                    int daysSinceLast = d.LastDonationDate.HasValue ? (DateTime.Now - d.LastDonationDate.Value).Days : 999;
                    string eligibility = daysSinceLast < 56 ? "In Cooldown" : "Eligible";

                    // Calculate AI Badge based on score
                    string aiBadge = score >= 90 ? "★★★★★ Perfect" :
                                     score >= 75 ? "★★★★ Excellent" :
                                     score >= 50 ? "★★★ Good" :
                                     score >= 30 ? "★★ Backup" : "★ Low Match";

                    results.Add(new DonorMatchResultViewModel
                    {
                        DonorId = d.UserId,
                        DonorName = d.FullName,
                        ProfilePicture = d.ProfilePicture ?? "default.png",
                        Age = d.DateOfBirth.HasValue ? DateTime.Now.Year - d.DateOfBirth.Value.Year : 0,
                        Gender = d.Gender ?? "Unknown",
                        BloodGroup = d.BloodGroup,
                        City = d.City ?? "Unknown",
                        Area = d.Address ?? "-",
                        DistanceKm = Math.Round(distance, 1),
                        TravelTime = travelTimeStr,
                        AvailabilityStatus = d.IsAvailable ? "Active" : "Inactive",
                        LastDonationDate = d.LastDonationDate,
                        EligibilityStatus = eligibility,
                        IsVerified = d.IsApproved && d.IsEmailVerified, // ✅ FIXED: Changed 'u' to 'd'
                        TotalDonations = totalDonations,
                        MatchScore = score,
                        ScoreBreakdown = breakdown,
                        Explanation = explanation,
                        AiBadge = aiBadge, // ✅ Now defined above
                        Phone = d.Phone,
                        Email = d.Email,
                        RegistrationDate = d.CreatedAt
                    });
                }

                var sorted = results.Where(r => r.MatchScore > 0).OrderByDescending(r => r.MatchScore).ThenBy(r => r.DistanceKm).ThenByDescending(r => r.AvailabilityStatus == "Active").ToList();
                await LogAdminAction($"Executed Smart Match for {input.BloodGroupRequired} in {input.City}. Found {sorted.Count} eligible donors.");
                return Json(new { success = true, donors = sorted });
            }
            catch (Exception ex) { return Json(new { success = false, message = $"Error: {ex.Message}" }); }
        }

        private (int score, string breakdown, string explanation) CalculateAdvancedMatchScore(DonorProfile donor, BloodRequest request, double distanceKm)
        {
            int bloodScore = 0, distScore = 0, availScore = 0, histScore = 0;
            var reasons = new List<string>();

            if (donor.BloodGroup == request.BloodGroup) { bloodScore = 40; reasons.Add("Exact blood match"); }
            else if (GetCompatibleDonorBloodGroups(request.BloodGroup).Contains(donor.BloodGroup)) { bloodScore = 30; reasons.Add("Compatible blood match"); }

            if (distanceKm <= 2) { distScore = 30; reasons.Add($"Very close ({distanceKm:F1} km)"); }
            else if (distanceKm <= 5) { distScore = 25; reasons.Add($"Close proximity ({distanceKm:F1} km)"); }
            else if (distanceKm <= 10) { distScore = 20; reasons.Add($"Within 10 km ({distanceKm:F1} km)"); }
            else if (distanceKm <= 20) { distScore = 10; reasons.Add($"Within 20 km ({distanceKm:F1} km)"); }
            else if (distanceKm <= 50) { distScore = 5; }

            if (donor.IsAvailable) { availScore = 15; reasons.Add("Available now"); if (donor.OnlineStatus == "Online") reasons.Add("Currently online"); }

            if (donor.SuccessfulDonations >= 5) { histScore += 5; reasons.Add("Highly experienced"); }
            else if (donor.SuccessfulDonations >= 2) { histScore += 3; reasons.Add("Experienced"); }
            else if (donor.SuccessfulDonations >= 1) { histScore += 1; }

            if (donor.AcceptanceRate >= 80) { histScore += 5; reasons.Add("High acceptance rate"); }
            else if (donor.AcceptanceRate >= 50) { histScore += 3; }
            else if (donor.AcceptanceRate > 0) { histScore += 1; }

            if (donor.ResponseRate >= 80) { histScore += 5; reasons.Add("Highly responsive"); }
            else if (donor.ResponseRate >= 50) { histScore += 3; }
            else if (donor.ResponseRate > 0) { histScore += 1; }

            if (donor.LastDonationDate.HasValue)
            {
                int daysSince = (DateTime.Now - donor.LastDonationDate.Value).Days;
                if (daysSince < 56) { histScore = 0; reasons.Add("Recently donated (Not eligible)"); }
                else if (daysSince >= 90) { reasons.Add("Fully eligible"); }
            }
            else { reasons.Add("First-time eligible"); }

            int totalScore = bloodScore + distScore + availScore + histScore;
            string breakdown = $"Blood: {bloodScore}/40 | Dist: {distScore}/30 | Avail: {availScore}/15 | Hist: {histScore}/15";
            string explanation = reasons.Any() ? string.Join(", ", reasons) : "Standard match";

            return (totalScore, breakdown, explanation);
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingRequests()
        {
            var requests = await _context.BloodRequests.Where(r => r.RequestStatus == "Pending").OrderByDescending(r => r.CreatedAt).Select(r => new { r.RequestId, r.BloodGroup, r.City, r.HospitalName, r.UrgencyLevel, r.UnitsRequired, r.Latitude, r.Longitude }).ToListAsync();
            return Json(requests);
        }

        [HttpGet]
        public async Task<IActionResult> GetLiveUpdates()
        {
            var stats = new
            {
                ActiveMatches = await _context.DonorMatches.CountAsync(m => m.Status == "Matched" || m.Status == "DonationScheduled"),
                PendingRequests = await _context.BloodRequests.CountAsync(r => r.RequestStatus == "Pending"),
                TodayMatches = await _context.DonorMatches.CountAsync(m => m.MatchDate.Date == DateTime.Today),
                TotalDonorsOnline = await _context.DonorProfiles.CountAsync(d => d.OnlineStatus == "Online"),
                AvailableDonors = await _context.DonorProfiles.CountAsync(d => d.IsAvailable),
                EmergencyRequests = await _context.BloodRequests.CountAsync(r => r.UrgencyLevel == "Critical" && r.RequestStatus == "Pending")
            };
            return Json(stats);
        }

        [HttpGet]
        public async Task<IActionResult> GetDonorProfile(int donorId)
        {
            var donor = await _context.DonorProfiles.Where(d => d.UserId == donorId).Join(_context.Users, d => d.UserId, u => u.UserId, (d, u) => new { d, u }).FirstOrDefaultAsync();
            if (donor == null) return NotFound();

            var history = await _context.Donations.Where(d => d.DonorId == donorId).OrderByDescending(d => d.DonationDate).Select(d => new DonationHistoryItem { Date = d.DonationDate, Hospital = d.HospitalName, Status = "Completed" }).ToListAsync();
            var timeline = await _context.DonorMatches.Where(m => m.DonorId == donorId).OrderByDescending(m => m.MatchDate).Select(m => new MatchTimelineItem { Status = m.Status, Date = m.MatchDate, Description = $"Match Score: {m.MatchScore}%" }).ToListAsync();

            var vm = new DonorProfileViewModel
            {
                DonorId = donor.u.UserId,
                FullName = donor.u.FullName,
                Email = donor.u.Email,
                Phone = donor.u.Phone,
                ProfilePicture = donor.u.ProfilePicture ?? "default.png",
                Gender = donor.d.Gender ?? "Unknown",
                DateOfBirth = donor.d.DateOfBirth,
                BloodGroup = donor.d.BloodGroup,
                City = donor.d.City,
                Area = donor.d.Address,
                Address = donor.d.Address,
                IsAvailable = donor.d.IsAvailable,
                LastDonationDate = donor.d.LastDonationDate,
                TotalDonations = history.Count,
                RegistrationDate = donor.u.CreatedAt,
                DonationHistory = history,
                Timeline = timeline
            };
            return Json(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LogContact(int donorId, string contactType, string message)
        {
            var log = new ContactLog { DonorId = donorId, AdminId = GetAdminId(), ContactType = contactType, Message = message, CreatedAt = DateTime.Now };
            _context.ContactLogs.Add(log);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ==================== UPDATED SMART MATCH WORKFLOW (EMAIL + WHATSAPP) ====================

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SendDonationRequest(int donorId, int bloodRequestId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var request = await _context.BloodRequests.FindAsync(bloodRequestId);
                if (request == null) throw new Exception("Blood request not found.");

                var existing = await _context.DonorMatches.FirstOrDefaultAsync(m => m.DonorId == donorId && m.BloodRequestId == bloodRequestId);
                if (existing != null && existing.Status != "Rejected" && existing.Status != "Cancelled")
                    return Json(new { success = false, message = "Request already sent to this donor." });

                var match = existing ?? new DonorMatch();
                match.BloodRequestId = bloodRequestId; match.DonorId = donorId; match.Status = "RequestSent"; match.MatchDate = DateTime.Now; match.AdminId = GetAdminId();
                if (existing == null) _context.DonorMatches.Add(match);

                var donorUser = await _context.Users.FindAsync(donorId);

                // NEW: Fetch Receiver details to include in WhatsApp message
                var receiverUser = await _context.Users.FindAsync(request.ReceiverId);

                // 1. Send Email (Unchanged)
                await _emailService.SendDonationRequestEmailAsync(donorUser.Email, donorUser.FullName, bloodRequestId, request.BloodGroup, request.HospitalName);

                // 2. Send WhatsApp (Replaces SMS)
                var whatsappMessage = $"🚨 *Urgent Blood Donation Request* 🚨\n\n" +
                                      $"*Donor Name:* {donorUser.FullName}\n" +
                                      $"*Blood Group:* {request.BloodGroup}\n" +
                                      $"*Hospital Name:* {request.HospitalName}\n" +
                                      $"*Patient Name:* {receiverUser?.FullName ?? "N/A"}\n" +
                                      $"*Required Blood Group:* {request.BloodGroup}\n" +
                                      $"*Required Units:* {request.UnitsRequired}\n" +
                                      $"*Hospital Address:* {request.HospitalName}, {request.City}\n" +
                                      $"*Emergency Contact:* {receiverUser?.Phone ?? "N/A"}\n" +
                                      $"*Request Date:* {request.RequiredDate?.ToString("dd-MMM-yyyy") ?? request.CreatedAt.ToString("dd-MMM-yyyy")}\n" +
                                      $"*Request ID:* REQ-{bloodRequestId:D4}\n\n" +
                                      $"Please login to your dashboard to accept this request and save a life.";

                // WhatsApp service handles its own errors internally, so it won't break the transaction if it fails
                await _whatsAppService.SendWhatsAppAsync(donorUser.Phone, whatsappMessage,
                    requestId: bloodRequestId, donorId: donorId, category: "SmartMatch");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await LogAdminAction($"Sent donation request to Donor ID: {donorId} for Request ID: {bloodRequestId}");
                await _hubContext.Clients.All.SendAsync("ReceiveSystemNotification", $"Donation request sent to Donor ID: {donorId}", "info");

                return Json(new { success = true, message = "Donation request sent successfully." });
            }
            catch (Exception ex) { await transaction.RollbackAsync(); return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectDonor(int donorId, int bloodRequestId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var request = await _context.BloodRequests.Include(r => r.Receiver).FirstOrDefaultAsync(r => r.RequestId == bloodRequestId);
                if (request == null) throw new Exception("Blood request not found.");

                var donorUser = await _context.Users.FindAsync(donorId);
                if (donorUser == null) throw new Exception("Donor not found.");

                // 1. Create or Update DonorMatch Record (Populates Donor's "Smart Matches" page)
                var match = await _context.DonorMatches.FirstOrDefaultAsync(m => m.DonorId == donorId && m.BloodRequestId == bloodRequestId);
                if (match == null)
                {
                    match = new DonorMatch
                    {
                        BloodRequestId = bloodRequestId,
                        DonorId = donorId,
                        MatchScore = 95,
                        Status = "Accepted", // Shows as Accepted in Donor Panel
                        MatchDate = DateTime.Now,
                        AdminId = GetAdminId(),
                        DistanceKm = 1.2,
                        TravelTime = "5 min",
                        EmailSent = true,
                        SmsSent = true
                    };
                    _context.DonorMatches.Add(match);
                }
                else
                {
                    match.Status = "Accepted";
                    match.MatchDate = DateTime.Now;
                }

                // Update Request Status
                request.RequestStatus = "Matched";

                // 2. Send Email (Reuses existing EmailService)
                await _emailService.SendDonorSelectedEmailAsync(donorUser.Email, donorUser.FullName, bloodRequestId, request.BloodGroup ?? "Unknown");

                // 3. Send WhatsApp Message Automatically
                var whatsappMessage = $"🎉 *Congratulations! You Have Been Selected* 🎉\n\n" +
                                      $"*Blood Group:* {request.BloodGroup}\n" +
                                      $"*Hospital:* {request.HospitalName}\n" +
                                      $"*Patient:* {request.PatientName ?? request.Receiver?.FullName ?? "N/A"}\n" +
                                      $"*Units Required:* {request.UnitsRequired}\n" +
                                      $"*Request ID:* REQ-{bloodRequestId:D4}\n\n" +
                                      $"Please login to your dashboard to view details and prepare for the donation.";

                await _whatsAppService.SendWhatsAppAsync(donorUser.Phone, whatsappMessage, bloodRequestId, null, donorId, "SmartMatch");

                // 4. Create Donor Notification (Populates Donor's "Notifications" page)
                bool notifExists = await _context.DonorNotifications.AnyAsync(n => n.DonorId == donorId && n.Category == "SmartMatch" && n.ReferenceId == bloodRequestId);
                if (!notifExists)
                {
                    _context.DonorNotifications.Add(new DonorNotification
                    {
                        DonorId = donorId,
                        Title = "Smart Match Assigned",
                        Message = $"You have been assigned for {request.BloodGroup} blood donation at {request.HospitalName}.",
                        Category = "SmartMatch",
                        ReferenceId = bloodRequestId,
                        ActionUrl = "/Donor/SmartMatches",
                        IsRead = false,
                        CreatedDate = DateTime.Now
                    });
                }

                // 5. If Emergency, Create Emergency Alert for Donor (Populates "Emergency Alerts" page)
                if (request.UrgencyLevel == "Critical")
                {
                    bool emergNotifExists = await _context.DonorNotifications.AnyAsync(n => n.DonorId == donorId && n.Category == "EmergencyAlert" && n.ReferenceId == bloodRequestId);
                    if (!emergNotifExists)
                    {
                        _context.DonorNotifications.Add(new DonorNotification
                        {
                            DonorId = donorId,
                            Title = "🚨 Emergency Blood Donation",
                            Message = $"URGENT: {request.BloodGroup} blood required at {request.HospitalName}. Please act immediately.",
                            Category = "EmergencyAlert",
                            ReferenceId = bloodRequestId,
                            ActionUrl = "/Donor/EmergencyAlerts",
                            IsRead = false,
                            CreatedDate = DateTime.Now
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await LogAdminAction($"Assigned Donor ID: {donorId} for Request ID: {bloodRequestId}");

                return Json(new { success = true, message = "Donor assigned successfully! Email, WhatsApp, and Notifications sent." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectDonor(int donorId, int bloodRequestId, string reason)
        {
            var match = await _context.DonorMatches.FirstOrDefaultAsync(m => m.DonorId == donorId && m.BloodRequestId == bloodRequestId);
            if (match == null) return Json(new { success = false, message = "Match record not found." });

            match.Status = "Rejected"; match.RejectionReason = reason; match.CancelledDate = DateTime.Now;
            await _context.SaveChangesAsync();

            await LogAdminAction($"Rejected Donor ID: {donorId} for Request ID: {bloodRequestId}. Reason: {reason}");
            await _hubContext.Clients.All.SendAsync("ReceiveSystemNotification", $"Donor ID: {donorId} rejected for Request REQ-{bloodRequestId:D4}.", "warning");

            return Json(new { success = true, message = "Donor rejected." });
        }

        // ==================== HELPERS & UTILITIES (UNCHANGED) ====================

        private int GetAdminId() => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 0;

        private List<string> GetCompatibleDonorBloodGroups(string receiver)
        {
            return receiver switch
            {
                "O-" => new List<string> { "O-" },
                "O+" => new List<string> { "O-", "O+" },
                "A-" => new List<string> { "O-", "A-" },
                "A+" => new List<string> { "O-", "O+", "A-", "A+" },
                "B-" => new List<string> { "O-", "B-" },
                "B+" => new List<string> { "O-", "O+", "B-", "B+" },
                "AB-" => new List<string> { "O-", "A-", "B-", "AB-" },
                "AB+" => new List<string> { "O-", "O+", "A-", "A+", "B-", "B+", "AB-", "AB+" },
                _ => new List<string>()
            };
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            if (lat1 == 0 || lon1 == 0 || lat2 == 0 || lon2 == 0) return 999;
            var R = 6371; var dLat = (lat2 - lat1) * Math.PI / 180; var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * (2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
        }

        private async Task LogAdminAction(string action)
        {
            try
            {
                var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(adminId, out int adminUserId))
                {
                    var log = new AdminLog { AdminId = adminUserId, Action = action, CreatedAt = DateTime.Now };
                    _context.AdminLogs.Add(log);
                    await _context.SaveChangesAsync();
                }
            }
            catch { }
        }

        [HttpGet]
        public async Task<IActionResult> GetMessageCounts()
        {
            try
            {
                var unread = await _context.ContactMessages.CountAsync(m => m.Status == "New");
                var pendingHospitals = await _context.HospitalProfiles.CountAsync(h => h.VerificationStatus == "Pending");
                var pendingRequests = await _context.BloodRequests.CountAsync(r => r.RequestStatus == "Pending");
                return Json(new { unread = unread, pendingHospitals = pendingHospitals, pendingRequests = pendingRequests });
            }
            catch (Exception ex) { return Json(new { unread = 0, pendingHospitals = 0, pendingRequests = 0, error = ex.Message }); }
        }

        // ==================== NOTIFICATION HISTORY & CONTACT MESSAGES (UNCHANGED) ====================

        [HttpGet]
        public IActionResult NotificationHistory()
        {
            ViewData["Title"] = "Notification History";
            ViewBag.UnreadMessageCount = _context.ContactMessages.Count(m => m.Status == "New");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationLogs(string search, string type, string status, string category, int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.NotificationLogs.AsQueryable();
                if (!string.IsNullOrEmpty(search)) query = query.Where(n => (n.RecipientEmail != null && n.RecipientEmail.Contains(search)) || (n.RecipientPhone != null && n.RecipientPhone.Contains(search)) || (n.Subject != null && n.Subject.Contains(search)));
                if (!string.IsNullOrEmpty(type) && type != "all") query = query.Where(n => n.NotificationType == type);
                if (!string.IsNullOrEmpty(status) && status != "all") query = query.Where(n => n.Status == status);
                if (!string.IsNullOrEmpty(category) && category != "all") query = query.Where(n => n.Category == category);

                var totalCount = await query.CountAsync();
                var logs = await query.OrderByDescending(n => n.SentAt).Skip((page - 1) * pageSize).Take(pageSize).Select(n => new NotificationLogViewModel { LogId = n.LogId, Recipient = n.RecipientEmail ?? n.RecipientPhone ?? "N/A", Type = n.NotificationType, Category = n.Category, Subject = n.Subject ?? "N/A", Status = n.Status, ErrorMessage = n.ErrorMessage, SentAt = n.SentAt }).ToListAsync();
                return Json(new { total = totalCount, rows = logs });
            }
            catch (Exception ex) { return Json(new { total = 0, rows = new List<NotificationLogViewModel>(), error = ex.Message }); }
        }

        [HttpGet]
        public async Task<IActionResult> ContactMessages()
        {
            ViewData["Title"] = "Contact Messages";
            var messages = await _context.ContactMessages.OrderByDescending(m => m.CreatedAt).ToListAsync();
            var model = messages.Select(AdminContactMessageViewModel.FromEntity).ToList();

            ViewBag.TotalMessages = model.Count;
            ViewBag.TodayMessages = model.Count(m => m.SentDate.Date == DateTime.Today);
            ViewBag.ReadMessages = model.Count(m => m.IsRead);
            ViewBag.UnreadMessages = model.Count(m => !m.IsRead);
            ViewBag.UnreadMessageCount = ViewBag.UnreadMessages;

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkMessageAsRead(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg == null) return NotFound();
            if (msg.Status != "Read") { msg.Status = "Read"; await _context.SaveChangesAsync(); }
            return Json(new { success = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkMessageAsUnread(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg == null) return NotFound();
            if (msg.Status != "New") { msg.Status = "New"; await _context.SaveChangesAsync(); }
            return Json(new { success = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg == null) return NotFound();
            _context.ContactMessages.Remove(msg);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyToMessage(int messageId, string replyMessage)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(replyMessage)) return Json(new { success = false, message = "Reply message cannot be empty." });
                var originalMessage = await _context.ContactMessages.FindAsync(messageId);
                if (originalMessage == null) return Json(new { success = false, message = "Message not found." });

                var adminId = GetAdminId();
                var admin = await _context.Users.FindAsync(adminId);
                var adminEmail = admin?.Email ?? "admin@khoonehayat.com";
                var adminName = admin?.FullName ?? "Khoon-e-Hayat Admin";

                var subject = $"Re: {originalMessage.Subject}";
                var body = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
            <h2 style='color: #90151C;'>Response from Khoon-e-Hayat</h2>
            <p>Dear {originalMessage.FullName},</p>
            <p>Thank you for contacting Khoon-e-Hayat. Here is our response to your inquiry:</p>
            <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #23BBB7; margin: 20px 0;'>
                <p style='margin: 5px 0;'><strong>Your Original Message:</strong></p>
                <p style='margin: 5px 0; color: #666;'>{originalMessage.Message}</p>
            </div>
            <div style='background-color: #e8f5e9; padding: 15px; border-left: 4px solid #198754; margin: 20px 0;'>
                <p style='margin: 5px 0;'><strong>Our Response:</strong></p>
                <p style='margin: 5px 0;'>{replyMessage.Replace(Environment.NewLine, "<br/>")}</p>
            </div>
            <p>If you have any further questions, please don't hesitate to contact us.</p>
            <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;' />
            <p style='color: #666; font-size: 0.9em;'>
                Best regards,<br />
                {adminName}<br />
                Khoon-e-Hayat Team<br />
                <a href='mailto:support@khoonehayat.pk'>support@khoonehayat.pk</a>
            </p>
        </div>";

                await _emailService.SendEmailAsync(originalMessage.Email, subject, body, "ContactMessageReply");

                var notificationLog = new NotificationLog
                {
                    RequestId = null,
                    AlertId = null,
                    DonorId = null,
                    RecipientEmail = originalMessage.Email,
                    RecipientPhone = null,
                    NotificationType = "Email",
                    Category = "ContactMessageReply",
                    Subject = subject,
                    Message = replyMessage,
                    Status = "Sent",
                    ErrorMessage = null,
                    SentAt = DateTime.Now
                };

                _context.NotificationLogs.Add(notificationLog);
                await _context.SaveChangesAsync();
                await LogAdminAction($"Replied to contact message MSG-{messageId:D4} from {originalMessage.Email}");
                return Json(new { success = true, message = "Reply sent successfully!" });
            }
            catch (Exception ex) { return Json(new { success = false, message = $"Error: {ex.Message}" }); }
        }

        // GET: Admin/GetLatestNotifications
        [HttpGet]
        public async Task<IActionResult> GetLatestNotifications()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Json(new { success = false, message = "User not authenticated", notifications = new List<object>(), unreadCount = 0 });
                }

                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Json(new { success = false, message = "Invalid user ID", notifications = new List<object>(), unreadCount = 0 });
                }

                // For Admin, we'll get notifications from multiple sources
                var notifications = new List<object>();

                // Get recent contact messages
                var messages = await _context.ContactMessages
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(5)
                    .Select(m => new
                    {
                        NotificationId = m.MessageId,
                        Title = "New Contact Message",
                        Message = $"{m.FullName} sent you a message: {m.Subject}",
                        Category = "ContactMessage",
                        IsRead = m.Status == "Read",
                        CreatedDate = m.CreatedAt,
                        ActionUrl = "/Admin/ContactMessages",
                        TimeAgo = GetTimeAgo(m.CreatedAt)
                    })
                    .ToListAsync();

                // Get pending hospital verifications
                var pendingHospitals = await _context.HospitalProfiles
                    .Where(h => h.VerificationStatus == "Pending")
                    .Join(_context.Users, h => h.UserId, u => u.UserId, (h, u) => new
                    {
                        NotificationId = h.HospitalId + 1000,
                        Title = "Hospital Verification Pending",
                        Message = $"{h.HospitalName} is awaiting verification",
                        Category = "HospitalVerification",
                        IsRead = false,
                        CreatedDate = u.CreatedAt,
                        ActionUrl = "/Admin/HospitalVerification",
                        TimeAgo = GetTimeAgo(u.CreatedAt)
                    })
                    .Take(5)
                    .ToListAsync();

                // Get pending blood requests
                var pendingRequests = await _context.BloodRequests
                    .Where(r => r.RequestStatus == "Pending")
                    .Join(_context.Users, r => r.ReceiverId, u => u.UserId, (r, u) => new
                    {
                        NotificationId = r.RequestId + 2000,
                        Title = "Pending Blood Request",
                        Message = $"New blood request for {r.BloodGroup} at {r.HospitalName}",
                        Category = "BloodRequest",
                        IsRead = false,
                        CreatedDate = r.CreatedAt,
                        ActionUrl = "/Admin/BloodRequests",
                        TimeAgo = GetTimeAgo(r.CreatedAt)
                    })
                    .Take(5)
                    .ToListAsync();

                // Combine and sort
                var allNotifications = messages.Concat<dynamic>(pendingHospitals).Concat<dynamic>(pendingRequests)
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(10)
                    .ToList();

                var unreadCount = await _context.ContactMessages.CountAsync(m => m.Status == "New") +
                                 await _context.HospitalProfiles.CountAsync(h => h.VerificationStatus == "Pending") +
                                 await _context.BloodRequests.CountAsync(r => r.RequestStatus == "Pending");

                return Json(new
                {
                    success = true,
                    notifications = allNotifications,
                    unreadCount
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    notifications = new List<object>(),
                    unreadCount = 0
                });
            }
        }

        // GET: Admin/GetUnreadNotificationCount
        [HttpGet]
        public async Task<IActionResult> GetUnreadNotificationCount()
        {
            try
            {
                var unreadMessages = await _context.ContactMessages.CountAsync(m => m.Status == "New");
                var pendingHospitals = await _context.HospitalProfiles.CountAsync(h => h.VerificationStatus == "Pending");
                var pendingRequests = await _context.BloodRequests.CountAsync(r => r.RequestStatus == "Pending");

                var totalUnread = unreadMessages + pendingHospitals + pendingRequests;

                return Json(new { unread = totalUnread });
            }
            catch (Exception ex)
            {
                return Json(new { unread = 0, error = ex.Message });
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
    }
}
