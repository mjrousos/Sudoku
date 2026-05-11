# Copilot Instructions

## Project Overview

This is a **Sudoku web application** built with an **ASP.NET Core** backend and a **Vue.js** frontend, deployed via **.NET Aspire**. The PRD and planning documents live in `plans/`, and the prompt templates for generating them live in `prompts/`.

## Architecture

- **Backend**: ASP.NET Core REST API
- **Frontend**: Vue.js SPA (TypeScript, Composition API)
- **Orchestration**: .NET Aspire AppHost for local dev and deployment
- **Database**: PostgreSQL with Entity Framework Core
- **Auth**: Social login (Google, GitHub, Facebook) — login is optional for gameplay but required for leaderboard and game history
- **Storage**: Azure Blob Storage for profile pictures

## Project Structure

- `src/Sudoku.Api/` — ASP.NET Core backend (REST API)
- `src/Sudoku.Web/` — Vue.js frontend (Vite + TypeScript)
- `src/Sudoku.AppHost/` — .NET Aspire AppHost
- `src/Sudoku.ServiceDefaults/` — Shared Aspire service defaults
- `tests/Sudoku.Api.Tests/` — Backend unit and integration tests
- `tests/Sudoku.Web.Tests/` — Frontend unit tests (Vitest)
- `tests/Sudoku.E2E.Tests/` — Playwright end-to-end tests

## Key Design Decisions

- Sudoku puzzles are generated on-the-fly by the backend (not pre-stored). Each puzzle must be solvable with a unique solution.
- Game state is in-memory only — no save/resume across sessions.
- Leaderboards are segmented by difficulty (easy/medium/hard) and time window (daily/weekly/all-time).
- Using hints or solution reveal disqualifies a player from the leaderboard.
- Timer is server-authoritative — the server records start and end times. The client timer is for display only. Leaderboard times are computed server-side to prevent cheating.

## Coding Conventions

### C# (Backend)
- Use **controllers** (not minimal APIs) with proper `[ApiController]` attributes.
- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`).
- Follow standard C# naming: PascalCase for public members, `_camelCase` for private fields, PascalCase for async methods with `Async` suffix.
- Use **Problem Details** (RFC 9457) for all error responses.
- No API versioning for now — single version.

### TypeScript / Vue.js (Frontend)
- **TypeScript** for all frontend code — no plain JS files.
- **Vue 3 Composition API** with `<script setup lang="ts">` syntax.
- **Pinia** for state management.
- **Scoped styles** (`<style scoped>`) per component; use the `frontend-design` skill for overall design direction.
- Component files use **PascalCase** naming (e.g., `SudokuGrid.vue`, `LeaderBoard.vue`).
- Use the native **fetch** API for HTTP calls (no Axios).
- kebab-case for non-component files and directories.

### API Design
- RESTful endpoints under `/api/` prefix.
- Response bodies use bare objects/arrays (no wrapper envelope).
- Error responses use **Problem Details** format (`application/problem+json`).
- Use `DateTimeOffset` for all timestamps.

## Constraints

- Do **NOT** store game state in the database — keep it in-memory only.
- Do **NOT** implement email/password authentication — social login only.
- Do **NOT** pre-generate or cache puzzles — always generate on-the-fly.
- Do **NOT** use minimal APIs — use controllers.
- Do **NOT** use JavaScript — all frontend code must be TypeScript.

## Copilot Skills

This repo has custom Copilot skills in `.github/skills/`:

- **`aspire`** — Use for all Aspire AppHost and deployment work.
- **`playwright-cli`** — Use for end-to-end test automation.
- **`frontend-design`** — Use when building or styling UI components. Required for the Sudoku grid and overall visual design.

## Testing Strategy

- **Unit tests**: Both backend (C#) and frontend (TypeScript/Vitest), targeting ≥80% code coverage.
- **Integration tests**: Verify cross-component interactions (API + DB, auth flows).
- **End-to-end tests**: Playwright-based browser tests covering user workflows.
- **Visible UI changes**: When fixing an issue that results in visible UI changes, always inspect the running app with browser automation after the code change. Use Aspire to run/discover the app when applicable, then use Playwright to exercise the affected scenario and confirm the visual result, preferably with a screenshot artifact.
- Focus areas: puzzle generation algorithm correctness, authentication flows, timer accuracy.
