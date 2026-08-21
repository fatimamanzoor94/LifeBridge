namespace Khoon_e_Hayat.Services
{
    public interface IWhatsAppService
    {
        // Matches the previous SMS signature for seamless replacement
        // Returns true if sent successfully, false if failed
        Task<bool> SendWhatsAppAsync(string phoneNumber, string message, int? requestId = null, int? alertId = null, int? donorId = null, string category = "General");
    }
}