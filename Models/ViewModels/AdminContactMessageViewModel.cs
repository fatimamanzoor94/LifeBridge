using Khoon_e_Hayat.Models.Entities;
using System;

namespace Khoon_e_Hayat.ViewModels
{
    public class AdminContactMessageViewModel
    {
        public int MessageId { get; set; }
        public string MessageIdDisplay => $"MSG-{MessageId.ToString().PadLeft(4, '0')}";
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }

        public string MessagePreview => string.IsNullOrEmpty(Message) ? "" :
            (Message.Length > 60 ? Message.Substring(0, 60) + "..." : Message);

        public DateTime SentDate { get; set; }
        public string SentDateDisplay => SentDate.ToString("dd MMM yyyy, hh:mm tt");

        public bool IsRead { get; set; }
        public string StatusText => IsRead ? "Read" : "Unread";
        public string StatusBadgeClass => IsRead ? "bg-secondary-subtle text-secondary" : "bg-primary-subtle text-primary";

        // Factory method to map from Entity
        public static AdminContactMessageViewModel FromEntity(ContactMessage entity)
        {
            return new AdminContactMessageViewModel
            {
                MessageId = entity.MessageId,
                FullName = entity.FullName,
                Email = entity.Email,
                Subject = entity.Subject,
                Message = entity.Message,
                SentDate = entity.CreatedAt,
                IsRead = entity.Status != "New" // 'New' means Unread
            };
        }
    }
}
