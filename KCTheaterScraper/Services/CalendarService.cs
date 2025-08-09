using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using KCTheaterScraper.Models;
using Microsoft.Extensions.Logging;

namespace KCTheaterScraper.Services
{
    public class CalendarService
    {
        private readonly ILogger<CalendarService> _logger;

        public CalendarService(ILogger<CalendarService> logger)
        {
            _logger = logger;
        }

        public string CreateICalendar(List<TheaterEvent> events, string calendarName = "Kansas City Theater Events")
        {
            try
            {
                _logger.LogInformation($"Creating iCalendar with {events.Count} events");

                var calendar = new Calendar();
                calendar.Properties.Add(new CalendarProperty("PRODID", "-//KC Theater Scraper//EN"));
                calendar.Properties.Add(new CalendarProperty("VERSION", "2.0"));
                calendar.Properties.Add(new CalendarProperty("CALSCALE", "GREGORIAN"));
                calendar.Properties.Add(new CalendarProperty("METHOD", "PUBLISH"));
                calendar.Properties.Add(new CalendarProperty("X-WR-CALNAME", calendarName));
                calendar.Properties.Add(new CalendarProperty("X-WR-CALDESC", "Theater events in the Kansas City metro area"));

                foreach (var theaterEvent in events)
                {
                    try
                    {
                        var calEvent = CreateCalendarEvent(theaterEvent);
                        if (calEvent != null)
                        {
                            calendar.Events.Add(calEvent);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Could not create calendar event for: {theaterEvent.Title}");
                    }
                }

                var serializer = new CalendarSerializer(new SerializationContext());
                var icalString = serializer.SerializeToString(calendar);

                _logger.LogInformation($"Successfully created iCalendar with {calendar.Events.Count} events");
                return icalString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating iCalendar");
                throw;
            }
        }

        private CalendarEvent? CreateCalendarEvent(TheaterEvent theaterEvent)
        {
            // Skip events without dates
            if (theaterEvent.StartDateTime == DateTime.MinValue)
            {
                _logger.LogDebug($"Skipping event without date: {theaterEvent.Title}");
                return null;
            }

            var calEvent = new CalendarEvent
            {
                Uid = theaterEvent.Id,
                Created = new CalDateTime(theaterEvent.LastUpdated),
                DtStamp = new CalDateTime(DateTime.UtcNow),
                LastModified = new CalDateTime(theaterEvent.LastUpdated),
                Summary = theaterEvent.Title,
                Location = !string.IsNullOrWhiteSpace(theaterEvent.VenueAddress) 
                    ? $"{theaterEvent.VenueName}, {theaterEvent.VenueAddress}"
                    : theaterEvent.VenueName
            };

            // Set start time
            calEvent.DtStart = new CalDateTime(theaterEvent.StartDateTime);

            // Set end time (default to 2 hours if not specified)
            if (theaterEvent.EndDateTime.HasValue)
            {
                calEvent.DtEnd = new CalDateTime(theaterEvent.EndDateTime.Value);
            }
            else
            {
                calEvent.DtEnd = new CalDateTime(theaterEvent.StartDateTime.AddHours(2));
            }

            // Create description
            var description = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(theaterEvent.Description))
            {
                description.Add(theaterEvent.Description);
                description.Add("");
            }

            description.Add($"Venue: {theaterEvent.VenueName}");
            
            if (!string.IsNullOrWhiteSpace(theaterEvent.PriceRange))
            {
                description.Add($"Price: {theaterEvent.PriceRange}");
            }

            if (!string.IsNullOrWhiteSpace(theaterEvent.EventUrl))
            {
                description.Add($"More info: {theaterEvent.EventUrl}");
            }

            if (!string.IsNullOrWhiteSpace(theaterEvent.TicketUrl))
            {
                description.Add($"Tickets: {theaterEvent.TicketUrl}");
            }

            description.Add($"Source: {theaterEvent.Source}");

            calEvent.Description = string.Join("\n", description);

            // Add categories
            if (theaterEvent.Categories.Any())
            {
                calEvent.Categories = theaterEvent.Categories;
            }

            // Add URL
            if (!string.IsNullOrWhiteSpace(theaterEvent.EventUrl))
            {
                calEvent.Url = new Uri(theaterEvent.EventUrl);
            }

            return calEvent;
        }

        public async Task SaveCalendarToFileAsync(string icalContent, string filePath)
        {
            try
            {
                _logger.LogInformation($"Saving calendar to: {filePath}");
                await File.WriteAllTextAsync(filePath, icalContent);
                _logger.LogInformation("Calendar saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving calendar to file: {filePath}");
                throw;
            }
        }

        public List<TheaterEvent> FilterEventsByDateRange(List<TheaterEvent> events, DateTime? startDate = null, DateTime? endDate = null)
        {
            var filtered = events.Where(e => e.StartDateTime != DateTime.MinValue);

            if (startDate.HasValue)
            {
                filtered = filtered.Where(e => e.StartDateTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                filtered = filtered.Where(e => e.StartDateTime <= endDate.Value);
            }

            return filtered.ToList();
        }

        public List<TheaterEvent> FilterEventsByVenue(List<TheaterEvent> events, params string[] venueNames)
        {
            if (venueNames == null || venueNames.Length == 0)
                return events;

            return events.Where(e => venueNames.Any(v => 
                e.VenueName.Contains(v, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        public List<TheaterEvent> FilterEventsByCategory(List<TheaterEvent> events, params string[] categories)
        {
            if (categories == null || categories.Length == 0)
                return events;

            return events.Where(e => e.Categories.Any(c => 
                categories.Any(cat => c.Contains(cat, StringComparison.OrdinalIgnoreCase)))).ToList();
        }
    }
}
