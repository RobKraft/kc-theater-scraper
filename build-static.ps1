# Static Site Builder Script for KC Theater Web App
# This script builds the web app and generates static files for Netlify deployment

param(
    [string]$OutputPath = ".\static-site",
    [string]$DataPath = ".\output"
)

Write-Host "Building static site for KC Theater Web App..." -ForegroundColor Green

# Ensure output directory exists
if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

# Copy data files
Write-Host "Copying data files..." -ForegroundColor Yellow
$dataOutputPath = Join-Path $OutputPath "data"
New-Item -ItemType Directory -Path $dataOutputPath -Force | Out-Null

if (Test-Path $DataPath) {
    Copy-Item "$DataPath\*.json" -Destination $dataOutputPath -Force
    Write-Host "Data files copied to $dataOutputPath" -ForegroundColor Green
} else {
    Write-Warning "Data path not found: $DataPath"
}

# Copy web app static files (wwwroot)
Write-Host "Copying static assets..." -ForegroundColor Yellow
$webAppPath = ".\KCTheaterWeb"
$wwwrootSource = Join-Path $webAppPath "wwwroot"

if (Test-Path $wwwrootSource) {
    Copy-Item "$wwwrootSource\*" -Destination $OutputPath -Recurse -Force
    Write-Host "Static assets copied from $wwwrootSource" -ForegroundColor Green
} else {
    Write-Warning "wwwroot not found: $wwwrootSource"
}

# Create a simple index.html that loads the data and displays it
Write-Host "Generating static HTML..." -ForegroundColor Yellow

$indexHtml = @"
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Kansas City Theater Events</title>
    <link href="css/bootstrap.min.css" rel="stylesheet" />
    <link href="css/site.css" rel="stylesheet" />
    <link href="lib/bootstrap/dist/css/bootstrap.min.css" rel="stylesheet" />
