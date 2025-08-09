# KC Theater Scraper

A Kansas City theater events scraper and web application that displays upcoming theater performances in the Kansas City metro area.

## Features

- **Web Scraping**: Automated scraping of theater websites to collect event data
- **Static Site Generation**: PowerShell script to generate a static website from scraped data
- **Interactive Calendar**: Month-at-a-glance calendar view with event details
- **List View**: Card-based event listings with search functionality
- **Responsive Design**: Bootstrap-based responsive design that works on all devices
- **Netlify Deployment**: Automated deployment to Netlify via GitHub Actions

## Components

### KCTheaterScraper (.NET Console App)
- Scrapes theater websites for event information
- Saves data to JSON files for the static site

### Static Site Generator (PowerShell)
- `build-static.ps1` - Generates static HTML site from scraped data
- Creates deployable static site in `static-site/` directory

### Web Interface
- **List View**: Interactive cards showing event details
- **Calendar View**: Traditional monthly calendar with events
- **Event Details**: Modal popups with comprehensive event information
- **Search**: Filter events by title, description, or venue

## Deployment

The site is automatically deployed to Netlify when changes are pushed to the main branch.

## Local Development

1. Run the scraper: `dotnet run` in the KCTheaterScraper directory
2. Generate static site: `.\build-static.ps1`
3. Deploy the `static-site` folder to any static hosting service

## Live Site

Visit the live site at your Netlify URL to see upcoming Kansas City theater events.
