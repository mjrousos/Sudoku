# Sudoku Web Game — Product Requirements Document

**Status:** Draft v1.0
**Last updated:** 2026-05-08
**Owners:** Sudoku Web Game project team

---

## 1. Overview

### 1.1 Purpose

Sudoku Web Game is a browser-based application that lets anyone play classic 9×9 Sudoku puzzles instantly, with an optional account system that unlocks history tracking and competitive leaderboards. The product targets casual players who want a fast, frictionless puzzle and engaged players who want to track their improvement and compete on time.

### 1.2 Goals

- Anyone can land on the home page and start playing a freshly generated puzzle in under 5 seconds with no sign-up required.
- Authenticated players can review their personal history and compete on per-difficulty, per-time-window leaderboards.
- The puzzle and timing systems are correct and tamper-resistant: every puzzle has a unique solution, and leaderboard times reflect server-measured play time.
- The app is responsive, accessible (WCAG 2.1 AA), and visually polished using a modern minimalist aesthetic.

### 1.3 Non-Goals

- No save/resume of puzzles across sessions. Game state is in-memory only.
- No email/password registration. Social login only.
- No pre-generated puzzle library. Puzzles are generated on demand.
- No multiplayer/co-op play, chat, friending, or social features beyond public leaderboards.
- No monetization features (ads, subscriptions, in-app purchases) in v1.
- No native mobile apps; the responsive web app covers mobile.
- No localization in v1 (English only).
- No PWA / offline play in v1.
- No daily-challenge / shared-seed puzzle mode in v1.

### 1.4 Target Users & Primary Scenarios

- **Casual visitor (anonymous):** "I want to play one quick puzzle." Lands on home, picks a difficulty, plays, sees their time, optionally plays again.
- **Returning enthusiast (authenticated):** "I want my times to count." Signs in via social login, plays daily, checks personal history and weekly leaderboards.
- **Competitive solver (authenticated):** "I want to be on the all-time leaderboard for hard puzzles." Plays repeatedly without using hints/reveal, watches their rank.

---

## 2. Features

### 2.1 Puzzle Generation

- The backend generates puzzles on demand for three difficulty levels:
  - **Easy:** ~38–45 given clues.
  - **Medium:** ~30–35 given clues.
  - **Hard:** ~24–28 given clues.
- Difficulty is defined primarily by **clue count**. The exact thresholds are tunable but must be documented and consistent.
- Every generated puzzle MUST:
  - Have a single, unique valid solution (verified by the generator before being returned).
  - Be generated within a soft target of 1 second p95 server-side; hard cap of 3 seconds before the request fails with a Problem Details response and is retried.
- Generation algorithm requirements:
  - Start from a fully solved valid grid (e.g., randomized backtracking fill).
  - Symmetrically or randomly remove cells while re-running a uniqueness solver after each removal; revert if uniqueness is lost.
  - The generator MUST NOT be seeded by client input. The server stores the puzzle's complete solution server-side, keyed by a server-issued game ID, and only returns the puzzle (givens) to the client.
- Puzzles and solutions are NOT persisted to the database. They live only in the active game's in-memory state.

### 2.2 Gameplay

- 9×9 grid with standard Sudoku rules: digits 1–9, no duplicates in any row, column, or 3×3 box.
- Cell input: keyboard digits (1–9), backspace/delete to clear, and on-screen number pad for touch users.
- Cell navigation: arrow keys, Tab, click/tap.
- **Conflict highlighting:** When a value placed by the user conflicts with another value (same row, column, or box), all conflicting cells are visually flagged in real time. Conflict detection runs on the client; the server is the source of truth on submission.
- **Scratch / pencil-mark mode:** A toggle (e.g., keyboard shortcut `N` and a UI button) switches input between value mode and notes mode. In notes mode, entered digits 1–9 are toggled as small candidate marks within the cell. Notes are auto-cleared from a cell when a final value is set in that cell.
- **Undo / redo:** At least 50 steps of undo/redo for value and note actions (UI-only state, not persisted).
- **Auto-completion check:** When the grid is full and valid, the puzzle is automatically marked complete; no manual "submit" button is required. The client sends a completion request to the server, which validates against the stored solution and finalizes the timer.
- **Erase / clear cell:** Backspace, delete, or an on-screen erase button clears the selected (non-given) cell.
- **New puzzle:** A "New Puzzle" control is always available; starting a new puzzle abandons the current game.

