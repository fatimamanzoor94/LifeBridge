// File: Khoon_e_Hayat/ViewModels/HospitalNotificationsViewModel.cs

using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class HospitalNotificationsViewModel
    {
        public HospitalNotificationStatisticsViewModel Statistics { get; set; } = new();
        public HospitalNotificationFiltersViewModel Filters { get; set; } = new();
        public List<HospitalNotificationItemViewModel> Notifications { get; set; } = new();

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;

        public DateTime CurrentDate => DateTime.Now;
    }

    public class HospitalNotificationStatisticsViewModel
    {
        public int TotalNotifications { get; set; }
        public int UnreadNotifications { get; set; }
        public int ReadNotifications { get; set; }
        public int EmergencyAlerts { get; set; }
        public int InventoryAlerts { get; set; }
    }

    public class HospitalNotificationFiltersViewModel
    {
        public string SearchQuery { get; set; }
        public string ReadStatus { get; set; } = "all";       // all, unread, read
        public string Category { get; set; } = "all";
        public string Priority { get; set; } = "all";
        public string SortOrder { get; set; } = "newest";     // newest, oldest
    }

    public class HospitalNotificationItemViewModel
    {
        public int NotificationId { get; set; }
        public string? Title { get; set; }           // ✅ Nullable
        public string? Message { get; set; }         // ✅ Nullable
        public string? Category { get; set; }        // ✅ Nullable
        public string? CategoryIcon { get; set; }
        public string? CategoryColor { get; set; }
        public string? Priority { get; set; }
        public int? RequestId { get; set; }
        public int? DonorId { get; set; }
        public string RequestCode { get; set; }
        public string ActionUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
        public string TimeAgo { get; set; }
        public string FormattedDate { get; set; }
        public string FormattedTime { get; set; }
    }
}