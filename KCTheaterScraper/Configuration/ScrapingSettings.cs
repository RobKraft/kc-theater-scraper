namespace KCTheaterScraper.Configuration
{
    public class ScrapingSettings
    {
        public int MaxConcurrentScrapes { get; set; } = 5;
        public int RequestDelayMs { get; set; } = 1000;
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryAttempts { get; set; } = 3;
        public string OutputDirectory { get; set; } = "./output";
        public string CalendarFileName { get; set; } = "kc-theater-events.ics";
    }
}
