using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Khoon_e_Hayat.Data;
using Khoon_e_Hayat.Models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Khoon_e_Hayat.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WhatsAppService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public WhatsAppService(IConfiguration config, ApplicationDbContext context, ILogger<WhatsAppService> logger, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> SendWhatsAppAsync(string phoneNumber, string message, int? requestId = null, int? alertId = null, int? donorId = null, string category = "General")
        {
            // ✅ STEP 1: Clean and Format Phone Number for WhatsApp API (E.164 format without '+')
            string cleanPhone = CleanPhoneNumber(phoneNumber);

            _logger.LogInformation($"📱 Original Phone: {phoneNumber} | Cleaned Phone: {cleanPhone}");

            // ✅ STEP 2: Create Log Entry (Pending)
            var log = new NotificationLog
            {
                RecipientPhone = cleanPhone, // Save cleaned number in DB
                NotificationType = "WhatsApp",
                Message = message,
                Status = "Pending",
                RequestId = requestId,
                AlertId = alertId,
                DonorId = donorId,
                Category = category,
                SentAt = DateTime.Now
            };

            try
            {
                var accessToken = _config["WhatsApp:AccessToken"];
                var phoneNumberId = _config["WhatsApp:PhoneNumberId"];
                var apiVersion = _config["WhatsApp:ApiVersion"] ?? "v18.0";

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(phoneNumberId))
                {
                    log.Status = "Failed";
                    log.ErrorMessage = "WhatsApp credentials not configured in appsettings.json";
                    _logger.LogError(log.ErrorMessage);
                }
                else
                {
                    var url = $"https://graph.facebook.com/{apiVersion}/{phoneNumberId}/messages";

                    // WhatsApp Cloud API Payload
                    var payload = new
                    {
                        messaging_product = "whatsapp",
                        to = cleanPhone, // ✅ Use cleaned phone number here
                        type = "text",
                        text = new { body = message }
                    };

                    var client = _httpClientFactory.CreateClient();
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Add("Authorization", $"Bearer {accessToken}");
                    request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(request);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        log.Status = "Sent";
                        _logger.LogInformation($"✅ WhatsApp sent successfully to {cleanPhone}! Response: {responseBody}");
                    }
                    else
                    {
                        log.Status = "Failed";
                        // ✅ Log exact API error so you can see why it failed in NotificationLogs table
                        log.ErrorMessage = $"WhatsApp API Error: {(int)response.StatusCode} - {responseBody}";
                        _logger.LogError($"❌ WhatsApp failed for {cleanPhone}: {log.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Graceful failure: Log error but DO NOT throw, so Email flow continues
                log.Status = "Failed";
                log.ErrorMessage = ex.Message;
                _logger.LogError(ex, $"❌ Exception while sending WhatsApp to {cleanPhone}");
            }
            finally
            {
                // Always save log to database
                _context.NotificationLogs.Add(log);
                await _context.SaveChangesAsync();
            }

            return log.Status == "Sent";
        }

        // ✅ Helper method to clean phone number for WhatsApp API
        private string CleanPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return phone;

            // Remove all non-digit characters (like +, -, spaces, parentheses)
            string cleaned = new string(phone.Where(char.IsDigit).ToArray());

            // If number starts with '0' (e.g., 03211245847), replace '0' with '92' (Pakistan country code)
            if (cleaned.StartsWith("0"))
            {
                cleaned = "92" + cleaned.Substring(1);
            }
            // If it already starts with 92, it's fine.
            // If it is exactly 10 digits and doesn't start with 92, assume it needs 92
            else if (cleaned.Length == 10 && !cleaned.StartsWith("92"))
            {
                cleaned = "92" + cleaned;
            }

            return cleaned;
        }
    }
}