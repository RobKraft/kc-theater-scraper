namespace KCTheaterWeb.Models
{
    public class TheaterEvent
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string VenueAddress { get; set; } = string.Empty;
        public string VenueWebsite { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal? TicketPrice { get; set; }
        public string? TicketUrl { get; set; }
        public DateTime ScrapedAt { get; set; }

        public string FormattedDate => StartDate.ToString("MMM dd, yyyy");
        public string FormattedTime => StartDate.ToString("h:mm tt");
        public string FormattedDateTime => StartDate.ToString("MMM dd, yyyy h:mm tt");
        public bool HasEndDate => EndDate.HasValue;
        public string Duration => HasEndDate ? $"{FormattedDateTime} - {EndDate!.Value:h:mm tt}" : FormattedDateTime;
    }

    public class TheaterEventCollection
    {
        public List<TheaterEvent> Events { get; set; } = new();
        public DateTime LastUpdated { get; set; }
        public int TotalEvents => Events.Count;
        public List<string> Venues => Events.Select(e => e.VenueName).Distinct().OrderBy(v => v).ToList();
        public List<string> Categories => Events.Select(e => e.Category).Distinct().OrderBy(c => c).ToList();
    }
}
