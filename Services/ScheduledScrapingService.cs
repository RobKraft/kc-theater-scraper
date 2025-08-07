using KCTheaterScraper.Configuration;
using KCTheaterScraper.Models;
using KCTheaterScraper.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KCTheaterScraper.Services
{
    public class ScheduledScrapingService : BackgroundService
    {
        private readonly ScrapingService _scrapingService;
        private readonly CalendarService _calendarService;
        private readonly ILogger<ScheduledScrapingService> _logger;
        private readonly ScrapingSettings _settings;
        private readonly IConfiguration _configuration;

        public ScheduledScrapingService(
            ScrapingService scrapingService,
            CalendarService calendarService,
            ILogger<ScheduledScrapingService> logger,
            IOptions<ScrapingSettings> settings,
            IConfiguration configuration)
        {
            _scrapingService = scrapingService;
            _calendarService = calendarService;
            _logger = logger;
            _settings = settings.Value;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scheduled scraping service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunScrapingCycle(stoppingToken);
                    
                    // Wait for 6 hours before next scraping cycle
                    _logger.LogInformation("Scraping cycle completed. Next cycle in 6 hours.");
                    await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Scheduled scraping service was cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in scheduled scraping service");
                    // Wait 30 minutes before retrying on error
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
            }

            _logger.LogInformation("Scheduled scraping service stopped");
        }

        private async Task RunScrapingCycle(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting scraping cycle");

            // Load venues from configuration
            var venues = LoadVenuesFromConfiguration();
            _logger.LogInformation($"Loaded {venues.Count} venues from configuration");

            // Scrape all venues
            var allEvents = await _scrapingService.ScrapeAllVenuesAsync(venues, cancellationToken);
            _logger.LogInformation($"Scraped {allEvents.Count} total events");

            if (allEvents.Any())
            {
                // Create output directory if it doesn't exist
                var outputDir = Path.GetFullPath(_settings.OutputDirectory);
                Directory.CreateDirectory(outputDir);

                // Generate calendar
                var calendar = _calendarService.CreateICalendar(allEvents, "Kansas City Theater Events");
                
                // Save calendar file
                var calendarPath = Path.Combine(outputDir, _settings.CalendarFileName);
                await _calendarService.SaveCalendarToFileAsync(calendar, calendarPath);

                // Also save a JSON file with all event details
                var jsonPath = Path.Combine(outputDir, "kc-theater-events.json");
                await SaveEventsAsJsonAsync(allEvents, jsonPath);

                // Generate some statistics
                await GenerateStatisticsAsync(allEvents, outputDir);

                _logger.LogInformation($"Scraping cycle completed successfully. Files saved to: {outputDir}");
            }
            else
            {
                _logger.LogWarning("No events found during scraping cycle");
            }
        }

        private List<TheaterVenue> LoadVenuesFromConfiguration()
        {
            var venues = new List<TheaterVenue>();
            var venuesSection = _configuration.GetSection("Venues");
            
            foreach (var venueSection in venuesSection.GetChildren())
            {
                var venue = new TheaterVenue();
                venueSection.Bind(venue);
                venues.Add(venue);
            }

            return venues.Where(v => v.IsActive).ToList();
        }

        private async Task SaveEventsAsJsonAsync(List<TheaterEvent> events, string filePath)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(events, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync(filePath, json);
                _logger.LogInformation($"Events saved as JSON to: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving events as JSON to: {filePath}");
            }
        }

        private async Task GenerateStatisticsAsync(List<TheaterEvent> events, string outputDir)
        {
            try
            {
                var stats = new
                {
                    TotalEvents = events.Count,
                    VenueCount = events.Select(e => e.VenueName).Distinct().Count(),
                    DateRange = new
                    {
                        Earliest = events.Where(e => e.StartDateTime != DateTime.MinValue).Min(e => e.StartDateTime),
                        Latest = events.Where(e => e.StartDateTime != DateTime.MinValue).Max(e => e.StartDateTime)
                    },
                    EventsByVenue = events.GroupBy(e => e.VenueName)
                        .Select(g => new { Venue = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .ToList(),
                    EventsByMonth = events.Where(e => e.StartDateTime != DateTime.MinValue)
                        .GroupBy(e => e.StartDateTime.ToString("yyyy-MM"))
                        .Select(g => new { Month = g.Key, Count = g.Count() })
                        .OrderBy(x => x.Month)
                        .ToList(),
                    Categories = events.SelectMany(e => e.Categories).Distinct().ToList(),
                    LastUpdated = DateTime.UtcNow
                };

                var json = System.Text.Json.JsonSerializer.Serialize(stats, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

                var statsPath = Path.Combine(outputDir, "scraping-statistics.json");
                await File.WriteAllTextAsync(statsPath, json);
                _logger.LogInformation($"Statistics saved to: {statsPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating statistics");
            }
        }

        public async Task RunManualScrapingAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Running manual scraping cycle");
            await RunScrapingCycle(cancellationToken);
        }
    }
}
