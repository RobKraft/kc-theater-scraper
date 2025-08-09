# Netlify Deployment Setup

## Quick Setup Instructions

### 1. Get Your Netlify Site ID and Auth Token

#### Option A: Use Existing Netlify Site
If you already have a Netlify site:
1. Go to your [Netlify dashboard](https://app.netlify.com/)
2. Click on your site
3. Go to **Site settings** → **General**
4. Copy the **Site ID** (under "Site details")

#### Option B: Create New Netlify Site
1. Go to [Netlify](https://app.netlify.com/)
2. Click "New site from Git"
3. Choose GitHub and authorize if needed
4. Select your `kc-theater-scraper` repository
5. Set build settings:
   - **Build command**: Leave empty (we handle this in GitHub Actions)
   - **Publish directory**: `KCTheaterScraper/static-site`
6. Click "Deploy site"
7. Go to **Site settings** → **General** and copy the **Site ID**

#### Get Your Netlify Auth Token
1. Go to [Netlify User Settings](https://app.netlify.com/user/applications)
2. Click **Personal access tokens**
3. Click **New access token**
4. Give it a name like "GitHub Actions Deploy"
5. Copy the token (save it somewhere safe!)

### 2. Add GitHub Secrets

1. Go to your GitHub repository: https://github.com/RobKraft/kc-theater-scraper
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add these two secrets:

   **Secret 1:**
   - Name: `NETLIFY_SITE_ID`
   - Value: Your site ID from step 1

   **Secret 2:**
   - Name: `NETLIFY_AUTH_TOKEN`
   - Value: Your auth token from step 1

### 3. Trigger Deployment

Once the secrets are set up, the deployment will automatically run when you push to the main/master branch.

You can also manually trigger it:
1. Go to **Actions** tab in your GitHub repo
2. Click on "Build and Deploy KC Theater Scraper" workflow
3. Click "Run workflow" → "Run workflow"

### 4. What the Workflow Does

The GitHub Actions workflow will:
1. Build the .NET console scraper
2. Run the scraper to get fresh theater data
3. Generate the static site using your enhanced HTML/CSS/JS
4. Deploy to Netlify automatically

Your site will update with fresh theater data every time the workflow runs!

## Troubleshooting

If deployment fails:
1. Check the **Actions** tab for error logs
2. Verify your Netlify secrets are correct
3. Make sure your Netlify site exists and the Site ID is correct

## Manual Deployment (Backup)

If you need to deploy manually:
1. Run `.\build-static.ps1` in the KCTheaterScraper directory
2. Drag and drop the `static-site` folder to Netlify dashboard
