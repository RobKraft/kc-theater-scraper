# KC Theater Scraper

A C# .NET service that scrapes theater calendars from venues across the Kansas City Metro area and aggregates them into a single calendar.

## Features

- **Multi-venue Scraping**: Scrapes dozens of theater websites in the KC Metro area
- **Flexible Architecture**: Modular scraper system that can handle different website structures
- **Calendar Export**: Generates iCalendar (.ics) files compatible with Google Calendar, Outlook, and other calendar apps
- **Scheduling**: Runs automatically every 6 hours to keep event data current
- **JSON Export**: Also saves detailed event data in JSON format
- **Statistics**: Generates scraping statistics and venue analytics
- **Command Line Interface**: Can be run manually or as a scheduled service

## Supported Venues

The scraper is configured to work with major Kansas City theater venues including:

- Kauffman Center for the Performing Arts
- Kansas City Repertory Theatre
- The Unicorn Theatre
- Kansas City Young Audiences
- The Coterie Theatre
- Starlight Theatre
- Music Hall Kansas City
- The Folly Theater
- Uptown Theater
- KC Melting Pot Theatre
- The Living Room Theatre
- Quality Hill Playhouse
- Theatre in the Park
- New Theatre Restaurant
- Johnson County Community College Theatre

## Installation

### Prerequisites

- .NET 8.0 SDK or later
- Windows, macOS, or Linux

### Building the Project

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd KCTheaterScraper
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Build the project:
   ```bash
   dotnet build
   ```

## Usage

### Command Line Options

```bash
# Run scraping once and exit
dotnet run scrape

# Test all configured venues
dotnet run test

# List all configured venues
dotnet run list

# Show help
dotnet run help

# Run as a scheduled service (default)
dotnet run
```

### Configuration

Edit `appsettings.json` to:
- Add or modify venue configurations
- Adjust scraping settings (delays, timeouts, etc.)
- Change output directory and file names

Example venue configuration:
```json
{
  "Name": "Example Theater",
  "Url": "https://example-theater.com/events",
  "Address": "123 Main St, Kansas City, MO 64111",
  "ScraperType": "generic",
  "IsActive": true,
  "ScraperConfig": {
    "EventSelector": ".event-item",
    "TitleSelector": "h3.title",
    "DateSelector": ".date"
  }
}
```

### Output Files

The scraper generates several output files in the configured output directory:

- `kc-theater-events.ics` - iCalendar file with all events
- `kc-theater-events.json` - Detailed event data in JSON format
- `scraping-statistics.json` - Statistics about the scraping run
- `logs/` - Log files with detailed scraping information

## Architecture

### Scraper Types

1. **Specific Scrapers**: Custom scrapers for venues with known website structures
   - `KauffmanCenterScraper`
   - `KCRepScraper`

2. **Generic Scraper**: Fallback scraper that attempts to extract events from any theater website using common HTML patterns

### Services

- **ScrapingService**: Orchestrates the scraping process across all venues
- **CalendarService**: Handles iCalendar generation and event filtering
- **ScheduledScrapingService**: Background service that runs scraping on a schedule

### Models

- **TheaterEvent**: Represents a single theater performance with all relevant details
- **TheaterVenue**: Configuration for a theater venue including scraping parameters

## Adding New Venues

1. Add the venue configuration to `appsettings.json` in the `Venues` array
2. Set `ScraperType` to "generic" for most venues
3. For complex sites, create a custom scraper implementing `ITheaterScraper`
4. Test the new venue with `dotnet run test`

## Logging

The application uses Serilog for logging with output to:
- Console (for immediate feedback)
- File (rolling daily logs in the `logs/` directory)

Log levels can be configured in `appsettings.json`.

## Development

### Project Structure

```
KCTheaterScraper/
├── Configuration/          # Configuration classes
├── Models/                 # Data models
├── Scrapers/              # Scraper implementations
├── Services/              # Business logic services
├── appsettings.json       # Configuration file
└── Program.cs             # Application entry point
```

### Creating Custom Scrapers

To create a scraper for a specific venue:

1. Inherit from `BaseTheaterScraper`
2. Implement the required methods:
   - `CanScrape()` - Determines if this scraper can handle a venue
   - `ScrapeEventsAsync()` - Extracts events from the venue's website
3. Register the scraper in `Program.cs`

Example:
```csharp
public class CustomVenueScraper : BaseTheaterScraper
{
    public override string ScraperName => "Custom Venue";
    
    public override bool CanScrape(TheaterVenue venue)
    {
        return venue.ScraperType == "custom" || 
               venue.Url.Contains("customvenue.com");
    }
    
    public override async Task<List<TheaterEvent>> ScrapeEventsAsync(
        TheaterVenue venue, CancellationToken cancellationToken = default)
    {
        // Implementation here
    }
}
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Disclaimer

This tool is for educational and personal use. Please respect the terms of service of the websites being scraped and implement appropriate delays and rate limiting. The authors are not responsible for any misuse of this software.