### 2.3 Timer

- The timer is **server-authoritative**.
  - On `POST /api/games`, the server records `startedAt` (`DateTimeOffset`).
  - On `POST /api/games/{id}/complete`, the server records `completedAt` and computes `elapsed = completedAt - startedAt`.
  - The client displays a local timer for UX, synchronized to `startedAt` returned by the server, but the server's computed elapsed is the canonical value used for leaderboard and history.
- Pausing is not supported in v1. (Out of scope.)
- If a player abandons a game (closes tab, starts a new puzzle), the in-memory game state is dropped and no time is recorded.

### 2.4 Hints and Solution Reveal

- **Hint:** The user selects an empty cell and clicks "Hint." The server returns the correct value for that cell from the stored solution. The cell is filled and visually marked as hinted.
- **Solution reveal:** A "Reveal solution" button (with a confirmation dialog) fills the entire grid from the stored solution and ends the game.
- Both features set a `disqualifiedFromLeaderboard = true` flag on the active game. A disqualified game:
  - Still records to game history (with a "hint used" or "revealed" badge).
  - Is excluded from all leaderboards.
- The hint count per game is tracked and shown in history.

### 2.5 Authentication and Profile

- Login is **optional** to play. Authentication is required for:
  - Saving and viewing personal game history.
  - Submitting qualifying times to leaderboards.
  - Viewing or updating the personalized profile page.
- Supported social providers (v1): **Google**, **GitHub**, **Facebook**.
  - No email/password authentication is offered.
- On first sign-in via any provider, the system:
  - Creates a user record keyed by `(provider, providerSubjectId)`.
  - Pre-populates `displayName` and `email` from the provider profile when available.
  - Prompts the user to confirm/edit a public **username** (unique, 3–20 chars, `[a-zA-Z0-9_-]`). This username is what appears on leaderboards.
- Profile fields:
  - `username` (public, unique, required).
  - `email` (private, sourced from provider, editable only insofar as the user can switch primary linked provider; not user-typed).
  - `profilePictureUrl` (optional).
- **Profile pictures:**
  - Stored in **Azure Blob Storage** in a dedicated container (e.g., `profile-pictures`) using server-generated blob names.
  - Only the blob URL (or blob name) is stored in the database.
  - Upload constraints: PNG/JPEG/WebP, ≤ 2 MB, square crop enforced client-side; server re-validates and re-encodes to a normalized size (e.g., 256×256).
  - Default avatar (initials-based or generic icon) is shown when no picture is set.
- **Account management (GDPR-style controls):**
  - Users can request **account deletion**: removes user record, history, leaderboard entries, and profile picture blob.
  - Users can request **data export**: a JSON download of their profile, history, and leaderboard entries.
  - Both flows are accessible from the profile page; deletion requires a confirmation dialog.
- Sign-out clears the session cookie and returns the user to an anonymous state.

### 2.6 Game History

- For each completed game by an authenticated user, the system stores:
  - Game ID, difficulty, `startedAt`, `completedAt`, elapsed time.
  - Hint count, `solutionRevealed` flag, `disqualifiedFromLeaderboard` flag.
- Anonymous games are **not** recorded.
- The history view supports:
  - Filtering by difficulty.
  - Sorting by date or elapsed time.
  - Showing personal-best per difficulty.
  - Pagination (e.g., 25 per page).
- Aggregate stats shown on the profile page: total games, games per difficulty, best time per difficulty, average time per difficulty (excluding disqualified games), current streak (consecutive days played).

### 2.7 Leaderboards

- Leaderboards are **public** (visible to anonymous and authenticated users).
- Three difficulty leaderboards: Easy, Medium, Hard.
- Three time windows per difficulty: **Daily**, **Weekly**, **All-Time**, for a total of 9 leaderboards.
  - Daily window: completed games where `completedAt` falls within the current UTC day.
  - Weekly window: current ISO week (Monday 00:00 UTC – Sunday 23:59:59 UTC).
  - All-time: no window.
