using System;

namespace KCTheaterScraper.Models
{
    public class TheaterEvent
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string VenueName { get; set; } = string.Empty;
        public string VenueAddress { get; set; } = string.Empty;
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public string EventUrl { get; set; } = string.Empty;
        public string TicketUrl { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string PriceRange { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = new List<string>();
        public string ImageUrl { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        // Generate a unique ID based on content
        public void GenerateId()
        {
            var content = $"{Title}-{VenueName}-{StartDateTime:yyyy-MM-dd-HH-mm}";
            Id = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content))
                .Replace("/", "_").Replace("+", "-").Replace("=", "");
        }
    }
}
