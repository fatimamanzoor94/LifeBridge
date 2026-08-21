namespace Khoon_e_Hayat.Services
{
    public interface IGoogleMapsService
    {
        /// <summary>
        /// Calculates road distance and driving time between two coordinates.
        /// </summary>
        /// <returns>A tuple containing (Distance in KM, Duration string, Success boolean)</returns>
        Task<(double distanceKm, string duration, bool success)> GetDistanceAndDurationAsync(double originLat, double originLng, double destLat, double destLng);
    }
}