- Each leaderboard shows the top 100 entries: rank, username, profile picture (small), elapsed time, completion timestamp.
- Disqualified games (hint used or solution revealed) are **excluded**.
- A user can appear multiple times on a leaderboard (each qualifying game is its own entry). The personal-best filter is available as a UI toggle ("Show only best time per player").
- An authenticated viewer sees their own rank highlighted, even if outside the top 100.

---

## 3. User Interface

### 3.1 Design Direction

- **Theme:** Modern minimalist. Clean layout with generous whitespace, the puzzle grid as the visual focal point, restrained chrome around it.
- **Modes:** Light and dark themes, switchable from the header. The user's choice is persisted in `localStorage` and respects `prefers-color-scheme` on first visit.
- **Color palette (recommended starting point — final values to be tuned by the design pass):**
  - Light: background `#FAFAF7` (warm off-white), surface `#FFFFFF`, primary text `#1F2328`, secondary text `#5C6370`, grid lines `#D5D7DC`, thick block lines `#1F2328`, accent `#3B82F6` (calm blue), error/conflict `#DC2626`, success `#059669`, hint highlight `#F59E0B`.
  - Dark: background `#0F1115`, surface `#171A21`, primary text `#E6E8EC`, secondary text `#9AA0A6`, grid lines `#2A2E37`, thick block lines `#E6E8EC`, accent `#60A5FA`, error `#F87171`, success `#34D399`, hint highlight `#FBBF24`.
- **Typography:** A single sans-serif family for UI (e.g., Inter or system UI stack) and a tabular-figures font for the timer and grid digits to ensure stable widths.
- The `frontend-design` skill MUST be consulted when implementing the UI to ensure the visual treatment is distinctive and avoids generic AI styling. Implementers should read the skill before starting on the grid, header, and leaderboard layouts.

### 3.2 Information Architecture

- **Header (persistent):** Logo/home link, navigation to Play, Leaderboards, How to Play; right side: theme toggle, profile menu (sign in / username + avatar dropdown).
- **Pages:**
  - `/` — **Play** (default landing). Difficulty selector + active game.
  - `/leaderboards` — Leaderboards with difficulty + time-window tabs.
  - `/history` — Authenticated only. Personal game history.
  - `/profile` — Authenticated only. Username, picture, account controls (export, delete), sign-out.
  - `/sign-in` — Provider buttons (Google, GitHub, Facebook).
  - `/how-to-play` — Brief rules + keyboard shortcuts.
  - 404 and error pages with consistent styling.

### 3.3 Sudoku Grid (key component)

- The grid dominates the play screen. Required visuals:
  - Strong outer border and 3×3 block dividers; thinner cell dividers.
  - Given cells: bold weight, slightly muted color, non-editable.
  - User-entered cells: regular weight, primary text color.
  - Hinted cells: distinct accent color (hint highlight) and a small icon marker.
  - Selected cell: clear focus ring (also visible to keyboard users).
  - Same-value highlight: when a cell is selected, all other cells with the same value are subtly tinted.
  - Peer highlight: same row, column, and box of the selected cell are subtly tinted.
  - Conflict cells: error color background tint plus a red underline on the conflicting digit.
  - Pencil marks (notes): small grid of 9 candidate digits inside the cell, monospaced/tabular.
- Smooth 150–200ms transitions for selection and highlight changes.

### 3.4 Controls and Layout

- **Desktop:** Grid centered; right-rail panel with timer, difficulty indicator, and action buttons (New Puzzle, Hint, Reveal, Notes toggle, Undo/Redo). On-screen number pad hidden by default; shown if user clicks a "Show pad" toggle.
- **Mobile:** Grid sized to viewport width; below the grid: timer + difficulty; below that: action buttons in a horizontal scroll/wrap; on-screen number pad always visible (large touch targets, ≥ 44×44 CSS px).
- The timer is always visible on the play screen, with `font-variant-numeric: tabular-nums` to prevent jitter.

### 3.5 Responsiveness

