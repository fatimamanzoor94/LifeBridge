using System;
using System.Collections.Generic;

namespace Khoon_e_Hayat.ViewModels
{
    public class ReceiverNotificationsViewModel
    {
        public NotificationStatisticsViewModel Statistics { get; set; } = new();
        public NotificationFiltersViewModel Filters { get; set; } = new();
        public List<NotificationItemViewModel> Notifications { get; set; } = new();

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;

        public DateTime CurrentDate => DateTime.Now;
    }

    public class NotificationStatisticsViewModel
    {
        public int TotalNotifications { get; set; }
        public int UnreadNotifications { get; set; }
        public int TodayNotifications { get; set; }
        public int ImportantAlerts { get; set; }
    }

    public class NotificationFiltersViewModel
    {
        public string SearchQuery { get; set; }
        public string ReadStatus { get; set; } = "all";       // all, unread, read
        public string Category { get; set; } = "all";
        public string Priority { get; set; } = "all";
        public string SortOrder { get; set; } = "newest";     // newest, oldest
    }

    public class NotificationItemViewModel
    {
        public int NotificationId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Category { get; set; }
        public string CategoryIcon { get; set; }
        public string CategoryColor { get; set; }
        public string Priority { get; set; }
        public int? RequestId { get; set; }
        public string RequestCode { get; set; }
        public string HospitalName { get; set; }
        public string BloodGroup { get; set; }
        public string RequestStatus { get; set; }
        public string ActionUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
        public string TimeAgo { get; set; }
        public string FormattedDate { get; set; }
        public string FormattedTime { get; set; }
    }
}