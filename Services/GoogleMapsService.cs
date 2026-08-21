using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Khoon_e_Hayat.Services
{
    public class GoogleMapsService : IGoogleMapsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<GoogleMapsService> _logger;

        public GoogleMapsService(HttpClient httpClient, IConfiguration config, ILogger<GoogleMapsService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<(double distanceKm, string duration, bool success)> GetDistanceAndDurationAsync(double originLat, double originLng, double destLat, double destLng)
        {
            try
            {
                var apiKey = _config["GoogleMaps:ApiKey"];

                // Fallback if API key is not configured
                if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_GOOGLE_MAPS_API_KEY_HERE")
                {
                    _logger.LogWarning("Google Maps API Key is not configured. Falling back to Haversine formula.");
                    return (CalculateHaversine(originLat, originLng, destLat, destLng), "N/A", false);
                }

                var baseUrl = _config["GoogleMaps:DistanceMatrixUrl"];
                var url = $"{baseUrl}?origins={originLat},{originLng}&destinations={destLat},{destLng}&key={apiKey}&mode=driving";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.GetProperty("status").GetString() == "OK")
                {
                    var rows = root.GetProperty("rows");
                    var elements = rows[0].GetProperty("elements");
                    var element = elements[0];

                    if (element.GetProperty("status").GetString() == "OK")
                    {
                        var distanceValue = element.GetProperty("distance").GetProperty("value").GetInt32(); // in meters
                        var durationText = element.GetProperty("duration").GetProperty("text").GetString(); // e.g., "25 mins"

                        double distanceKm = distanceValue / 1000.0;
                        return (distanceKm, durationText, true);
                    }
                }

                _logger.LogWarning($"Google Maps API returned non-OK status: {json}");
                return (CalculateHaversine(originLat, originLng, destLat, destLng), "N/A", false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Google Maps Distance Matrix API.");
                return (CalculateHaversine(originLat, originLng, destLat, destLng), "N/A", false);
            }
        }

        // Fallback straight-line calculation
        private double CalculateHaversine(double lat1, double lon1, double lat2, double lon2)
        {
            if (lat1 == 0 || lon1 == 0 || lat2 == 0 || lon2 == 0) return 999;
            var R = 6371;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * (2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
        }
    }
}