- Breakpoints (recommended): mobile `< 640px`, tablet `640–1024px`, desktop `≥ 1024px`.
- The application MUST be fully usable on a 360px-wide viewport.

### 3.6 Accessibility (WCAG 2.1 AA)

- Color contrast ratios MUST meet AA in both light and dark themes.
- Full keyboard navigation: Arrow keys move between cells, Tab moves focus between major regions, all controls reachable without a mouse.
- Visible focus indicators on every interactive element.
- The grid is exposed as an ARIA grid with `role="grid"`, `role="row"`, `role="gridcell"`. Each cell announces row/column, value, given/user-entered status, and conflict status to screen readers.
- All buttons have accessible names (`aria-label` where icon-only).
- Live regions announce game completion, conflict introduction, and hint usage.
- Respect `prefers-reduced-motion`: disable non-essential transitions/animations.
- The default color palette MUST NOT be the sole indicator of state (conflicts also use an underline; hints also use an icon).

---

## 4. Technical Requirements

### 4.1 Architecture Overview

- **Backend:** ASP.NET Core (latest LTS) REST API.
- **Frontend:** Vue 3 SPA built with Vite, written in TypeScript, using the Composition API.
- **Orchestration:** .NET Aspire AppHost orchestrates the API, frontend, PostgreSQL, and supporting services for local development and deployment. The `aspire` skill MUST be consulted when configuring or operating the AppHost.
- **Database:** PostgreSQL accessed via Entity Framework Core.
- **Object storage:** Azure Blob Storage for profile pictures.
- **Authentication:** ASP.NET Core authentication with OAuth/OpenID Connect handlers for Google, GitHub, and Facebook. Session is maintained via an HTTP-only, SameSite=Lax, Secure cookie.

### 4.2 Solution Layout

```
src/
  Sudoku.Api/              # ASP.NET Core REST API (controllers)
  Sudoku.Web/              # Vue 3 + Vite + TypeScript SPA
  Sudoku.AppHost/          # .NET Aspire AppHost
  Sudoku.ServiceDefaults/  # Shared Aspire service defaults
tests/
  Sudoku.Api.Tests/        # Backend unit + integration tests (xUnit)
  Sudoku.Web.Tests/        # Frontend unit tests (Vitest)
  Sudoku.E2E.Tests/        # Playwright end-to-end tests
plans/
  SudokuWebGame.prd.md     # This document
```

### 4.3 Backend (ASP.NET Core)

- Use **controllers** (not minimal APIs), each annotated with `[ApiController]` and `[Route("api/[controller]")]`.
- Nullable reference types enabled (`<Nullable>enable</Nullable>`); treat warnings as errors in CI.
- Naming: PascalCase for public members, `_camelCase` for private fields, async methods suffixed `Async`.
- All error responses use **Problem Details** (RFC 9457, `application/problem+json`).
- All timestamps are `DateTimeOffset` (UTC) at the API boundary.
- No API versioning in v1; a single unversioned API surface under `/api/`.
- Dependency injection for all services (puzzle generator, game store, repositories).
- Logging via `ILogger<T>`; structured logs.
- OpenAPI/Swagger document generated for development; not necessarily exposed in production.

#### 4.3.1 In-memory game store

- Active games are stored in a thread-safe in-memory dictionary keyed by `gameId` (GUID). Each entry holds:
  - `gameId`, `userId?` (null for anonymous), `difficulty`, `puzzle` (givens), `solution`, `startedAt`, `disqualifiedFromLeaderboard`, `hintCount`, `solutionRevealed`.
- Entries are evicted after a configurable idle timeout (default 2 hours) and on explicit completion or abandonment.
- The store interface is abstracted (`IGameStore`) so it can be swapped for a distributed cache later if needed; in v1 a single in-process implementation is sufficient.

#### 4.3.2 REST API surface (initial)

All endpoints under `/api`. Response bodies are bare objects/arrays (no envelope). Errors use Problem Details.

