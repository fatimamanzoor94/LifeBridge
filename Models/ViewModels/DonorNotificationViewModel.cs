using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class DonorNotificationViewModel
    {
        public int TotalNotifications { get; set; }
        public int UnreadNotifications { get; set; }
        public int EmergencyAlerts { get; set; }
        public int SmartMatches { get; set; }
        public int Donations { get; set; }

        // Pagination Properties
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        public List<DonorNotificationItem> Notifications { get; set; } = new();
    }

    public class DonorNotificationItem
    {
        public int NotificationId { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ActionUrl { get; set; }
        public int? ReferenceId { get; set; }
    }
}