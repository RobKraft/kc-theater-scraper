using HtmlAgilityPack;
using KCTheaterScraper.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace KCTheaterScraper.Scrapers
{
    public abstract class BaseTheaterScraper : ITheaterScraper
    {
        protected readonly HttpClient HttpClient;
        protected readonly ILogger Logger;

        public abstract string ScraperName { get; }

        protected BaseTheaterScraper(HttpClient httpClient, ILogger logger)
        {
            HttpClient = httpClient;
            Logger = logger;
            
            // Set user agent to avoid blocking
            HttpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        public abstract Task<List<TheaterEvent>> ScrapeEventsAsync(TheaterVenue venue, CancellationToken cancellationToken = default);
        
        public abstract bool CanScrape(TheaterVenue venue);

        protected async Task<HtmlDocument> LoadHtmlDocumentAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation($"Loading HTML from: {url}");
                var response = await HttpClient.GetStringAsync(url, cancellationToken);
                
                var doc = new HtmlDocument();
                doc.LoadHtml(response);
                return doc;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to load HTML from: {url}");
                throw;
            }
        }

        protected DateTime? ParseDateTime(string dateTimeText, string? timeText = null)
        {
            if (string.IsNullOrWhiteSpace(dateTimeText))
                return null;

            try
            {
                // Clean up the text
                dateTimeText = CleanDateTimeText(dateTimeText);
                
                if (!string.IsNullOrWhiteSpace(timeText))
                {
                    timeText = CleanDateTimeText(timeText);
                    dateTimeText = $"{dateTimeText} {timeText}";
                }

                // Try various date formats
                var formats = new[]
                {
                    "yyyy-MM-dd HH:mm",
                    "yyyy-MM-dd h:mm tt",
                    "MM/dd/yyyy HH:mm",
                    "MM/dd/yyyy h:mm tt",
                    "MMM dd, yyyy HH:mm",
                    "MMM dd, yyyy h:mm tt",
                    "MMMM dd, yyyy HH:mm",
                    "MMMM dd, yyyy h:mm tt",
                    "dd MMM yyyy HH:mm",
                    "dd MMM yyyy h:mm tt",
                    "yyyy-MM-dd",
                    "MM/dd/yyyy",
                    "MMM dd, yyyy",
                    "MMMM dd, yyyy",
                    "dd MMM yyyy"
                };

                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(dateTimeText, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                    {
                        return result;
                    }
                }

                // Try generic parsing
                if (DateTime.TryParse(dateTimeText, out var genericResult))
                {
                    return genericResult;
                }

                Logger.LogWarning($"Could not parse date/time: {dateTimeText}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, $"Error parsing date/time: {dateTimeText}");
                return null;
            }
        }

        protected string CleanDateTimeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Remove extra whitespace and common unwanted characters
            text = Regex.Replace(text, @"\s+", " ").Trim();
            text = text.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");
            
            return text;
        }

        protected string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Decode HTML entities and clean up text
            text = System.Net.WebUtility.HtmlDecode(text);
            text = Regex.Replace(text, @"\s+", " ").Trim();
            
            return text;
        }

        protected decimal? ParsePrice(string priceText)
        {
            if (string.IsNullOrWhiteSpace(priceText))
                return null;

            // Extract numbers and decimal points from price text
            var match = Regex.Match(priceText, @"[\d,]+\.?\d*");
            if (match.Success)
            {
                var cleanPrice = match.Value.Replace(",", "");
                if (decimal.TryParse(cleanPrice, out var price))
                {
                    return price;
                }
            }

            return null;
        }

        protected string MakeAbsoluteUrl(string baseUrl, string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
                return string.Empty;

            if (Uri.IsWellFormedUriString(relativeUrl, UriKind.Absolute))
                return relativeUrl;

            if (Uri.TryCreate(new Uri(baseUrl), relativeUrl, out var absoluteUri))
                return absoluteUri.ToString();

            return relativeUrl;
        }
    }
}
