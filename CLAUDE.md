# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FitWifFrens is a fitness commitment and accountability platform. Users create fitness challenges, stake cryptocurrency as collateral, track metrics from fitness providers (Strava, Withings, Microsoft Tasks), and participate in community accountability via Telegram polls. Users are rewarded or penalized based on goal achievement.

## Common Commands

```bash
# Build the solution
dotnet build FitWifFrens.sln

# Build just the web app
dotnet build FitWifFrens.Web/FitWifFrens.Web.csproj

# Publish for deployment
dotnet publish FitWifFrens.Web/FitWifFrens.Web.csproj -c Release

# Run the web app
dotnet run --project FitWifFrens.Web/FitWifFrens.Web.csproj

# Run the Playground (database utilities: migrations, seeding, updates)
dotnet run --project FitWifFrens.Playground/FitWifFrens.Playground.csproj
```

There are no automated tests in this repository.

## Architecture

### Solution Projects

| Project | Purpose |
|---------|---------|
| **FitWifFrens.Web** | Main ASP.NET Core 8 app — Blazor pages, API controllers, background jobs, integrations |
| **FitWifFrens.Web.Client** | Blazor WebAssembly client — interactive components with Web3/Metamask |
| **FitWifFrens.Data** | EF Core entities, `DataContext`, and PostgreSQL migrations |
| **FitWifFrens.Playground** | Worker service for running DB migrations and one-off data utilities |
| **AspNet.Security.OAuth.Withings** | Custom OAuth 2.0 provider for Withings health devices |
| **AspNet.Security.OAuth.WorldId** | Custom OAuth 2.0 provider for World ID |
| **FitWifFrens.Contracts** | Solidity smart contracts for on-chain staking and reward distribution |

### Web Project Layout

- `Components/` — Blazor pages and UI components (server-rendered + interactive)
- `Controllers/` — REST API endpoints for Telegram polls, display data, and webhooks
- `Background/` — Hangfire background services for metric sync, commitment processing, and Telegram messaging
- `Telegram/` — Telegram bot logic (poll creation, responses, summaries, notifications)

### Key Architectural Patterns

**Background Jobs (Hangfire)**: All periodic work runs via Hangfire with PostgreSQL storage. Jobs are registered in `Program.cs` and run on schedules:
- Every 5 min: Telegram poll answer updates
- Hourly: Microsoft/Strava/Withings metric sync
- Hourly (offset): Commitment period creation, goal evaluation, period completion
- Daily @ 9am: Telegram polls, weight summaries, correlation summaries

**OAuth Multi-Provider Auth**: Users authenticate via Microsoft, Strava, or Withings. Token refresh logic lives in the respective service classes. The custom `Withings` and `WorldId` OAuth providers are in their own projects.

**Metric Provider Pattern**: Each fitness data source (`StravaService`, `WithingsService`, `MicrosoftService`) implements a consistent pattern: fetch data via OAuth token → upsert `MetricValue` rows → trigger downstream commitment evaluation.

**Telegram Integration**: Webhook-driven. Telegram pushes updates to `/api/telegram/...` controllers. The `Telegram/` folder contains services for polls, responses, image generation (ScottPlot + ImageSharp), and weekly summaries.

**Commitment Lifecycle**: `CommitmentPeriod` → `CommitmentPeriodUser` → goal evaluation → Hangfire job triggers payout/penalty via Nethereum smart contract calls.

**Web3/Blockchain**: Smart contracts in Solidity (`FitWifFrens.Contracts/`). Server-side calls use Nethereum. Client-side wallet interactions (staking, deposits) use Metamask via Blazor JS interop in `FitWifFrens.Web.Client`.

### Data Model Summary

Core entities in `FitWifFrens.Data`:
- Identity: `User`, `Deposit`, `Balance`
- Metrics: `Metric`, `MetricProvider`, `MetricValue`, `UserMetricProvider`
- Commitments: `Commitment`, `CommitmentPeriod`, `CommitmentUser`, `CommitmentPeriodUser`, goal variants
- Telegram: `CommitmentTelegramPoll`, `UserTelegramPollResponse`, `CommitmentTelegramPollRule`
- Displays: `Display`, `UserDisplay`

## Configuration

Secrets are stored via .NET User Secrets (ID: `aspnet-FitWifFrens.Web-5bf2b8eb-e34d-474c-bf72-cf8ab8bbbdb2`). Required configuration keys:

```
PostgresConnection
Authentication:Microsoft:ClientId / ClientSecret
Authentication:Strava:ClientId / ClientSecret
Authentication:Withings:ClientId / ClientSecret
Services:Telegram:Token
Services:Telegram:ChatId
Services:Telegram:WebhookSecretToken
CallbackUrl
```

The default timezone is `"AUS Eastern Standard Time"` (Australia), set via `Constants.LocalTimeZoneId`.

## Database Migrations

**Do NOT create EF Core migrations.** When adding or modifying entities, only create/update the entity classes and `DataContext` configuration. The developer will create the migration manually after reviewing the changes.

## Deployment

CI/CD via GitHub Actions (`.github/workflows/master_fitwiffrens.yml`) deploys to Azure Web Apps on push to `master`.
