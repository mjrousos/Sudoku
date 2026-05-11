# Sudoku Web Game

Full-stack Sudoku web application built from `plans\SudokuWebGame.prd.md`.

## Stack

- ASP.NET Core REST API with controllers, Problem Details, cookie auth, anti-forgery, EF Core, PostgreSQL, and Azure Blob-compatible profile picture storage.
- Vue 3 + Vite + TypeScript SPA with Pinia, Vue Router, native `fetch`, accessible Sudoku board controls, history, profile, and leaderboards.
- .NET Aspire AppHost for local PostgreSQL, Azurite Blob Storage, API, and Vite frontend orchestration.

## Repository layout

- `src\Sudoku.Api` - backend API.
- `src\Sudoku.Web` - Vue frontend.
- `src\Sudoku.AppHost` - Aspire orchestration.
- `src\Sudoku.ServiceDefaults` - shared Aspire service defaults.
- `tests\Sudoku.Api.Tests` - backend unit/integration tests.
- `tests\Sudoku.Web.Tests` - frontend unit tests.
- `tests\Sudoku.E2E.Tests` - Playwright E2E tests.
- `plans\` and `prompts\` - PRD and prompt/reference material.

## Local setup

Prerequisites: .NET 10 SDK, Node.js 24/npm, Docker, Aspire CLI, and Playwright browsers.

```powershell
dotnet restore Sudoku.slnx
dotnet tool restore
npm install
npx playwright install chromium
```

Run the distributed app:

```powershell
aspire run --project src\Sudoku.AppHost\Sudoku.AppHost.csproj --non-interactive
```

The AppHost starts PostgreSQL, Azurite, the API, and the Vite frontend. Active games are intentionally in-memory; server restarts or idle eviction can lose an active puzzle, and the UI treats that as a normal "start a new puzzle" path.

## Tests and validation

```powershell
dotnet test Sudoku.slnx --verbosity minimal
npm run build:web
npm run test:web
npm run test:e2e
```

E2E tests launch Vite and mock API contracts for stable browser coverage. For full-stack E2E runs, enable the deterministic non-production generator with `E2E__UseDeterministicGenerator=true`; `Program.cs` rejects that setting in production.

## Configuration

Production configuration must provide:

- `ConnectionStrings__sudokudb` for PostgreSQL.
- `ConnectionStrings__profile-pictures` or `ConnectionStrings__storage` for Blob Storage.
- `Authentication__Google__ClientId` and `Authentication__Google__ClientSecret`.
- `Authentication__GitHub__ClientId` and `Authentication__GitHub__ClientSecret`.
- `Authentication__Facebook__ClientId` and `Authentication__Facebook__ClientSecret`.
- `Cors__AllowedOrigins__0` when serving the SPA and API from different origins.

OAuth credentials and storage connection strings are secrets and must not be committed. Cookie and anti-forgery cookies use secure production defaults and HSTS is enabled outside development.

## Database and storage

EF Core migrations live under `src\Sudoku.Api\Data\Migrations`. Apply migrations as an explicit deployment step:

```powershell
dotnet dotnet-ef database update --project src\Sudoku.Api\Sudoku.Api.csproj --startup-project src\Sudoku.Api\Sudoku.Api.csproj
```

Profile pictures are stored in a private `profile-pictures` blob container and served through backend-proxied `/api/users/{userId}/profile-picture` URLs. Uploads are normalized to 256x256 PNG with SkiaSharp (MIT license).

## Aspire publish

Validate publish artifacts locally:

```powershell
aspire publish --project src\Sudoku.AppHost\Sudoku.AppHost.csproj --output-path aspire-output --non-interactive
```

The current AppHost publishes Azure Bicep for storage resources and leaves compute hosting flexible. Database migration/rollback remains a deliberate deployment operation rather than hidden startup mutation.

## Implemented features

- Anonymous Sudoku play with server-generated unique puzzles, hints, reveal, notes, undo/redo, conflict highlighting, keyboard/touch controls, and server-authoritative completion timing.
- Cookie/social-auth infrastructure for Google, GitHub, and Facebook, plus development-only test sign-in.
- Profile management with username validation, provider linking metadata, primary provider selection, avatar upload/delete, data export, sign-out, and account deletion.
- Authenticated completion persistence, private history/stats, public leaderboards, best-per-player ranking, and disqualification for hints/reveals.
- CI workflow for formatting, backend tests, frontend build/unit tests, and Playwright E2E.
