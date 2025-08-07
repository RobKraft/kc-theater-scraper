using HtmlAgilityPack;
using KCTheaterScraper.Models;
using Microsoft.Extensions.Logging;

namespace KCTheaterScraper.Scrapers
{
    public class KauffmanCenterScraper : BaseTheaterScraper
    {
        public override string ScraperName => "Kauffman Center for the Performing Arts";

        public KauffmanCenterScraper(HttpClient httpClient, ILogger<KauffmanCenterScraper> logger) 
            : base(httpClient, logger)
        {
        }

        public override bool CanScrape(TheaterVenue venue)
        {
            return venue.ScraperType == "kauffman" || 
                   venue.Url.Contains("kauffmancenter.org") ||
                   venue.Name.Contains("Kauffman");
        }

        public override async Task<List<TheaterEvent>> ScrapeEventsAsync(TheaterVenue venue, CancellationToken cancellationToken = default)
        {
            var events = new List<TheaterEvent>();

            try
            {
                Logger.LogInformation($"Scraping events from {venue.Name}");

                var doc = await LoadHtmlDocumentAsync(venue.Url, cancellationToken);

                // This is a template - actual selectors would need to be updated based on the real website structure
                var eventNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'event-item')]") ?? new HtmlNodeCollection(null);

                foreach (var node in eventNodes)
                {
                    try
                    {
                        var titleNode = node.SelectSingleNode(".//h2 | .//h3 | .//a[contains(@class, 'title')]");
                        var dateNode = node.SelectSingleNode(".//*[contains(@class, 'date') or contains(@class, 'time')]");
                        var linkNode = node.SelectSingleNode(".//a[@href]");

                        if (titleNode == null) continue;

                        var theaterEvent = new TheaterEvent
                        {
                            Title = CleanText(titleNode.InnerText),
                            VenueName = venue.Name,
                            VenueAddress = venue.Address,
                            Source = ScraperName,
                            EventUrl = linkNode != null ? MakeAbsoluteUrl(venue.Url, linkNode.GetAttributeValue("href", "")) : "",
                        };

                        if (dateNode != null)
                        {
                            var dateTime = ParseDateTime(CleanText(dateNode.InnerText));
                            if (dateTime.HasValue)
                            {
                                theaterEvent.StartDateTime = dateTime.Value;
                            }
                        }

                        // Try to get more details if we have a link
                        if (!string.IsNullOrWhiteSpace(theaterEvent.EventUrl))
                        {
                            await EnrichEventDetailsAsync(theaterEvent, cancellationToken);
                        }

                        theaterEvent.GenerateId();
                        events.Add(theaterEvent);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Error parsing event node");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error scraping events from {venue.Name}");
            }

            Logger.LogInformation($"Found {events.Count} events from {venue.Name}");
            return events;
        }

        private async Task EnrichEventDetailsAsync(TheaterEvent theaterEvent, CancellationToken cancellationToken)
        {
            try
            {
                var doc = await LoadHtmlDocumentAsync(theaterEvent.EventUrl, cancellationToken);
                
                // Extract additional details from the event page
                var descriptionNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'description')] | //div[contains(@class, 'summary')]");
                if (descriptionNode != null)
                {
                    theaterEvent.Description = CleanText(descriptionNode.InnerText);
                }

                var priceNode = doc.DocumentNode.SelectSingleNode("//*[contains(text(), '$') or contains(@class, 'price')]");
                if (priceNode != null)
                {
                    theaterEvent.Price = ParsePrice(priceNode.InnerText);
                    theaterEvent.PriceRange = CleanText(priceNode.InnerText);
                }

                var imageNode = doc.DocumentNode.SelectSingleNode("//img[contains(@class, 'event-image') or contains(@alt, 'event')]");
                if (imageNode != null)
                {
                    theaterEvent.ImageUrl = MakeAbsoluteUrl(theaterEvent.EventUrl, imageNode.GetAttributeValue("src", ""));
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, $"Could not enrich details for event: {theaterEvent.Title}");
            }
        }
    }
}
