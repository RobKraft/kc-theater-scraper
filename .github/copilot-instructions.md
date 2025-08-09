<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# KC Theater Scraper - Copilot Instructions

This is a C# .NET 8.0 console application that scrapes theater events from Kansas City Metro area venues and aggregates them into calendar formats.

## Project Context

- **Purpose**: Scrape theater event information from multiple venue websites and create unified calendar files
- **Target Framework**: .NET 8.0
- **Output Formats**: iCalendar (.ics), JSON, and statistics
- **Architecture**: Modular scraper system with dependency injection

## Key Technologies

- **Web Scraping**: HtmlAgilityPack for HTML parsing
- **Calendar Generation**: Ical.Net for iCalendar format
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Logging**: Serilog with console and file sinks
- **Configuration**: Microsoft.Extensions.Configuration with JSON
- **Background Services**: Microsoft.Extensions.Hosting

## Architecture Patterns

- **Strategy Pattern**: Different scrapers for different venue types
- **Factory Pattern**: IHttpClientFactory for HTTP client management
- **Repository Pattern**: Configuration-based venue management
- **Background Service**: Scheduled scraping with HostedService

## Code Style Preferences

- Use async/await pattern for all I/O operations
- Implement proper error handling and logging
- Follow C# naming conventions (PascalCase for public members, camelCase for private)
- Use nullable reference types appropriately
- Prefer dependency injection over static dependencies
- Include comprehensive XML documentation for public APIs

## When Adding New Features

- Create scrapers by inheriting from `BaseTheaterScraper`
- Use the existing logging infrastructure
- Follow the configuration pattern in `appsettings.json`
- Add appropriate error handling and cancellation token support
- Consider rate limiting and respectful scraping practices

## Testing Considerations

- Test with real venue websites carefully to avoid overwhelming servers
- Implement retry logic for network failures
- Validate calendar output with standard tools
- Test with various date/time formats from different venues

## Deployment Notes

- Can run as a console application or Windows Service
- Requires network access to scrape venue websites
- Outputs files to configurable directory
- Logs are stored in rolling daily files
