using HtmlAgilityPack;
using KCTheaterScraper.Models;
using Microsoft.Extensions.Logging;

namespace KCTheaterScraper.Scrapers
{
    public class KCRepScraper : BaseTheaterScraper
    {
        public override string ScraperName => "Kansas City Repertory Theatre";

        public KCRepScraper(HttpClient httpClient, ILogger<KCRepScraper> logger) 
            : base(httpClient, logger)
        {
        }

        public override bool CanScrape(TheaterVenue venue)
        {
            return venue.ScraperType == "kcrep" || 
                   venue.Url.Contains("kcrep.org") ||
                   venue.Name.Contains("KC Rep") ||
                   venue.Name.Contains("Kansas City Repertory");
        }

        public override async Task<List<TheaterEvent>> ScrapeEventsAsync(TheaterVenue venue, CancellationToken cancellationToken = default)
        {
            var events = new List<TheaterEvent>();

            try
            {
                Logger.LogInformation($"Scraping events from {venue.Name}");

                var doc = await LoadHtmlDocumentAsync(venue.Url, cancellationToken);

                // Template selectors - would need to be updated for actual site
                var eventNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'show')] | //div[contains(@class, 'production')]") ?? new HtmlNodeCollection(null);

                foreach (var node in eventNodes)
                {
                    try
                    {
                        var titleNode = node.SelectSingleNode(".//h1 | .//h2 | .//h3 | .//a[contains(@class, 'title')]");
                        var dateNodes = node.SelectNodes(".//*[contains(@class, 'date') or contains(@class, 'showtime')]");
                        var linkNode = node.SelectSingleNode(".//a[@href]");

                        if (titleNode == null) continue;

                        var baseEvent = new TheaterEvent
                        {
                            Title = CleanText(titleNode.InnerText),
                            VenueName = venue.Name,
                            VenueAddress = venue.Address,
                            Source = ScraperName,
                            EventUrl = linkNode != null ? MakeAbsoluteUrl(venue.Url, linkNode.GetAttributeValue("href", "")) : "",
                        };

                        // KC Rep often has multiple showtimes for the same production
                        if (dateNodes != null && dateNodes.Count > 0)
                        {
                            foreach (var dateNode in dateNodes)
                            {
                                var dateTime = ParseDateTime(CleanText(dateNode.InnerText));
                                if (dateTime.HasValue)
                                {
                                    var theaterEvent = new TheaterEvent
                                    {
                                        Title = baseEvent.Title,
                                        VenueName = baseEvent.VenueName,
                                        VenueAddress = baseEvent.VenueAddress,
                                        Source = baseEvent.Source,
                                        EventUrl = baseEvent.EventUrl,
                                        StartDateTime = dateTime.Value
                                    };

                                    // Try to get more details
                                    if (!string.IsNullOrWhiteSpace(theaterEvent.EventUrl))
                                    {
                                        await EnrichEventDetailsAsync(theaterEvent, cancellationToken);
                                    }

                                    theaterEvent.GenerateId();
                                    events.Add(theaterEvent);
                                }
                            }
                        }
                        else
                        {
                            // No specific date found, add as general event
                            if (!string.IsNullOrWhiteSpace(baseEvent.EventUrl))
                            {
                                await EnrichEventDetailsAsync(baseEvent, cancellationToken);
                            }
                            baseEvent.GenerateId();
                            events.Add(baseEvent);
                        }
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
                
                // Extract show description
                var descriptionNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'description')] | //div[contains(@class, 'synopsis')] | //p[contains(@class, 'summary')]");
                if (descriptionNode != null)
                {
                    theaterEvent.Description = CleanText(descriptionNode.InnerText);
                }

                // Extract ticket information
                var ticketNode = doc.DocumentNode.SelectSingleNode("//a[contains(@href, 'ticket') or contains(text(), 'Buy Tickets')]");
                if (ticketNode != null)
                {
                    theaterEvent.TicketUrl = MakeAbsoluteUrl(theaterEvent.EventUrl, ticketNode.GetAttributeValue("href", ""));
                }

                // Extract pricing
                var priceNode = doc.DocumentNode.SelectSingleNode("//*[contains(text(), '$') and (contains(@class, 'price') or contains(@class, 'cost'))]");
                if (priceNode != null)
                {
                    theaterEvent.Price = ParsePrice(priceNode.InnerText);
                    theaterEvent.PriceRange = CleanText(priceNode.InnerText);
                }

                // Extract show image
                var imageNode = doc.DocumentNode.SelectSingleNode("//img[contains(@class, 'show-image') or contains(@alt, 'show') or contains(@class, 'production')]");
                if (imageNode != null)
                {
                    theaterEvent.ImageUrl = MakeAbsoluteUrl(theaterEvent.EventUrl, imageNode.GetAttributeValue("src", ""));
                }

                // Set categories for theater
                theaterEvent.Categories.Add("Theater");
                theaterEvent.Categories.Add("Live Performance");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, $"Could not enrich details for event: {theaterEvent.Title}");
            }
        }
    }
}