</head>
<body>
    <header>
        <nav class="navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3">
            <div class="container-fluid">
                <a class="navbar-brand" href="/">KC Theater Events</a>
                <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target=".navbar-collapse">
                    <span class="navbar-toggler-icon"></span>
                </button>
                <div class="navbar-collapse collapse d-sm-inline-flex justify-content-between">
                    <ul class="navbar-nav flex-grow-1">
                        <li class="nav-item">
                            <a class="nav-link text-dark" href="#" onclick="showListView()">List View</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link text-dark" href="#" onclick="showCalendarView()">Calendar</a>
                        </li>
                    </ul>
                </div>
            </div>
        </nav>
    </header>
    <div class="container">
        <main role="main" class="pb-3">
            <div id="loading" class="text-center">
                <div class="spinner-border" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <p>Loading theater events...</p>
            </div>
            
            <div id="content" style="display: none;">
                <div class="row">
                    <div class="col-md-8">
                        <h1>Kansas City Theater Events</h1>
                        <p>Discover the latest plays and performances in the Kansas City metro area.</p>
                    </div>
                    <div class="col-md-4">
                        <div class="input-group mb-3">
                            <input type="text" class="form-control" id="searchInput" placeholder="Search events...">
                            <button class="btn btn-outline-secondary" type="button" onclick="searchEvents()">Search</button>
                        </div>
                    </div>
                </div>
                
                <div id="list-view">
                    <h2>Upcoming Events</h2>
                    <div id="events-list" class="row"></div>
                </div>
                
                <div id="calendar-view" style="display: none;">
                    <h2>Calendar View</h2>
                    <div id="events-calendar"></div>
                </div>
            </div>
        </main>
    </div>

    <footer class="border-top footer text-muted">
        <div class="container">
            &copy; 2025 - KC Theater Events - <a href="https://github.com/robkraft/kc-theater-scraper">GitHub</a>
        </div>
    </footer>

    <script src="lib/jquery/dist/jquery.min.js"></script>
    <script src="lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
    <script>
        let allEvents = [];
        
        // Load events data
        fetch('./data/kc-theater-events.json')
            .then(response => response.json())
            .then(data => {
                allEvents = data;
                displayEvents(allEvents);
                document.getElementById('loading').style.display = 'none';
                document.getElementById('content').style.display = 'block';
            })
            .catch(error => {
                console.error('Error loading events:', error);
                document.getElementById('loading').innerHTML = '<p class="text-danger">Error loading events. Please try again later.</p>';
            });

        function displayEvents(events) {
            const eventsList = document.getElementById('events-list');
            eventsList.innerHTML = '';
            
            if (events.length === 0) {
                eventsList.innerHTML = '<div class="col-12"><p class="text-muted">No events found.</p></div>';
                return;
            }
            
            events.forEach(event => {
                const eventCard = `
                    <div class="col-md-6 col-lg-4 mb-4">
                        <div class="card h-100">
                            <div class="card-body">
                                <h5 class="card-title">`+event.title+`</h5>
                                <h6 class="card-subtitle mb-2 text-muted">`+event.venueName+`</h6>
                                <p class="card-text">`+event.description+`</p>
                                <p class="card-text">
                                    <small class="text-muted">
                                        `+new Date(event.startDate).toLocaleDateString()+` at `+new Date(event.startDate).toLocaleTimeString()+`
                                    </small>
                                </p>
                            </div>
                        </div>
                    </div>
                `;
                eventsList.innerHTML += eventCard;
            });
        }

        function searchEvents() {
            const searchTerm = document.getElementById('searchInput').value.toLowerCase();
            if (searchTerm === '') {
                displayEvents(allEvents);
                return;
            }
            
            const filteredEvents = allEvents.filter(event => 
                event.title.toLowerCase().includes(searchTerm) ||
                event.description.toLowerCase().includes(searchTerm) ||
                event.venueName.toLowerCase().includes(searchTerm)
            );
            
            displayEvents(filteredEvents);
        }

        function showListView() {
            document.getElementById('list-view').style.display = 'block';
            document.getElementById('calendar-view').style.display = 'none';
        }

        function showCalendarView() {
            document.getElementById('list-view').style.display = 'none';
            document.getElementById('calendar-view').style.display = 'block';
            
            // Simple calendar implementation
            const calendar = document.getElementById('events-calendar');
            calendar.innerHTML = '<p class="text-info">Calendar view coming soon!</p>';
        }

        // Allow search on Enter key
        document.getElementById('searchInput').addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                searchEvents();
            }
        });
    </script>
</body>
</html>
"@

$indexPath = Join-Path $OutputPath "index.html"
$indexHtml | Out-File -FilePath $indexPath -Encoding UTF8
Write-Host "Generated index.html at $indexPath" -ForegroundColor Green

# Create netlify.toml for deployment configuration
$netlifyToml = @"
[build]
  publish = "."

[[redirects]]
  from = "/*"
  to = "/index.html"
  status = 200

[build.environment]
  NODE_VERSION = "18"

[[headers]]
  for = "*.json"
  [headers.values]
    Content-Type = "application/json"
    Cache-Control = "public, max-age=3600"

[[headers]]
  for = "*.css"
  [headers.values]
    Cache-Control = "public, max-age=86400"

[[headers]]
  for = "*.js"
  [headers.values]
    Cache-Control = "public, max-age=86400"
"@

$netlifyPath = Join-Path $OutputPath "netlify.toml"
$netlifyToml | Out-File -FilePath $netlifyPath -Encoding UTF8
Write-Host "Generated netlify.toml at $netlifyPath" -ForegroundColor Green

Write-Host "Static site build complete! Output: $OutputPath" -ForegroundColor Green
Write-Host "Files generated:" -ForegroundColor Cyan
Get-ChildItem $OutputPath -Recurse | ForEach-Object { Write-Host "  $($_.FullName)" -ForegroundColor Gray }
