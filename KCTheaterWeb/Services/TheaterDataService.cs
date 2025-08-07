using System.Text.Json;
using KCTheaterWeb.Models;

namespace KCTheaterWeb.Services
{
    public interface ITheaterDataService
    {
        Task<TheaterEventCollection> GetEventsAsync();
        Task<List<TheaterEvent>> GetEventsByVenueAsync(string venueName);
        Task<List<TheaterEvent>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<TheaterEvent>> SearchEventsAsync(string searchTerm);
    }

    public class TheaterDataService : ITheaterDataService
    {
        private readonly ILogger<TheaterDataService> _logger;
        private readonly string _dataDirectory;
        private TheaterEventCollection? _cachedEvents;
        private DateTime _lastCacheTime = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

        public TheaterDataService(ILogger<TheaterDataService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _dataDirectory = configuration.GetValue<string>("DataDirectory") ?? 
                           Path.Combine(Directory.GetCurrentDirectory(), "..", "output");
        }

        public async Task<TheaterEventCollection> GetEventsAsync()
        {
            // Return cached data if still valid
            if (_cachedEvents != null && DateTime.Now - _lastCacheTime < _cacheExpiry)
            {
                _logger.LogDebug("Returning cached theater events data");
                return _cachedEvents;
            }

            try
            {
                var jsonFile = Path.Combine(_dataDirectory, "kc-theater-events.json");
                
                if (!File.Exists(jsonFile))
                {
                    _logger.LogWarning("Theater events JSON file not found at {Path}", jsonFile);
                    return new TheaterEventCollection();
                }

                var jsonContent = await File.ReadAllTextAsync(jsonFile);
                var events = JsonSerializer.Deserialize<List<TheaterEvent>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _cachedEvents = new TheaterEventCollection
                {
                    Events = events ?? new List<TheaterEvent>(),
                    LastUpdated = File.GetLastWriteTime(jsonFile)
                };

                _lastCacheTime = DateTime.Now;
                _logger.LogInformation("Loaded {Count} theater events from {Path}", _cachedEvents.Events.Count, jsonFile);

                return _cachedEvents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading theater events from JSON file");
                return new TheaterEventCollection();
            }
        }

        public async Task<List<TheaterEvent>> GetEventsByVenueAsync(string venueName)
        {
            var events = await GetEventsAsync();
            return events.Events
                .Where(e => e.VenueName.Equals(venueName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.StartDate)
                .ToList();
        }

        public async Task<List<TheaterEvent>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var events = await GetEventsAsync();
            return events.Events
                .Where(e => e.StartDate.Date >= startDate.Date && e.StartDate.Date <= endDate.Date)
                .OrderBy(e => e.StartDate)
                .ToList();
        }

        public async Task<List<TheaterEvent>> SearchEventsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<TheaterEvent>();

            var events = await GetEventsAsync();
            var term = searchTerm.ToLowerInvariant();

            return events.Events
                .Where(e => 
                    e.Title.ToLowerInvariant().Contains(term) ||
                    e.Description.ToLowerInvariant().Contains(term) ||
                    e.VenueName.ToLowerInvariant().Contains(term) ||
                    e.Category.ToLowerInvariant().Contains(term))
                .OrderBy(e => e.StartDate)
                .ToList();
        }
    }
}
