using KCTheaterScraper.Models;
using KCTheaterScraper.Scrapers;
using Microsoft.Extensions.Logging;

namespace KCTheaterScraper.Services
{
    public class ScrapingService
    {
        private readonly IEnumerable<ITheaterScraper> _scrapers;
        private readonly ILogger<ScrapingService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ScrapingService(IEnumerable<ITheaterScraper> scrapers, ILogger<ScrapingService> logger, IHttpClientFactory httpClientFactory)
        {
            _scrapers = scrapers;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<TheaterEvent>> ScrapeAllVenuesAsync(List<TheaterVenue> venues, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Starting to scrape {venues.Count} venues");
            
            var allEvents = new List<TheaterEvent>();
            var tasks = new List<Task<List<TheaterEvent>>>();

            // Create tasks for parallel scraping
            foreach (var venue in venues.Where(v => v.IsActive))
            {
                tasks.Add(ScrapeVenueAsync(venue, cancellationToken));
            }

            // Wait for all scraping tasks to complete
            var results = await Task.WhenAll(tasks);
            
            // Combine all results
            foreach (var events in results)
            {
                allEvents.AddRange(events);
            }

            // Remove duplicates based on ID
            var uniqueEvents = allEvents
                .GroupBy(e => e.Id)
                .Select(g => g.First())
                .OrderBy(e => e.StartDateTime)
                .ToList();

            _logger.LogInformation($"Scraped {allEvents.Count} total events, {uniqueEvents.Count} unique events from {venues.Count} venues");
            
            return uniqueEvents;
        }

        public async Task<List<TheaterEvent>> ScrapeVenueAsync(TheaterVenue venue, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Scraping venue: {venue.Name}");

                // Find the appropriate scraper for this venue
                var scraper = _scrapers.FirstOrDefault(s => s.CanScrape(venue));
                
                if (scraper == null)
                {
                    _logger.LogWarning($"No suitable scraper found for venue: {venue.Name}");
                    return new List<TheaterEvent>();
                }

                _logger.LogInformation($"Using scraper: {scraper.ScraperName} for venue: {venue.Name}");

                var events = await scraper.ScrapeEventsAsync(venue, cancellationToken);
                
                // Update last scraped time
                venue.LastScraped = DateTime.UtcNow;

                _logger.LogInformation($"Successfully scraped {events.Count} events from {venue.Name}");
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error scraping venue: {venue.Name}");
                return new List<TheaterEvent>();
            }
        }

        public async Task<bool> TestVenueAsync(TheaterVenue venue, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Testing venue: {venue.Name} at {venue.Url}");

                // Test if the URL is accessible
                using var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(venue.Url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Venue {venue.Name} returned status code: {response.StatusCode}");
                    return false;
                }

                // Try to scrape a few events to test
                var events = await ScrapeVenueAsync(venue, cancellationToken);
                _logger.LogInformation($"Test successful for {venue.Name}, found {events.Count} events");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Test failed for venue: {venue.Name}");
                return false;
            }
        }
    }
}