- `POST /api/games` — Body: `{ difficulty: "easy" | "medium" | "hard" }`. Creates a new game; returns `{ gameId, difficulty, puzzle: number[81], startedAt }`.
- `GET /api/games/{gameId}` — Returns current server-side game metadata (without solution): `{ gameId, difficulty, puzzle, startedAt, hintCount, disqualifiedFromLeaderboard }`. 404 if not found / expired.
- `POST /api/games/{gameId}/hint` — Body: `{ index: 0..80 }`. Returns `{ index, value }` and sets `disqualifiedFromLeaderboard = true`.
- `POST /api/games/{gameId}/reveal` — Returns the full solution; sets `solutionRevealed` and `disqualifiedFromLeaderboard`.
- `POST /api/games/{gameId}/complete` — Body: `{ board: number[81] }`. Server validates board against stored solution; on success returns `{ elapsedMs, completedAt, qualifiedForLeaderboard, leaderboardRank? }`. On mismatch, returns 422 with Problem Details and the game remains active.
- `GET /api/leaderboards/{difficulty}/{window}` — `window` ∈ `daily | weekly | all-time`. Optional query: `?bestPerPlayer=true`, `?limit=100`, `?offset=0`. Returns array of `{ rank, username, profilePictureUrl, elapsedMs, completedAt }`.
- `GET /api/me` — Authenticated. Returns current user profile.
- `PUT /api/me` — Authenticated. Body: `{ username }`. Validates uniqueness/format.
- `POST /api/me/profile-picture` — Authenticated. Multipart upload. Returns `{ profilePictureUrl }`.
- `DELETE /api/me/profile-picture` — Authenticated. Removes picture.
- `GET /api/me/history` — Authenticated. Paginated list of completed games. Query: `?difficulty=`, `?sort=`, `?limit=`, `?offset=`.
- `GET /api/me/stats` — Authenticated. Aggregate stats.
- `POST /api/me/export` — Authenticated. Returns JSON dump of the user's data.
- `DELETE /api/me` — Authenticated. Hard-deletes the user and all associated rows + profile picture blob.
- `GET /api/auth/providers` — Anonymous. Lists available login providers.
- `GET /api/auth/sign-in/{provider}` — Initiates OAuth flow.
- `POST /api/auth/sign-out` — Ends session.

### 4.4 Frontend (Vue.js)

- Vue 3 + Vite + TypeScript. **No plain JavaScript files.**
- `<script setup lang="ts">` syntax for all components.
- **Pinia** for state management. Stores include: `gameStore` (active puzzle, notes, undo stack, timer offset), `authStore` (current user), `leaderboardStore`, `themeStore`.
- **Vue Router** for navigation.
- Component file naming: **PascalCase** (`SudokuGrid.vue`, `LeaderBoard.vue`). Non-component files and directories use **kebab-case**.
- Styles are **scoped** per component (`<style scoped>`). Shared design tokens (colors, spacing, typography) are exposed as CSS custom properties in a global stylesheet.
- HTTP via the native **fetch** API. No Axios. A small typed client wrapper handles base URL, credentials, and Problem Details parsing.
- Form validation client-side, but server is the source of truth.
- Build output is served as static assets in production; served by Vite dev server during local development under Aspire.

### 4.5 Database (PostgreSQL + EF Core)

- ORM: **Entity Framework Core** (Npgsql provider).
- Migrations are checked into source control; applied automatically in non-production environments via Aspire-managed startup, and applied via a deliberate deployment step in production.
- Schema (logical):
  - `users` — `id (uuid, pk)`, `username (citext, unique)`, `email (text)`, `profile_picture_blob_name (text, nullable)`, `created_at (timestamptz)`, `last_seen_at (timestamptz)`.
  - `user_logins` — `id (uuid, pk)`, `user_id (fk users)`, `provider (text)`, `provider_subject_id (text)`, `created_at (timestamptz)`. Unique on `(provider, provider_subject_id)`.
  - `completed_games` — `id (uuid, pk)`, `user_id (fk users)`, `difficulty (text: 'easy'|'medium'|'hard')`, `started_at (timestamptz)`, `completed_at (timestamptz)`, `elapsed_ms (int)`, `hint_count (int)`, `solution_revealed (bool)`, `qualified_for_leaderboard (bool)`. Indexes on `(difficulty, qualified_for_leaderboard, completed_at, elapsed_ms)` to support leaderboard queries.
