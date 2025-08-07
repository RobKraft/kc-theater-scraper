using HtmlAgilityPack;
using KCTheaterScraper.Models;
using Microsoft.Extensions.Logging;

namespace KCTheaterScraper.Scrapers
{
    public class GenericTheaterScraper : BaseTheaterScraper
    {
        public override string ScraperName => "Generic Theater Scraper";

        public GenericTheaterScraper(HttpClient httpClient, ILogger<GenericTheaterScraper> logger) 
            : base(httpClient, logger)
        {
        }

        public override bool CanScrape(TheaterVenue venue)
        {
            // This scraper can attempt to scrape any venue as a fallback
            return venue.ScraperType == "generic" || string.IsNullOrWhiteSpace(venue.ScraperType);
        }

        public override async Task<List<TheaterEvent>> ScrapeEventsAsync(TheaterVenue venue, CancellationToken cancellationToken = default)
        {
            var events = new List<TheaterEvent>();

            try
            {
                Logger.LogInformation($"Attempting generic scraping for {venue.Name}");

                var doc = await LoadHtmlDocumentAsync(venue.Url, cancellationToken);

                // Try various common selectors for events
                var eventSelectors = new[]
                {
                    "//div[contains(@class, 'event')]",
                    "//div[contains(@class, 'show')]",
                    "//div[contains(@class, 'performance')]",
                    "//div[contains(@class, 'production')]",
                    "//article[contains(@class, 'event')]",
                    "//li[contains(@class, 'event')]",
                    "//*[@itemtype='http://schema.org/Event']",
                    "//div[contains(@class, 'calendar-event')]"
                };

                HtmlNodeCollection? eventNodes = null;
                foreach (var selector in eventSelectors)
                {
                    eventNodes = doc.DocumentNode.SelectNodes(selector);
                    if (eventNodes != null && eventNodes.Count > 0)
                    {
                        Logger.LogInformation($"Found {eventNodes.Count} events using selector: {selector}");
                        break;
                    }
                }

                if (eventNodes == null || eventNodes.Count == 0)
                {
                    Logger.LogWarning($"No events found using generic selectors for {venue.Name}");
                    return events;
                }

                foreach (var node in eventNodes)
                {
                    try
                    {
                        var theaterEvent = ExtractEventFromNode(node, venue);
                        if (theaterEvent != null && !string.IsNullOrWhiteSpace(theaterEvent.Title))
                        {
                            theaterEvent.GenerateId();
                            events.Add(theaterEvent);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Error parsing event node with generic scraper");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error with generic scraping for {venue.Name}");
            }

            Logger.LogInformation($"Generic scraper found {events.Count} events from {venue.Name}");
            return events;
        }

        private TheaterEvent? ExtractEventFromNode(HtmlNode node, TheaterVenue venue)
        {
            // Try various selectors to find title
            var titleSelectors = new[]
            {
                ".//h1", ".//h2", ".//h3", ".//h4",
                ".//*[contains(@class, 'title')]",
                ".//*[contains(@class, 'name')]",
                ".//*[@itemprop='name']",
                ".//a[not(contains(@class, 'btn'))]"
            };

            string? title = null;
            HtmlNode? titleNode = null;
            foreach (var selector in titleSelectors)
            {
                titleNode = node.SelectSingleNode(selector);
                if (titleNode != null)
                {
                    title = CleanText(titleNode.InnerText);
                    if (!string.IsNullOrWhiteSpace(title) && title.Length > 3)
                    {
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(title))
                return null;

            var theaterEvent = new TheaterEvent
            {
                Title = title,
                VenueName = venue.Name,
                VenueAddress = venue.Address,
                Source = ScraperName
            };

            // Try to find date/time
            var dateSelectors = new[]
            {
                ".//*[contains(@class, 'date')]",
                ".//*[contains(@class, 'time')]",
                ".//*[contains(@class, 'when')]",
                ".//*[@itemprop='startDate']",
                ".//*[@datetime]"
            };

            foreach (var selector in dateSelectors)
            {
                var dateNode = node.SelectSingleNode(selector);
                if (dateNode != null)
                {
                    var dateTime = ParseDateTime(CleanText(dateNode.InnerText));
                    if (dateTime.HasValue)
                    {
                        theaterEvent.StartDateTime = dateTime.Value;
                        break;
                    }

                    // Try datetime attribute
                    var dateTimeAttr = dateNode.GetAttributeValue("datetime", "");
                    if (!string.IsNullOrWhiteSpace(dateTimeAttr))
                    {
                        var dateTime2 = ParseDateTime(dateTimeAttr);
                        if (dateTime2.HasValue)
                        {
                            theaterEvent.StartDateTime = dateTime2.Value;
                            break;
                        }
                    }
                }
            }

            // Try to find link
            var linkNode = node.SelectSingleNode(".//a[@href]") ?? titleNode?.SelectSingleNode("ancestor-or-self::a[@href]");
            if (linkNode != null)
            {
                theaterEvent.EventUrl = MakeAbsoluteUrl(venue.Url, linkNode.GetAttributeValue("href", ""));
            }

            // Try to find description
            var descSelectors = new[]
            {
                ".//*[contains(@class, 'description')]",
                ".//*[contains(@class, 'summary')]",
                ".//*[@itemprop='description']",
                ".//p[not(contains(@class, 'date')) and not(contains(@class, 'time'))]"
            };

            foreach (var selector in descSelectors)
            {
                var descNode = node.SelectSingleNode(selector);
                if (descNode != null)
                {
                    var desc = CleanText(descNode.InnerText);
                    if (!string.IsNullOrWhiteSpace(desc) && desc.Length > 10)
                    {
                        theaterEvent.Description = desc;
                        break;
                    }
                }
            }

            // Try to find price
            var priceNode = node.SelectSingleNode(".//*[contains(text(), '$')]");
            if (priceNode != null)
            {
                theaterEvent.Price = ParsePrice(priceNode.InnerText);
                theaterEvent.PriceRange = CleanText(priceNode.InnerText);
            }

            // Try to find image
            var imageNode = node.SelectSingleNode(".//img[@src]");
            if (imageNode != null)
            {
                theaterEvent.ImageUrl = MakeAbsoluteUrl(venue.Url, imageNode.GetAttributeValue("src", ""));
            }

            // Add default categories
            theaterEvent.Categories.Add("Theater");

            return theaterEvent;
        }
    }
}
