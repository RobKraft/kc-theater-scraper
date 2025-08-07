using KCTheaterScraper.Configuration;
using KCTheaterScraper.Scrapers;
using KCTheaterScraper.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace KCTheaterScraper
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/scraper-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting KC Theater Scraper");

                var builder = Host.CreateApplicationBuilder(args);

                // Add configuration
                builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                // Add Serilog
                builder.Services.AddSerilog();

                // Configure settings
                builder.Services.Configure<ScrapingSettings>(
                    builder.Configuration.GetSection("ScrapingSettings"));

                // Add HTTP client
                builder.Services.AddHttpClient();
                
                // Register scrapers with HTTP client
                builder.Services.AddTransient<ITheaterScraper>(provider =>
                {
                    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                    var logger = provider.GetRequiredService<ILogger<KauffmanCenterScraper>>();
                    return new KauffmanCenterScraper(httpClientFactory.CreateClient(), logger);
                });
                
                builder.Services.AddTransient<ITheaterScraper>(provider =>
                {
                    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                    var logger = provider.GetRequiredService<ILogger<KCRepScraper>>();
                    return new KCRepScraper(httpClientFactory.CreateClient(), logger);
                });
                
                builder.Services.AddTransient<ITheaterScraper>(provider =>
                {
                    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                    var logger = provider.GetRequiredService<ILogger<GenericTheaterScraper>>();
                    return new GenericTheaterScraper(httpClientFactory.CreateClient(), logger);
                });

                // Register services
                builder.Services.AddTransient<ScrapingService>();
                builder.Services.AddTransient<CalendarService>();
                builder.Services.AddHostedService<ScheduledScrapingService>();

                var host = builder.Build();

                // Handle command line arguments
                if (args.Length > 0)
                {
                    await HandleCommandLineArgs(args, host.Services);
                }
                else
                {
                    // Run as a service
                    Log.Information("Running as scheduled service");
                    await host.RunAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        static async Task HandleCommandLineArgs(string[] args, IServiceProvider services)
        {
            var command = args[0].ToLower();

            switch (command)
            {
                case "scrape":
                case "run":
                    Log.Information("Running one-time scraping");
                    var scheduledService = services.GetRequiredService<ScheduledScrapingService>();
                    await scheduledService.RunManualScrapingAsync();
                    break;

                case "test":
                    await TestVenues(services);
                    break;

                case "list":
                    ListVenues(services);
                    break;

                case "help":
                case "--help":
                case "-h":
                default:
                    ShowHelp();
                    break;
            }
        }

        static async Task TestVenues(IServiceProvider services)
        {
            Log.Information("Testing venues");
            var configuration = services.GetRequiredService<IConfiguration>();
            var scrapingService = services.GetRequiredService<ScrapingService>();

            var venues = LoadVenuesFromConfiguration(configuration);
            
            Console.WriteLine($"Testing {venues.Count} venues:\n");

            foreach (var venue in venues)
            {
                Console.Write($"Testing {venue.Name}... ");
                var success = await scrapingService.TestVenueAsync(venue);
                Console.WriteLine(success ? "✓ Success" : "✗ Failed");
            }
        }

        static void ListVenues(IServiceProvider services)
        {
            Log.Information("Listing configured venues");
            var configuration = services.GetRequiredService<IConfiguration>();
            var venues = LoadVenuesFromConfiguration(configuration);

            Console.WriteLine($"Configured venues ({venues.Count}):\n");

            foreach (var venue in venues)
            {
                Console.WriteLine($"• {venue.Name}");
                Console.WriteLine($"  URL: {venue.Url}");
                Console.WriteLine($"  Address: {venue.Address}");
                Console.WriteLine($"  Scraper: {venue.ScraperType}");
                Console.WriteLine($"  Active: {venue.IsActive}");
                Console.WriteLine();
            }
        }

        static List<KCTheaterScraper.Models.TheaterVenue> LoadVenuesFromConfiguration(IConfiguration configuration)
        {
            var venues = new List<KCTheaterScraper.Models.TheaterVenue>();
            var venuesSection = configuration.GetSection("Venues");
            
            foreach (var venueSection in venuesSection.GetChildren())
            {
                var venue = new KCTheaterScraper.Models.TheaterVenue();
                venueSection.Bind(venue);
                venues.Add(venue);
            }

            return venues;
        }

        static void ShowHelp()
        {
            Console.WriteLine("KC Theater Scraper");
            Console.WriteLine("==================");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  KCTheaterScraper [command]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  scrape, run    Run scraping once and exit");
            Console.WriteLine("  test           Test all configured venues");
            Console.WriteLine("  list           List all configured venues");
            Console.WriteLine("  help           Show this help message");
            Console.WriteLine();
            Console.WriteLine("If no command is provided, runs as a scheduled service.");
            Console.WriteLine();
            Console.WriteLine("Output files are saved to the configured output directory (default: ./output)");
        }
    }
}