- Leaderboard data is stored as qualifying `completed_games` rows. No separate leaderboard table is required in v1 unless later performance testing shows a need for denormalized ranking snapshots.
- **Game state (in-progress puzzles) is NEVER stored in the database.** Only completed games are persisted.

### 4.6 Azure Blob Storage

- A single container `profile-pictures` with private access; images are served via a backend-issued short-lived SAS URL or proxied through the API.
- Blob naming: `{userId}/{guid}.{ext}` to allow easy per-user cleanup on account deletion.
- Server validates uploaded content type and dimensions, re-encodes to PNG at 256×256, and uploads. The original is discarded.

### 4.7 Authentication Details

- ASP.NET Core authentication middleware configured with handlers for Google, GitHub, and Facebook.
- Provider client IDs/secrets are loaded from configuration (user secrets in development, Azure Key Vault or equivalent in production). They are NEVER checked into source control.
- The frontend never sees or handles provider tokens. Login flows redirect through the API; the API issues a session cookie.
- Anti-forgery protection enabled for state-changing requests originating from the SPA.

### 4.8 Security & Privacy

- Mitigate the OWASP Top 10:
  - Parameterized queries via EF Core (no string-concatenated SQL).
  - Authorization checks on every authenticated endpoint; tested explicitly.
  - Input validation on all request bodies (e.g., difficulty enum, board length 81, digit range 0–9, index 0–80).
  - HTTPS-only in non-development environments; HSTS enabled.
  - Cookies marked `HttpOnly`, `Secure`, `SameSite=Lax`.
  - Rate limiting on `POST /api/games` and `POST /api/games/{id}/hint` to prevent abuse.
  - Profile picture upload size cap and content-type/magic-byte validation.
  - Server-authoritative timer and validation prevent client-side time forgery.
- PII minimization: only username, email, and profile picture are stored. Email is not displayed publicly.
- Account deletion fully removes user records, completed games, login records, and profile picture blob.
- Logging never includes PII or session tokens.

### 4.9 Performance Targets

- Puzzle generation: p95 ≤ 1s, p99 ≤ 3s server time.
- API p95 latency for non-generation endpoints: ≤ 200ms under nominal load.
- Frontend initial load (LCP): ≤ 2.5s on a typical mobile connection.
- Time to interactive on the play page: ≤ 3s.

### 4.10 Deployment & Configuration

- Local dev: `aspire run` (or equivalent) brings up the API, Vite dev server, and a containerized PostgreSQL via the Aspire AppHost. Azure Blob is emulated locally with Azurite.
- Production: deployed via the Aspire AppHost publish flow to the chosen Azure target (specific target TBD; AppHost should be authored to keep that decision flexible). The `aspire` skill MUST be used for any deployment work.
- Configuration sources, in precedence order: environment variables → Aspire configuration → appsettings overrides → appsettings.json defaults.

---

## 5. Testing and Quality Assurance

### 5.1 Strategy

A layered approach with unit, integration, and end-to-end tests. The goal is high confidence in correctness — especially for puzzle generation, authentication, timing, and leaderboard ranking — without over-investing in brittle UI snapshot tests.

### 5.2 Coverage Goal

- **≥ 80% line coverage** across all solution components (`Sudoku.Api`, `Sudoku.Web`), measured by combining unit and integration test runs.
- Coverage is enforced in CI; pull requests dropping coverage below threshold fail the build.

### 5.3 Backend Tests (`tests/Sudoku.Api.Tests`)

- Framework: **xUnit** with FluentAssertions; HTTP integration via `WebApplicationFactory`.
- **Unit tests:**
  - Puzzle generator: every generated puzzle is solvable and has a unique solution (verify by running an independent solver). Difficulty clue counts fall within their declared bands. Generator handles many iterations without infinite loops.
  - Solver: known puzzles (with and without unique solutions) produce expected results.
  - Game store: thread-safety under concurrent access; eviction after timeout; proper handling of unknown game IDs.
  - Leaderboard ranking logic: ordering by elapsed time ascending, tie-breaking by `completed_at` ascending, exclusion of disqualified games, correct windowing (daily/weekly/all-time) at boundaries (UTC midnight, ISO week rollover).
  - Username validation, profile picture validation.
