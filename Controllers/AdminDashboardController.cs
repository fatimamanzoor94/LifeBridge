using Khoon_e_Hayat.Data;
using Khoon_e_Hayat.Models; 
using Khoon_e_Hayat.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Khoon_e_Hayat.Controllers
{
    public class AdminDashboardController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel();

            // ==================== SUMMARY CARDS ====================
            vm.TotalUsers = await _context.Users.CountAsync();
            vm.TotalDonors = await _context.Users.CountAsync(u => u.Role == "Donor");
            vm.TotalReceivers = await _context.Users.CountAsync(u => u.Role == "Receiver");
            vm.TotalHospitals = await _context.Users.CountAsync(u => u.Role == "Hospital");

            vm.PendingHospitalApprovals = await _context.HospitalProfiles
                .CountAsync(h => h.VerificationStatus == "Pending");

            vm.ActiveBloodRequests = await _context.BloodRequests
                .CountAsync(r => r.RequestStatus == "Pending");

            vm.EmergencyRequests = await _context.BloodRequests
                .CountAsync(r => r.UrgencyLevel == "Critical");

            vm.ContactMessages = await _context.ContactMessages.CountAsync();

            // ==================== CHARTS DATA ====================

            // 1. User Distribution
            vm.UserDistributionData = new List<ChartData>
            {
                new ChartData { Label = "Donors", Value = vm.TotalDonors, Color = "#0d6efd" },
                new ChartData { Label = "Receivers", Value = vm.TotalReceivers, Color = "#198754" },
                new ChartData { Label = "Hospitals", Value = vm.TotalHospitals, Color = "#ffc107" }
            };

            // 2. Blood Requests By Status
            var statusCounts = await _context.BloodRequests
                .GroupBy(r => r.RequestStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var allStatuses = new[] { "Pending", "Approved", "Fulfilled", "Rejected" };
            vm.BloodRequestsByStatusData = allStatuses.Select(s => new ChartData
            {
                Label = s,
                Value = statusCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0,
                Color = s switch
                {
                    "Pending" => "#ffc107",
                    "Approved" => "#0dcaf0",
                    "Fulfilled" => "#198754",
                    "Rejected" => "#dc3545",
                    _ => "#6c757d"
                }
            }).ToList();

            // 3. Blood Requests By Blood Group
            var bgCounts = await _context.BloodRequests
                .GroupBy(r => r.BloodGroup)
                .Select(g => new { BG = g.Key, Count = g.Count() })
                .ToListAsync();

            var allBloodGroups = new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
            vm.BloodRequestsByBloodGroupData = allBloodGroups.Select(bg => new ChartData
            {
                Label = bg,
                Value = bgCounts.FirstOrDefault(x => x.BG == bg)?.Count ?? 0,
                Color = "#025f67"
            }).ToList();

            // 4. Monthly Registrations (Last 6 Months)
            var sixMonthsAgo = DateTime.Now.AddMonths(-5);
            var startOfMonth = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

            var monthlyRegs = await _context.Users
                .Where(u => u.CreatedAt >= startOfMonth)
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            for (int i = 0; i < 6; i++)
            {
                var date = DateTime.Now.AddMonths(-5 + i);
                var monthData = monthlyRegs.FirstOrDefault(m => m.Year == date.Year && m.Month == date.Month);
                vm.MonthlyRegistrationsData.Add(new MonthlyRegistrationsData
                {
                    Month = date.ToString("MMM yyyy"),
                    Count = monthData?.Count ?? 0
                });
            }

            // ==================== TABLES DATA ====================

            // Recent Blood Requests
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

            // Recent Users
            var recentUsersList = await _context.Users.OrderByDescending(u => u.CreatedAt).Take(5).ToListAsync();
            foreach (var u in recentUsersList)
            {
                string city = "N/A";
                if (u.Role == "Donor") city = await _context.DonorProfiles.Where(d => d.UserId == u.UserId).Select(d => d.City).FirstOrDefaultAsync() ?? "N/A";
                else if (u.Role == "Receiver") city = await _context.ReceiverProfiles.Where(r => r.UserId == u.UserId).Select(r => r.City).FirstOrDefaultAsync() ?? "N/A";
                else if (u.Role == "Hospital") city = await _context.HospitalProfiles.Where(h => h.UserId == u.UserId).Select(h => h.City).FirstOrDefaultAsync() ?? "N/A";

                vm.RecentUsers.Add(new RecentUserItem
                {
                    UserId = u.UserId,
                    Name = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    City = city,
                    Status = u.IsActive ? "Active" : "Inactive",
                    RegistrationDate = u.CreatedAt
                });
            }

            // Pending Hospitals
            vm.PendingHospitals = await (from h in _context.HospitalProfiles
                                         join u in _context.Users on h.UserId equals u.UserId
                                         where h.VerificationStatus == "Pending"
                                         orderby u.CreatedAt descending
                                         select new PendingHospitalItem
                                         {
                                             HospitalId = h.HospitalId,
                                             UserId = h.UserId,
                                             HospitalName = h.HospitalName,
                                             LicenseNumber = h.LicenseNumber,
                                             City = h.City,
                                             ContactPerson = h.ContactPerson,
                                             RegistrationDate = u.CreatedAt
                                         }).Take(5).ToListAsync();

            // Recent Contact Messages
            vm.RecentContactMessages = await _context.ContactMessages
                .OrderByDescending(m => m.CreatedAt)
                .Take(5)
                .Select(m => new RecentContactMessageItem
                {
                    MessageId = m.MessageId,
                    Name = m.FullName,
                    Email = m.Email,
                    Subject = m.Subject,
                    Status = m.Status,
                    Date = m.CreatedAt
                })
                .ToListAsync();

            return View(vm);
        }
    }
}