namespace Khoon_e_Hayat.ViewModels
{
    public class NotificationLogViewModel
    {
        public int LogId { get; set; }
        public string Recipient { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string Subject { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime SentAt { get; set; }
    }
}