- **Integration tests:**
  - Full request/response cycle for every endpoint, including auth-required endpoints with a stubbed authentication scheme.
  - Database interactions against a real PostgreSQL container (via Testcontainers) — not an in-memory provider, to catch provider-specific issues.
  - Problem Details responses returned for all error paths.
  - Authorization: anonymous users cannot access `/api/me/*`, `/api/me/history`, etc.
  - Timer correctness: a completed game's elapsed time matches `completedAt - startedAt` within a small tolerance.

### 5.4 Frontend Tests (`tests/Sudoku.Web.Tests`)

- Framework: **Vitest** + Vue Test Utils; Testing Library for user-centric assertions.
- Cover:
  - Pinia stores: state transitions for selecting cells, entering values, toggling notes, undo/redo, conflict detection, timer display synchronization.
  - Components: `SudokuGrid` rendering of givens vs. user values vs. notes vs. conflicts vs. selection/peer highlight; keyboard interactions; ARIA attributes.
  - HTTP client wrapper: correct URL construction, cookie-credential handling, Problem Details parsing.
  - Theme store: respects `prefers-color-scheme` initially, persists user choice.

### 5.5 End-to-End Tests (`tests/Sudoku.E2E.Tests`)

- Framework: **Playwright** (TypeScript). The `playwright-cli` skill MUST be used to author and run these tests.
- Run against the Aspire-orchestrated stack so the full system (API + frontend + PostgreSQL + Azurite) is exercised.
- Critical user journeys:
  - Anonymous user starts an easy puzzle, plays to completion, sees their time.
  - Anonymous user uses a hint, completes the puzzle, sees the disqualified badge.
  - Anonymous user opens Leaderboards from the main menu and can view leaderboard data without signing in.
  - Authenticated user (using a stubbed/mock auth provider for tests) completes a medium puzzle and appears on the daily leaderboard.
  - Authenticated user views game history and personal stats.
  - User toggles notes mode, enters pencil marks, then enters a value that clears them.
  - User triggers conflict highlighting by entering a duplicate digit.
  - Theme toggle switches light/dark and persists across reloads.
  - Profile picture upload succeeds; oversized upload is rejected with a clear message.
  - Account deletion removes the user and their leaderboard entries.
- Mobile viewport (e.g., 390×844) covered for the play and leaderboard journeys.
- Accessibility smoke check: run an automated axe scan on the play page, leaderboard, and profile.

### 5.6 Specific Focus Areas

1. **Puzzle generation algorithm:** uniqueness verification under stress (≥ 1,000 generated puzzles per difficulty in the test suite, sampled in CI to keep runtimes manageable).
2. **Authentication flows:** correct user creation on first sign-in per provider; correct linking when the same provider subject signs in again; sign-out clears session; protected endpoints reject anonymous requests.
3. **Timing system:** server-authoritative timer is immune to client clock manipulation (tests submit completions with manipulated client timestamps and verify the server ignores them).
4. **Leaderboard correctness:** daily/weekly windows respect UTC boundaries; disqualified games never appear; `bestPerPlayer` filter behaves as documented.

### 5.7 Tooling, CI, and Quality Gates

- CI pipeline runs on every pull request:
  1. Backend build + unit + integration tests, with coverage report.
  2. Frontend build + unit tests, with coverage report.
  3. End-to-end tests against the Aspire-orchestrated stack.
  4. Lint: `dotnet format --verify-no-changes` for backend; ESLint + `vue-tsc --noEmit` for frontend.
  5. Combined coverage gate: fail if < 80%.
- Code review required on every PR; no direct pushes to `main`.

---

## 6. Open Questions / Future Work

The following are explicitly out of scope for v1 but may be revisited:

- Daily challenge with a shared seed for all users.
- Pause/resume within a session.
- PWA / offline play.
- Localization to additional languages.
- Pre-generated puzzle library or caching for instant start.
- Pencil-mark "auto-candidates" feature.
- Friend/follow system or private leaderboards.
- Email/password authentication or magic-link sign-in.
