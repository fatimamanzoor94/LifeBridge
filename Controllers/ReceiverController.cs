using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Khoon_e_Hayat.Data;
using Khoon_e_Hayat.Models.Entities;
using Khoon_e_Hayat.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Khoon_e_Hayat.Controllers
{

    [Route("[controller]/[action]")]
    [Authorize(Roles = "Receiver")]
    public class ReceiverController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ReceiverController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            var receiverProfile = await _context.ReceiverProfiles.FirstOrDefaultAsync(r => r.UserId == userId);

            if (user == null || receiverProfile == null) return RedirectToAction("Login", "Account");

            var vm = new ReceiverDashboardViewModel
            {
                ReceiverName = user.FullName,
                ProfileImage = user.ProfilePicture ?? "/assets/img/avatars/DefaultAvatar.png",
                BloodGroupNeeded = receiverProfile.BloodGroupNeeded ?? "N/A",
                TotalRequests = await _context.BloodRequests.CountAsync(r => r.ReceiverId == userId),
                PendingRequests = await _context.BloodRequests.CountAsync(r => r.ReceiverId == userId && r.RequestStatus == "Pending"),
                MatchedDonors = await _context.DonorMatches.CountAsync(m => _context.BloodRequests.Where(r => r.ReceiverId == userId).Select(r => r.RequestId).Contains(m.BloodRequestId)),
                CompletedRequests = await _context.BloodRequests.CountAsync(r => r.ReceiverId == userId && r.RequestStatus == "Fulfilled"),
                EmergencyRequests = await _context.BloodRequests.CountAsync(r => r.ReceiverId == userId && r.UrgencyLevel == "Critical" && r.RequestStatus != "Fulfilled"),
                CompatibleDonorGroups = GetCompatibleDonorGroups(receiverProfile.BloodGroupNeeded)
            };

            var statusCounts = await _context.BloodRequests
                .Where(r => r.ReceiverId == userId)
                .GroupBy(r => r.RequestStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var allStatuses = new[] { "Pending", "Matched", "Fulfilled", "Cancelled" };
            vm.RequestStatusData = allStatuses.Select(s => new ChartData
            {
                Label = s,
                Value = statusCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0,
                Color = s switch { "Pending" => "#ffc107", "Matched" => "#0dcaf0", "Fulfilled" => "#198754", "Cancelled" => "#dc3545", _ => "#6c757d" }
            }).ToList();

            var bgCounts = await _context.BloodRequests
                .Where(r => r.ReceiverId == userId)
                .GroupBy(r => r.BloodGroup)
                .Select(g => new { BG = g.Key, Count = g.Count() })
                .ToListAsync();

            var allBloodGroups = new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
            vm.BloodGroupDistributionData = allBloodGroups.Select(bg => new ChartData
            {
                Label = bg,
                Value = bgCounts.FirstOrDefault(x => x.BG == bg)?.Count ?? 0,
                Color = "#2F6E9B"
            }).ToList();

            vm.RecentRequests = await _context.BloodRequests
                .Where(r => r.ReceiverId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Select(r => new RecentBloodRequestItem
                {
                    RequestId = r.RequestId,
                    ReceiverName = user.FullName,
                    BloodGroup = r.BloodGroup ?? "N/A",
                    UnitsRequired = r.UnitsRequired ?? 0,
                    HospitalName = r.HospitalName ?? "N/A",
                    City = r.City ?? "N/A",
                    UrgencyLevel = r.UrgencyLevel ?? "Normal",
                    Status = r.RequestStatus ?? "Pending",
                    CreatedDate = r.CreatedAt
                })
                .ToListAsync();

            vm.ActiveEmergencyRequests = await _context.BloodRequests
                .Where(r => r.ReceiverId == userId && r.UrgencyLevel == "Critical" && r.RequestStatus != "Fulfilled")
                .OrderByDescending(r => r.CreatedAt)
                .Take(3)
                .Select(r => new RecentEmergencyAlertItem
                {
                    AlertId = r.RequestId,
                    BloodGroup = r.BloodGroup ?? "N/A",
                    HospitalName = r.HospitalName ?? "N/A",
                    City = r.City ?? "N/A",
                    UrgencyLevel = r.UrgencyLevel ?? "Critical",
                    CreatedDate = r.CreatedAt
                })
                .ToListAsync();

            vm.RecentNotifications = await _context.NotificationLogs
                .Where(n => n.RecipientEmail == user.Email)
                .OrderByDescending(n => n.SentAt)
                .Take(5)
                .Select(n => new RecentNotificationItem
                {
                    LogId = n.LogId,
                    Category = n.Category,
                    Subject = n.Subject ?? "System Notification",
                    Status = n.Status,
                    SentAt = n.SentAt
                })
                .ToListAsync();

            vm.UnreadNotificationCount = await _context.NotificationLogs.CountAsync(n => n.RecipientEmail == user.Email && n.Status == "Pending");

            ViewData["Title"] = "Receiver Dashboard";
            return View(vm);
        }

        private List<string> GetCompatibleDonorGroups(string receiverGroup)
        {
            return receiverGroup switch
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

        [HttpGet]
        public async Task<IActionResult> MyBloodRequests([FromQuery] int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (page < 1) page = 1;
            const int pageSize = 10;

            var baseQuery = _context.BloodRequests.Where(r => r.ReceiverId == userId);

            var totalCount = await baseQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (page > totalPages) page = totalPages > 0 ? totalPages : 1;

            var requests = await baseQuery
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReceiverBloodRequestItem
                {
                    RequestId = r.RequestId,
                    PatientName = r.PatientName,
                    BloodGroup = r.BloodGroup,
                    UnitsRequired = r.UnitsRequired ?? 0,
                    HospitalName = r.HospitalName,
                    City = r.City,
                    UrgencyLevel = r.UrgencyLevel,
                    RequestStatus = r.RequestStatus,
                    CreatedDate = r.CreatedAt,
                    RequiredDate = r.RequiredDate,
                    HospitalAddress = r.Address ?? r.City,
                    Notes = "",
                    MatchedDonorsCount = _context.DonorMatches.Count(m => m.BloodRequestId == r.RequestId)
                })
                .ToListAsync();

            var vm = new ReceiverBloodRequestViewModel
            {
                Requests = requests,
                TotalCount = totalCount,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                PendingCount = await baseQuery.CountAsync(r => r.RequestStatus == "Pending"),
                FulfilledCount = await baseQuery.CountAsync(r => r.RequestStatus == "Fulfilled"),
                EmergencyCount = await baseQuery.CountAsync(r => r.UrgencyLevel == "Critical" && r.RequestStatus != "Fulfilled"),
                ActiveCitiesCount = await baseQuery.Where(r => r.RequestStatus != "Cancelled" && r.RequestStatus != "Fulfilled")
                                                    .Select(r => r.City)
                                                    .Distinct()
                                                    .CountAsync(),
                AvailableCities = await baseQuery.Select(r => r.City)
                                                 .Where(c => !string.IsNullOrEmpty(c))
                                                 .Distinct()
                                                 .OrderBy(c => c)
                                                 .ToListAsync()
            };

            ViewData["Title"] = "My Blood Requests";
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetRequestDetails(int requestId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var request = await _context.BloodRequests
                .Where(r => r.RequestId == requestId && r.ReceiverId == userId)
                .Select(r => new
                {
                    r.RequestId,
                    PatientName = r.PatientName,
                    r.BloodGroup,
                    r.UnitsRequired,
                    r.HospitalName,
                    r.City,
                    r.UrgencyLevel,
                    r.RequestStatus,
                    r.CreatedAt,
                    MatchedDonorsCount = _context.DonorMatches.Count(m => m.BloodRequestId == r.RequestId)
                })
                .FirstOrDefaultAsync();

            if (request == null) return NotFound();
            return Json(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(int requestId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var request = await _context.BloodRequests.FirstOrDefaultAsync(r => r.RequestId == requestId && r.ReceiverId == userId);

            if (request == null) return Json(new { success = false, message = "Request not found." });
            if (request.RequestStatus != "Pending") return Json(new { success = false, message = "Only pending requests can be cancelled." });

            request.RequestStatus = "Cancelled";
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Request cancelled successfully." });
        }

        [HttpGet]
        public async Task<IActionResult> CreateBloodRequest()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);

            var vm = new ReceiverCreateBloodRequestViewModel
            {
                RequiredDate = DateTime.Today,
                UrgencyLevel = "Normal",
                ReceiverName = user?.FullName ?? "N/A",
                ReceiverEmail = user?.Email ?? "N/A",
                ReceiverStatus = "Verified Receiver"
            };

            ViewData["Title"] = "Create Blood Request";
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> SearchHospitals(string term, double? userLat, double? userLng)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 Search Term: '{term}'");

                var query = _context.HospitalProfiles
                    .Where(h => h.VerificationStatus == "Approved" ||
                               h.VerificationStatus == "Verified" ||
                               h.IsVerified == true)
                    .Where(h => h.HospitalName != null && h.HospitalName.Trim() != "");

                if (!string.IsNullOrWhiteSpace(term))
                {
                    // ✅ CORRECT WAY - Case-insensitive search
                    query = query.Where(h => h.HospitalName.ToLower().Contains(term.ToLower()));

                    System.Diagnostics.Debug.WriteLine($"📝 Filtering by term: {term}");
                }

                var hospitals = await query.Take(20).ToListAsync();
                System.Diagnostics.Debug.WriteLine($"✅ Found {hospitals.Count} hospitals");

                // Debug: Print hospital names
                foreach (var h in hospitals)
                {
                    System.Diagnostics.Debug.WriteLine($"   - {h.HospitalName}");
                }

                var results = hospitals.Select(h => new
                {
                    h.HospitalId,
                    HospitalName = h.HospitalName ?? "Unknown Hospital",
                    h.Address,
                    h.City,
                    Contact = h.ContactPerson ?? "N/A",
                    h.Latitude,
                    h.Longitude,
                    Distance = (userLat.HasValue && userLng.HasValue && h.Latitude.HasValue && h.Longitude.HasValue)
                               ? CalculateDistance(userLat.Value, userLng.Value, h.Latitude.Value, h.Longitude.Value)
                               : (double?)null,
                    EstimatedTimeMinutes = (userLat.HasValue && userLng.HasValue && h.Latitude.HasValue && h.Longitude.HasValue)
                               ? (int)Math.Ceiling(CalculateDistance(userLat.Value, userLng.Value, h.Latitude.Value, h.Longitude.Value) * 2.5)
                               : (int?)null
                })
                .OrderBy(h => h.Distance ?? 9999)
                .Take(10)
                .ToList();

                return Json(results);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error: {ex.Message}");
                return Json(new List<object>());
            }
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return Math.Round(R * c, 1);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBloodRequest(ReceiverCreateBloodRequestViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var receiverProfile = await _context.ReceiverProfiles.FirstOrDefaultAsync(r => r.UserId == userId);

                if (receiverProfile == null)
                {
                    return Json(new { success = false, message = "Receiver profile not found." });
                }

                // ✅ REMOVE OR COMMENT THIS VALIDATION (if exists):
                // if (!string.IsNullOrEmpty(model.HospitalContact) && !Regex.IsMatch(model.HospitalContact, @"^03\d{9}$"))
                // {
                //     return Json(new { success = false, message = "Invalid contact number format" });
                // }

                var bloodRequest = new BloodRequest
                {
                    ReceiverId = userId,
                    BloodGroup = model.BloodGroup,
                    UnitsRequired = model.UnitsRequired,
                    HospitalName = model.HospitalName,
                    City = model.City,
                    UrgencyLevel = model.UrgencyLevel,
                    RequestStatus = "Pending",
                    CreatedAt = DateTime.Now,
                    RequiredDate = model.RequiredDate,
                    HospitalContact = model.HospitalContact, // ✅ Ab yeh optional hai
                    PatientName = model.PatientName,
                    PatientAge = model.PatientAge,
                    Address = model.Address,
                    Reason = model.Reason,
                    AdditionalNotes = model.AdditionalNotes,
                    Latitude = model.Latitude,
                    Longitude = model.Longitude,
                    HospitalId = model.SelectedHospitalId
                };

                _context.BloodRequests.Add(bloodRequest);
                await _context.SaveChangesAsync();

                if (model.IsEmergency)
                {
                    var emergencyAlert = new EmergencyAlert
                    {
                        RequestId = bloodRequest.RequestId,
                        AlertMessage = $"Emergency blood requirement for {model.BloodGroup}. Reason: {model.Reason}",
                        PriorityLevel = "Critical",
                        AlertStatus = "Active",
                        CreatedAt = DateTime.Now
                    };
                    _context.EmergencyAlerts.Add(emergencyAlert);
                    await _context.SaveChangesAsync();
                }

                return Json(new
                {
                    success = true,
                    message = "Blood request created successfully!",
                    requestId = bloodRequest.RequestId
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft([FromBody] JsonElement model)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                string GetString(string prop) => model.TryGetProperty(prop, out var p) ? p.GetString() : null;

                int? GetInt(string prop)
                {
                    if (model.TryGetProperty(prop, out var p))
                    {
                        if (p.ValueKind == JsonValueKind.Number) return p.GetInt32();
                        if (p.ValueKind == JsonValueKind.String)
                        {
                            var str = p.GetString();
                            if (!string.IsNullOrEmpty(str) && int.TryParse(str, out var result)) return result;
                        }
                    }
                    return null;
                }

                bool GetBool(string prop)
                {
                    if (model.TryGetProperty(prop, out var p))
                    {
                        if (p.ValueKind == JsonValueKind.True) return true;
                        if (p.ValueKind == JsonValueKind.False) return false;
                        if (p.ValueKind == JsonValueKind.String)
                        {
                            var str = p.GetString();
                            return str?.ToLower() == "true" || str?.ToLower() == "on";
                        }
                    }
                    return false;
                }

                decimal? GetDecimal(string prop)
                {
                    if (model.TryGetProperty(prop, out var p))
                    {
                        if (p.ValueKind == JsonValueKind.Number) return p.GetDecimal();
                        if (p.ValueKind == JsonValueKind.String)
                        {
                            var str = p.GetString();
                            if (!string.IsNullOrEmpty(str) && decimal.TryParse(str, out var result)) return result;
                        }
                    }
                    return null;
                }

                var draftData = new
                {
                    PatientName = GetString("PatientName"),
                    PatientAge = GetInt("PatientAge"),
                    Gender = GetString("Gender"),
                    BloodGroup = GetString("BloodGroup"),
                    UnitsRequired = GetInt("UnitsRequired"),
                    HospitalName = GetString("HospitalName"),
                    HospitalContact = GetString("HospitalContact"),
                    Address = GetString("Address"),
                    City = GetString("City"),
                    RequiredDate = GetString("RequiredDate"),
                    UrgencyLevel = GetString("UrgencyLevel"),
                    Reason = GetString("Reason"),
                    AdditionalNotes = GetString("AdditionalNotes"),
                    Area = GetString("Area"),
                    FullAddress = GetString("FullAddress"),
                    IsEmergency = GetBool("IsEmergency"),
                    Latitude = GetDecimal("Latitude"),
                    Longitude = GetDecimal("Longitude"),
                    SavedAt = DateTime.Now
                };

                var existingDraft = await _context.BloodRequestDrafts
                    .FirstOrDefaultAsync(d => d.UserId == userId && d.DraftType == "ReceiverRequest");

                if (existingDraft == null)
                {
                    var newDraft = new BloodRequestDraft
                    {
                        UserId = userId,
                        DraftType = "ReceiverRequest",
                        DraftData = System.Text.Json.JsonSerializer.Serialize(draftData),
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _context.BloodRequestDrafts.Add(newDraft);
                }
                else
                {
                    existingDraft.DraftData = System.Text.Json.JsonSerializer.Serialize(draftData);
                    existingDraft.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Draft saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to save draft: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult BloodCompatibility()
        {
            var vm = new DonorBloodCompatibilityViewModel
            {
                BloodGroups = new List<BloodGroupCompatibilityItem>
                {
                    new BloodGroupCompatibilityItem { BloodGroup = "O-", CanDonateTo = new List<string>{"O-","O+","A-","A+","B-","B+","AB-","AB+"}, CanReceiveFrom = new List<string>{"O-"}, ColorTheme = "danger", SpecialRole = "Universal Donor" },
                    new BloodGroupCompatibilityItem { BloodGroup = "O+", CanDonateTo = new List<string>{"O+","A+","B+","AB+"}, CanReceiveFrom = new List<string>{"O-","O+"}, ColorTheme = "danger", SpecialRole = "" },
                    new BloodGroupCompatibilityItem { BloodGroup = "A-", CanDonateTo = new List<string>{"A-","A+","AB-","AB+"}, CanReceiveFrom = new List<string>{"O-","A-"}, ColorTheme = "primary", SpecialRole = "" },
                    new BloodGroupCompatibilityItem { BloodGroup = "A+", CanDonateTo = new List<string>{"A+","AB+"}, CanReceiveFrom = new List<string>{"O-","O+","A-","A+"}, ColorTheme = "primary", SpecialRole = "" },
                    new BloodGroupCompatibilityItem { BloodGroup = "B-", CanDonateTo = new List<string>{"B-","B+","AB-","AB+"}, CanReceiveFrom = new List<string>{"O-","B-"}, ColorTheme = "success", SpecialRole = "" },
                    new BloodGroupCompatibilityItem { BloodGroup = "B+", CanDonateTo = new List<string>{"B+","AB+"}, CanReceiveFrom = new List<string>{"O-","O+","B-","B+"}, ColorTheme = "success", SpecialRole = "" },
                    new BloodGroupCompatibilityItem { BloodGroup = "AB-", CanDonateTo = new List<string>{"AB-","AB+"}, CanReceiveFrom = new List<string>{"O-","A-","B-","AB-"}, ColorTheme = "info", SpecialRole = "" },
                    new BloodGroupCompatibilityItem { BloodGroup = "AB+", CanDonateTo = new List<string>{"AB+"}, CanReceiveFrom = new List<string>{"O-","O+","A-","A+","B-","B+","AB-","AB+"}, ColorTheme = "info", SpecialRole = "Universal Receiver" }
                }
            };

            ViewData["Title"] = "Blood Compatibility";
            return View(vm);
        }

        // ============== TRACK REQUESTS MODULE ==============
        [HttpGet]
        public async Task<IActionResult> TrackRequests([FromQuery] TrackingFiltersViewModel filters)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            filters ??= new TrackingFiltersViewModel();

            var baseQuery = _context.BloodRequests.Where(r => r.ReceiverId == userId);

            // Apply filters
            var filteredQuery = baseQuery.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filters.SearchQuery))
            {
                var s = filters.SearchQuery.Trim().ToLower();
                filteredQuery = filteredQuery.Where(r =>
                    (r.PatientName != null && r.PatientName.ToLower().Contains(s)) ||
                    r.RequestId.ToString().Contains(filters.SearchQuery.Trim()) ||
                    (r.HospitalName != null && r.HospitalName.ToLower().Contains(s)) ||
                    (r.BloodGroup != null && r.BloodGroup.ToLower().Contains(s)) ||
                    (r.RequestStatus != null && r.RequestStatus.ToLower().Contains(s)) ||
                    (r.UrgencyLevel != null && r.UrgencyLevel.ToLower().Contains(s))
                );
            }
            if (!string.IsNullOrWhiteSpace(filters.BloodGroup) && filters.BloodGroup != "all")
                filteredQuery = filteredQuery.Where(r => r.BloodGroup == filters.BloodGroup);
            if (!string.IsNullOrWhiteSpace(filters.Status) && filters.Status != "all")
                filteredQuery = filteredQuery.Where(r => r.RequestStatus == filters.Status);
            if (!string.IsNullOrWhiteSpace(filters.Priority) && filters.Priority != "all")
                filteredQuery = filteredQuery.Where(r => r.UrgencyLevel == filters.Priority);
            if (!string.IsNullOrWhiteSpace(filters.Hospital) && filters.Hospital != "all")
                filteredQuery = filteredQuery.Where(r => r.HospitalName == filters.Hospital);
            if (filters.DateFrom.HasValue)
                filteredQuery = filteredQuery.Where(r => r.CreatedAt >= filters.DateFrom.Value);
            if (filters.DateTo.HasValue)
                filteredQuery = filteredQuery.Where(r => r.CreatedAt <= filters.DateTo.Value.AddDays(1).Date);

            // Statistics
            var stats = new TrackingStatisticsViewModel
            {
                ActiveRequests = await baseQuery.CountAsync(r =>
                    r.RequestStatus == "Matched" || r.RequestStatus == "Accepted" || r.RequestStatus == "Pending"),
                CompletedRequests = await baseQuery.CountAsync(r =>
                    r.RequestStatus == "Fulfilled" || r.RequestStatus == "Completed"),
                PendingRequests = await baseQuery.CountAsync(r => r.RequestStatus == "Pending"),
                AverageProcessingTime = await CalculateAverageProcessingTime(userId)
            };

            // Load filtered requests
            var requests = await filteredQuery
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Bulk-load donor match counts (avoid N+1)
            var requestIds = requests.Select(r => r.RequestId).ToList();
            var donorMatchCounts = requestIds.Any()
                ? await _context.DonorMatches
                    .Where(m => requestIds.Contains(m.BloodRequestId))
                    .GroupBy(m => m.BloodRequestId)
                    .Select(g => new { RequestId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.RequestId, x => x.Count)
                : new Dictionary<int, int>();

            // Build cards
            var cards = requests.Select(r => BuildTrackingCard(r, donorMatchCounts.GetValueOrDefault(r.RequestId, 0))).ToList();

            var vm = new ReceiverTrackRequestsViewModel
            {
                Statistics = stats,
                Filters = filters,
                Requests = cards,
                TotalCount = cards.Count,
                AvailableBloodGroups = await baseQuery.Select(r => r.BloodGroup).Where(b => b != null).Distinct().OrderBy(b => b).ToListAsync(),
                AvailableCities = await baseQuery.Select(r => r.City).Where(c => c != null).Distinct().OrderBy(c => c).ToListAsync(),
                AvailableHospitals = await baseQuery.Select(r => r.HospitalName).Where(h => h != null).Distinct().OrderBy(h => h).ToListAsync()
            };

            ViewData["Title"] = "Track Requests";
            return View(vm);
        }

        private async Task<string> CalculateAverageProcessingTime(int userId)
        {
            var recentFulfilled = await _context.BloodRequests
                .Where(r => r.ReceiverId == userId && (r.RequestStatus == "Fulfilled" || r.RequestStatus == "Completed"))
                .OrderByDescending(r => r.CreatedAt)
                .Take(20)
                .ToListAsync();

            if (recentFulfilled.Count < 2) return "N/A";

            var avgHours = recentFulfilled.Average(r =>
            {
                var urgencyMultiplier = r.UrgencyLevel switch
                {
                    "Critical" => 0.5,
                    "High" => 0.75,
                    _ => 1.0
                };
                return 6 * urgencyMultiplier;
            });

            if (avgHours < 1) return $"{(int)(avgHours * 60)} Minutes";
            if (avgHours < 24) return $"{avgHours:F1} Hours";
            return $"{(avgHours / 24):F1} Days";
        }

        private TrackingCardViewModel BuildTrackingCard(BloodRequest r, int donorCount)
        {
            var (displayStatus, progress, colorClass, currentStage) = MapStatus(r.RequestStatus, donorCount);

            return new TrackingCardViewModel
            {
                RequestId = r.RequestId,
                RequestCode = $"REQ-{r.RequestId:D4}",
                PatientName = r.PatientName ?? "N/A",
                PatientAge = r.PatientAge,
                Gender = "N/A", // FIXED: Gender doesn't exist in database
                BloodGroup = r.BloodGroup ?? "N/A",
                UnitsRequired = r.UnitsRequired ?? 0,
                HospitalName = r.HospitalName ?? "N/A",
                HospitalCity = r.City ?? "N/A",
                HospitalAddress = r.Address ?? "N/A",
                HospitalContact = r.HospitalContact,
                OriginalStatus = r.RequestStatus ?? "Pending", // FIXED: Use RequestStatus
                DisplayStatus = displayStatus,
                StatusColorClass = colorClass,
                ProgressPercentage = progress,
                UrgencyLevel = r.UrgencyLevel ?? "Normal",
                Reason = r.Reason ?? "Not specified",
                CreatedAt = r.CreatedAt,
                RequiredDate = r.RequiredDate,
                EstimatedCompletion = CalculateEstimatedCompletion(r, progress),
                LastUpdatedText = FormatTimeAgo(r.CreatedAt.AddHours(donorCount > 0 ? 3 : 1)),
                IsLive = r.RequestStatus != "Fulfilled" && r.RequestStatus != "Completed" && r.RequestStatus != "Cancelled" && r.RequestStatus != "Rejected",
                CurrentStage = currentStage,
                MatchedDonorsCount = donorCount,
                Timeline = BuildTimeline(r, donorCount),
                ActivityLogs = BuildActivityLogs(r, donorCount)
            };
        }

        private (string displayStatus, int progress, string colorClass, string currentStage) MapStatus(string status, int donorCount)
        {
            return status switch
            {
                "Pending" => ("Under Review", 20, "status-under-review", "Hospital Review"),
                "Approved" => ("Approved", 50, "status-approved", "Request Approved"),
                "Blood Issued" => ("Ready for Collection", 85, "status-reserved", "Blood Issued"), // NEW
                "Matched" when donorCount == 0 => ("Searching Donors", 50, "status-searching", "Searching Donors"),
                "Matched" when donorCount > 0 => ("Donor Accepted", 65, "status-accepted", "Donor Matched"),
                "Accepted" => ("Blood Reserved", 80, "status-reserved", "Blood Reserved"),
                "Fulfilled" or "Completed" => ("Completed", 100, "status-completed", "Completed"),
                "Cancelled" => ("Cancelled", 0, "status-cancelled", "Cancelled"),
                "Rejected" => ("Rejected", 0, "status-rejected", "Rejected"),
                _ => ("Submitted", 10, "status-submitted", "Submitted")
            };
        }

        private string CalculateEstimatedCompletion(BloodRequest r, int progress)
        {
            if (progress >= 100) return "Completed";
            if (r.RequestStatus == "Cancelled" || r.RequestStatus == "Rejected") return "N/A";

            if (r.RequiredDate.HasValue)
                return r.RequiredDate.Value.ToString("dd MMM yyyy, h:mm tt");

            var hours = r.UrgencyLevel switch
            {
                "Critical" => 2,
                "High" => 6,
                _ => 24
            };

            var estimated = r.CreatedAt.AddHours(hours);
            var remaining = estimated - DateTime.Now;
            if (remaining.TotalHours <= 0) return "Any moment now";
            if (remaining.TotalHours < 1) return $"Approx {(int)remaining.TotalMinutes} min";
            if (remaining.TotalHours < 24) return $"Approx {remaining.TotalHours:F1} Hours";
            return $"Approx {(int)remaining.TotalDays} Day(s)";
        }

        private string FormatTimeAgo(DateTime date)
        {
            var span = DateTime.Now - date;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} hr ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} day(s) ago";
            return date.ToString("dd MMM yyyy");
        }

        private List<TimelineStageViewModel> BuildTimeline(BloodRequest r, int donorCount)
        {
            var now = DateTime.Now;
            var created = r.CreatedAt;
            var status = r.RequestStatus; // FIXED: Use RequestStatus instead of OriginalStatus

            // BuildTimeline method ke andar stages list mein ye add/update karein:
            var stages = new List<(string title, string desc, string icon, DateTime? date, bool completed, bool isCurrent)>
{
    ("Request Submitted", "Your blood request has been received", "bi-check-circle-fill", created, true, status == "Pending"),
    ("Hospital Approved", "Hospital has approved the request", "bi-clipboard-check", created.AddHours(1), new[]{"Approved", "Blood Issued", "Fulfilled", "Completed"}.Contains(status), status == "Approved"),
    ("Blood Issued", "Blood has been issued from hospital inventory", "bi-box-seam", null, new[]{"Blood Issued", "Fulfilled", "Completed"}.Contains(status), status == "Blood Issued"), // NEW STAGE
    ("Ready for Pickup", "Blood is ready for pickup/delivery", "bi-truck", null, new[]{"Blood Issued", "Fulfilled", "Completed"}.Contains(status), false),
    ("Completed", "Request successfully fulfilled", "bi-check2-all", new[]{"Fulfilled", "Completed"}.Contains(status) ? DateTime.Now : null, new[]{"Fulfilled", "Completed"}.Contains(status), false)
};

            var timeline = new List<TimelineStageViewModel>();
            bool currentFound = false;

            foreach (var s in stages)
            {
                string stageStatus;
                if (s.isCurrent && !currentFound) { stageStatus = "current"; currentFound = true; }
                else if (s.completed && !currentFound) stageStatus = "completed";
                else stageStatus = "pending";

                timeline.Add(new TimelineStageViewModel
                {
                    Title = s.title,
                    Description = s.desc,
                    Icon = s.icon,
                    CompletedAt = s.completed ? s.date : null,
                    Status = stageStatus
                });
            }
            return timeline;
        }

        private List<ActivityLogViewModel> BuildActivityLogs(BloodRequest r, int donorCount)
        {
            var logs = new List<ActivityLogViewModel>();
            var c = r.CreatedAt;
            var status = r.RequestStatus; // FIXED: Use RequestStatus

            logs.Add(new ActivityLogViewModel { Timestamp = c, Description = "Your blood request has been submitted successfully.", Icon = "bi-plus-circle-fill", IconColor = "#5C88A8" });

            if (status != "Pending")
                logs.Add(new ActivityLogViewModel { Timestamp = c.AddHours(1), Description = "Hospital has reviewed and approved your request.", Icon = "bi-hospital", IconColor = "#198754" });

            if (new[] { "Matched", "Accepted", "Fulfilled", "Completed" }.Contains(status))
                logs.Add(new ActivityLogViewModel { Timestamp = c.AddHours(2), Description = "Nearby compatible donors have been notified via SMS and app alerts.", Icon = "bi-bell-fill", IconColor = "#0dcaf0" });

            if (donorCount > 0)
                logs.Add(new ActivityLogViewModel { Timestamp = c.AddHours(4), Description = $"{donorCount} donor(s) accepted your request.", Icon = "bi-person-check-fill", IconColor = "#198754" });

            if (new[] { "Accepted", "Fulfilled", "Completed" }.Contains(status))
                logs.Add(new ActivityLogViewModel { Timestamp = c.AddHours(5), Description = "Blood units have been reserved at the hospital.", Icon = "bi-droplet-fill", IconColor = "#90151C" });

            if (new[] { "Fulfilled", "Completed" }.Contains(status))
                logs.Add(new ActivityLogViewModel { Timestamp = c.AddHours(8), Description = "Blood request has been successfully completed.", Icon = "bi-check-circle-fill", IconColor = "#198754" });

            if (status == "Cancelled")
                logs.Add(new ActivityLogViewModel { Timestamp = DateTime.Now, Description = "Request has been cancelled.", Icon = "bi-x-circle-fill", IconColor = "#6c757d" });

            if (status == "Rejected")
                logs.Add(new ActivityLogViewModel { Timestamp = DateTime.Now, Description = "Request has been rejected by the hospital.", Icon = "bi-x-circle-fill", IconColor = "#90151C" });

            return logs.OrderByDescending(l => l.Timestamp)
                       .Select(l => { l.TimeAgo = FormatTimeAgo(l.Timestamp); return l; })
                       .Take(5).ToList();
        }

        // ============== NOTIFICATIONS MODULE ==============
        [HttpGet]
        public async Task<IActionResult> Notifications([FromQuery] NotificationFiltersViewModel filters, [FromQuery] int page = 1)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login", "Account");

            filters ??= new NotificationFiltersViewModel();
            if (page < 1) page = 1;
            const int pageSize = 10;

            var baseQuery = _context.ReceiverNotifications.Where(n => n.ReceiverId == userId);

            // Apply filters
            var filteredQuery = baseQuery.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filters.SearchQuery))
            {
                var s = filters.SearchQuery.Trim().ToLower();
                filteredQuery = filteredQuery.Where(n =>
                    (n.Title != null && n.Title.ToLower().Contains(s)) ||
                    (n.Message != null && n.Message.ToLower().Contains(s)) ||
                    (n.HospitalName != null && n.HospitalName.ToLower().Contains(s)) ||
                    (n.BloodGroup != null && n.BloodGroup.ToLower().Contains(s)) ||
                    (n.Category != null && n.Category.ToLower().Contains(s)) ||
                    (n.RequestId.HasValue && n.RequestId.Value.ToString().Contains(filters.SearchQuery.Trim()))
                );
            }

            if (filters.ReadStatus == "unread")
                filteredQuery = filteredQuery.Where(n => !n.IsRead);
            else if (filters.ReadStatus == "read")
                filteredQuery = filteredQuery.Where(n => n.IsRead);

            if (!string.IsNullOrWhiteSpace(filters.Category) && filters.Category != "all")
                filteredQuery = filteredQuery.Where(n => n.Category == filters.Category);

            if (!string.IsNullOrWhiteSpace(filters.Priority) && filters.Priority != "all")
                filteredQuery = filteredQuery.Where(n => n.Priority == filters.Priority);

            // Ordering
            filteredQuery = filters.SortOrder == "oldest"
                ? filteredQuery.OrderBy(n => n.CreatedDate)
                : filteredQuery.OrderByDescending(n => n.CreatedDate);

            // Statistics (always from baseQuery, not filtered)
            var today = DateTime.Today;
            var stats = new NotificationStatisticsViewModel
            {
                TotalNotifications = await baseQuery.CountAsync(),
                UnreadNotifications = await baseQuery.CountAsync(n => !n.IsRead),
                TodayNotifications = await baseQuery.CountAsync(n => n.CreatedDate >= today),
                ImportantAlerts = await baseQuery.CountAsync(n => n.Priority == "High" || n.Category == "Emergency")
            };

            // Pagination
            var totalCount = await filteredQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (page > totalPages) page = totalPages > 0 ? totalPages : 1;

            var notifications = await filteredQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationItemViewModel
                {
                    NotificationId = n.NotificationId,
                    Title = n.Title,
                    Message = n.Message,
                    Category = n.Category,
                    Priority = n.Priority ?? "Medium",
                    RequestId = n.RequestId,
                    HospitalName = n.HospitalName,
                    BloodGroup = n.BloodGroup,
                    RequestStatus = n.RequestStatus,
                    ActionUrl = n.ActionUrl,
                    IsRead = n.IsRead,
                    CreatedDate = n.CreatedDate
                })
                .ToListAsync();

            // Map category icons/colors in memory (small list)
            foreach (var n in notifications)
            {
                var (icon, color) = GetCategoryStyle(n.Category);
                n.CategoryIcon = icon;
                n.CategoryColor = color;
                n.RequestCode = n.RequestId.HasValue ? $"REQ-{n.RequestId.Value:D4}" : null;
                n.TimeAgo = FormatTimeAgo(n.CreatedDate);
                n.FormattedDate = n.CreatedDate.ToString("dd MMM yyyy");
                n.FormattedTime = n.CreatedDate.ToString("h:mm tt");
            }

            var vm = new ReceiverNotificationsViewModel
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

        // AJAX: Get fresh notifications + stats (for real-time polling)
        [HttpGet]
        public async Task<IActionResult> GetNotificationsLive([FromQuery] NotificationFiltersViewModel filters)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            filters ??= new NotificationFiltersViewModel();

            var baseQuery = _context.ReceiverNotifications.Where(n => n.ReceiverId == userId);
            var today = DateTime.Today;

            var stats = new
            {
                total = await baseQuery.CountAsync(),
                unread = await baseQuery.CountAsync(n => !n.IsRead),
                today = await baseQuery.CountAsync(n => n.CreatedDate >= today),
                important = await baseQuery.CountAsync(n => n.Priority == "High" || n.Category == "Emergency")
            };

            return Json(new { success = true, data = stats });
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadNotificationCount()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var unreadCount = await _context.ReceiverNotifications
                .CountAsync(n => n.ReceiverId == userId && !n.IsRead);

            return Json(new { unread = unreadCount });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var notif = await _context.ReceiverNotifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.ReceiverId == userId);
            if (notif == null) return Json(new { success = false, message = "Notification not found." });

            notif.IsRead = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Marked as read." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsUnread(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var notif = await _context.ReceiverNotifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.ReceiverId == userId);
            if (notif == null) return Json(new { success = false, message = "Notification not found." });

            notif.IsRead = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Marked as unread." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var unread = await _context.ReceiverNotifications
                .Where(n => n.ReceiverId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread) n.IsRead = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Marked {unread.Count} notifications as read." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var notif = await _context.ReceiverNotifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.ReceiverId == userId);
            if (notif == null) return Json(new { success = false, message = "Notification not found." });

            _context.ReceiverNotifications.Remove(notif);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Notification deleted." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAction(string action, [FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
                return Json(new { success = false, message = "No notifications selected." });

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var notifications = await _context.ReceiverNotifications
                .Where(n => ids.Contains(n.NotificationId) && n.ReceiverId == userId)
                .ToListAsync();

            if (!notifications.Any())
                return Json(new { success = false, message = "No valid notifications found." });

            if (action == "markread")
            {
                foreach (var n in notifications) n.IsRead = true;
            }
            else if (action == "markunread")
            {
                foreach (var n in notifications) n.IsRead = false;
            }
            else if (action == "delete")
            {
                _context.ReceiverNotifications.RemoveRange(notifications);
            }
            else
            {
                return Json(new { success = false, message = "Invalid action." });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Action '{action}' applied to {notifications.Count} notification(s)." });
        }

        // Helper: Map category to icon + color
        private (string icon, string color) GetCategoryStyle(string category)
        {
            return category switch
            {
                "BloodRequest" => ("bi-droplet-fill", "#90151C"),
                "Hospital" => ("bi-hospital", "#5C88A8"),
                "Donor" => ("bi-person-check", "#198754"),
                "BloodReserved" => ("bi-box-seam", "#0dcaf0"),
                "BloodReady" => ("bi-check-circle", "#198754"),
                "Completed" => ("bi-check2-all", "#198754"),
                "Rejected" => ("bi-x-circle", "#90151C"),
                "Emergency" => ("bi-exclamation-triangle", "#90151C"),
                "Reminder" => ("bi-alarm", "#0dcaf0"),
                "Information" => ("bi-info-circle", "#5C88A8"),
                "System" => ("bi-gear", "#6c757d"),
                "Success" => ("bi-patch-check", "#198754"),
                _ => ("bi-bell", "#5C88A8")
            };
        }

        // GET: Receiver/GetLatestNotifications
        [HttpGet]
        public async Task<IActionResult> GetLatestNotifications()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                System.Diagnostics.Debug.WriteLine($"=== GetLatestNotifications Called ===");
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

                var totalCount = await _context.ReceiverNotifications.CountAsync(n => n.ReceiverId == userId);
                System.Diagnostics.Debug.WriteLine($"Total notifications in DB: {totalCount}");

                var notifications = await _context.ReceiverNotifications
                    .Where(n => n.ReceiverId == userId)
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(5)
                    .Select(n => new
                    {
                        n.NotificationId,
                        n.Title,
                        n.Message,
                        n.Category,
                        n.IsRead,
                        n.CreatedDate,
                        n.RequestId,
                        ActionUrl = n.ActionUrl ?? (n.RequestId.HasValue ? $"/Receiver/TrackRequests" : null),
                        TimeAgo = GetTimeAgo(n.CreatedDate)
                    })
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"Notifications returned: {notifications.Count}");

                var unreadCount = await _context.ReceiverNotifications
                    .CountAsync(n => n.ReceiverId == userId && !n.IsRead);

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
    }
}
