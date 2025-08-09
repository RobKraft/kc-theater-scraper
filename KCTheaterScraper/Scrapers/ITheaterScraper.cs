using KCTheaterScraper.Models;

namespace KCTheaterScraper.Scrapers
{
    public interface ITheaterScraper
    {
        string ScraperName { get; }
        Task<List<TheaterEvent>> ScrapeEventsAsync(TheaterVenue venue, CancellationToken cancellationToken = default);
        bool CanScrape(TheaterVenue venue);
    }
}
