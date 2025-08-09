using KCTheaterWeb.Models;

namespace KCTheaterWeb.Models.ViewModels
{
    public class HomeViewModel
    {
        public TheaterEventCollection Events { get; set; } = new();
        public List<TheaterEvent> UpcomingEvents { get; set; } = new();
        public List<TheaterEvent> TodaysEvents { get; set; } = new();
        public List<TheaterEvent> ThisWeekEvents { get; set; } = new();
        public Dictionary<string, int> VenueEventCounts { get; set; } = new();
        public string? SelectedVenue { get; set; }
        public string? SelectedCategory { get; set; }
        public DateTime? SelectedDate { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
        
        public string ViewMode { get; set; } = "list"; // list, calendar, grid
    }

    public class CalendarViewModel
    {
        public List<TheaterEvent> Events { get; set; } = new();
        public DateTime CurrentMonth { get; set; } = DateTime.Today;
        public DateTime PreviousMonth => CurrentMonth.AddMonths(-1);
        public DateTime NextMonth => CurrentMonth.AddMonths(1);
        public string MonthYearDisplay => CurrentMonth.ToString("MMMM yyyy");
        
        public List<CalendarDay> CalendarDays { get; set; } = new();
    }

    public class CalendarDay
    {
        public DateTime Date { get; set; }
        public List<TheaterEvent> Events { get; set; } = new();
        public bool IsCurrentMonth { get; set; }
        public bool IsToday => Date.Date == DateTime.Today;
        public bool HasEvents => Events.Any();
        public int EventCount => Events.Count;
    }
}
