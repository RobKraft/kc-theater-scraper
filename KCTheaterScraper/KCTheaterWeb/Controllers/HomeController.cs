using Microsoft.AspNetCore.Mvc;
using KCTheaterWeb.Models;
using KCTheaterWeb.Models.ViewModels;
using KCTheaterWeb.Services;

namespace KCTheaterWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ITheaterDataService _theaterDataService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ITheaterDataService theaterDataService, ILogger<HomeController> logger)
        {
            _theaterDataService = theaterDataService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? venue, string? category, DateTime? date, string? search, string viewMode = "list")
        {
            var events = await _theaterDataService.GetEventsAsync();
            var filteredEvents = events.Events.AsEnumerable();

            // Apply filters
            if (!string.IsNullOrEmpty(venue))
            {
                filteredEvents = filteredEvents.Where(e => e.VenueName.Equals(venue, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(category))
            {
                filteredEvents = filteredEvents.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            if (date.HasValue)
            {
                filteredEvents = filteredEvents.Where(e => e.StartDate.Date == date.Value.Date);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var searchTerm = search.ToLowerInvariant();
                filteredEvents = filteredEvents.Where(e => 
                    e.Title.ToLowerInvariant().Contains(searchTerm) ||
                    e.Description.ToLowerInvariant().Contains(searchTerm) ||
                    e.VenueName.ToLowerInvariant().Contains(searchTerm));
            }

            var filteredList = filteredEvents.OrderBy(e => e.StartDate).ToList();
            var today = DateTime.Today;

            var viewModel = new HomeViewModel
            {
                Events = new TheaterEventCollection
                {
                    Events = filteredList,
                    LastUpdated = events.LastUpdated
                },
                UpcomingEvents = filteredList.Where(e => e.StartDate >= today).Take(10).ToList(),
                TodaysEvents = filteredList.Where(e => e.StartDate.Date == today).ToList(),
                ThisWeekEvents = filteredList.Where(e => e.StartDate.Date >= today && 
                                                        e.StartDate.Date <= today.AddDays(7)).ToList(),
                VenueEventCounts = events.Events
                    .GroupBy(e => e.VenueName)
                    .ToDictionary(g => g.Key, g => g.Count()),
                SelectedVenue = venue,
                SelectedCategory = category,
                SelectedDate = date,
                SearchTerm = search ?? string.Empty,
                ViewMode = viewMode
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Calendar(int? year, int? month)
        {
            var currentDate = new DateTime(year ?? DateTime.Today.Year, month ?? DateTime.Today.Month, 1);
            var events = await _theaterDataService.GetEventsAsync();
            
            // Get events for the current month and surrounding days
            var startDate = currentDate.AddDays(-currentDate.Day + 1).AddDays(-7); // Start from previous week
            var endDate = currentDate.AddMonths(1).AddDays(7); // End next week after month

            var monthEvents = events.Events
                .Where(e => e.StartDate >= startDate && e.StartDate <= endDate)
                .ToList();

            var calendarDays = new List<CalendarDay>();
            var current = startDate;

            while (current <= endDate)
            {
                calendarDays.Add(new CalendarDay
                {
                    Date = current,
                    Events = monthEvents.Where(e => e.StartDate.Date == current.Date).ToList(),
                    IsCurrentMonth = current.Month == currentDate.Month
                });
                current = current.AddDays(1);
            }

            var viewModel = new CalendarViewModel
            {
                Events = monthEvents,
                CurrentMonth = currentDate,
                CalendarDays = calendarDays
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Event(string id)
        {
            var events = await _theaterDataService.GetEventsAsync();
            var theaterEvent = events.Events.FirstOrDefault(e => e.Id == id);

            if (theaterEvent == null)
            {
                return NotFound();
            }

            return View(theaterEvent);
        }

        public async Task<IActionResult> Venue(string name)
        {
            var events = await _theaterDataService.GetEventsByVenueAsync(name);
            ViewBag.VenueName = name;
            return View(events);
        }

        public async Task<JsonResult> Search(string term)
        {
            var events = await _theaterDataService.SearchEventsAsync(term);
            return Json(events.Select(e => new 
            {
                id = e.Id,
                title = e.Title,
                venue = e.VenueName,
                date = e.FormattedDateTime,
                url = Url.Action("Event", new { id = e.Id })
            }));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
