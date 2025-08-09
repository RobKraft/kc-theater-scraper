namespace KCTheaterScraper.Models
{
    public class TheaterVenue
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ScraperType { get; set; } = string.Empty;
        public Dictionary<string, string> ScraperConfig { get; set; } = new Dictionary<string, string>();
        public bool IsActive { get; set; } = true;
        public DateTime LastScraped { get; set; } = DateTime.MinValue;
    }
}
