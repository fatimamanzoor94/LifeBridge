public class RequestTrackingFilters
{
    public string SearchQuery { get; set; } = string.Empty;
    public string Status { get; set; } = "all";
    public string Priority { get; set; } = "all";
    public string BloodGroup { get; set; } = "all";
    public string SortBy { get; set; } = "newest";
}