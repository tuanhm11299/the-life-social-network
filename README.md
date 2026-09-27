# The Life

A small, readable Instagram-style app: share photos, follow people, like, comment, save posts and post 24-hour stories.
It works on desktop and phone browsers.
Not affiliated with Instagram or Meta.

| Part     | Tech |
|----------|------|
| Backend  | .NET 10 · ASP.NET Core minimal APIs · Entity Framework Core · PostgreSQL · FluentValidation · JWT |
| Frontend | Nuxt 4 (Vue 3) · Nuxt UI 4 · Tailwind CSS 4 · Pinia · Zod |
| Tests    | xUnit · Testcontainers (real PostgreSQL in Docker) |

The backend combines **Clean Architecture** (layers) with **Vertical Slices + CQRS** (one file per use case).
Read [docs/architecture.md](docs/architecture.md) for the full tour and step-by-step recipes for adding features,
and [docs/roadmap.md](docs/roadmap.md) for what to build next.

## Features

- **Accounts**: sign up, log in, stay logged in (refresh-token cookie), log out, change password
- **Profiles**: photo, name, bio, post grid, followers / following lists
- **Posts**: 1–10 photos per post (swipeable carousel), captions with `@mentions` and `#hashtags`, edit caption, delete
- **Feed**: posts from people you follow, with infinite scrolling
- **Likes**: heart button or **double-tap the photo**, see who liked a post
- **Comments**: add, read and delete (your own comments, or any comment on your post)
- **Stories**: photos that disappear after 24 hours, story bar with colored/grey rings, full-screen viewer with progress bars, tap/hold controls, and "Seen by" for your own stories
- **Explore**: popular posts from people you don't follow yet
- **Search**: find people by username or name; "Suggested for you"
- **Saved**: bookmark posts into a private collection
- **Notifications**: likes, comments and new followers, with an unread badge and "Follow back"
- **Responsive UI**: bottom tab bar on phones, icon sidebar on tablets, full sidebar on desktops; light and dark mode

## Quick start

You need: [.NET SDK 10](https://dotnet.microsoft.com/download), [Node.js 22+](https://nodejs.org) and [Docker](https://www.docker.com/).

```bash
# 1. Start PostgreSQL (in Docker)
docker compose up -d

# 2. Start the API  →  http://localhost:5080  (API docs at http://localhost:5080/scalar)
cd backend
dotnet run --project src/TheLife.Api

# 3. In another terminal, start the web app  →  http://localhost:3000
cd frontend
npm install
npm run dev
```

In development the API creates the database tables automatically and adds **demo data**.
Log in as **`demo` / `Password123!`** (the login page has a button that fills this in).
All other demo accounts (`alice.travels`, `bob.cooks`, …) use the same password.

To start again from an empty database: `docker compose down -v`, then `docker compose up -d`.

## Project structure

```
the-life-social-network/
├── docker-compose.yml          PostgreSQL for local development
├── docs/architecture.md        How the code is organised + how to add features
├── docs/roadmap.md             What to build next, phase by phase
├── backend/
│   ├── src/
│   │   ├── TheLife.Domain/          Entities and business rules (no dependencies)
│   │   ├── TheLife.Application/     Use cases: Features/<Area>/<UseCase>.cs  (commands & queries)
│   │   ├── TheLife.Infrastructure/  Database (EF Core), JWT, password hashing, file storage
│   │   └── TheLife.Api/             HTTP endpoints, error handling, Program.cs
│   └── tests/TheLife.Tests/         Domain unit tests + API tests against real PostgreSQL
└── frontend/
    ├── app/
    │   ├── api/            One file per backend area: typed functions that call the API
    │   ├── pages/          One file per screen (file name = URL)
    │   ├── components/     UI pieces, grouped by area (posts, stories, users, layout, common)
    │   ├── composables/    Reusable logic (infinite lists, post actions, dialogs...)
    │   ├── stores/         Global state (who is logged in, unread notifications)
    │   ├── layouts/        Page frames (signed-in app vs. login screens)
    │   ├── middleware/     Redirects signed-out visitors to /login
    │   ├── types/api.ts    TypeScript shapes of the API's JSON
    │   └── utils/          Small helpers (formatting, error messages, image checks)
    └── server/             Runs on the Nuxt server: forwards /api and /uploads to the .NET API
```

## Running the tests

Docker must be running (the API tests start their own temporary PostgreSQL container).

```bash
cd backend
dotnet test
```

For the frontend, type-check the whole app with:

```bash
cd frontend
npm run typecheck
```

## Configuration

Backend settings live in `backend/src/TheLife.Api/appsettings.json` (overridden by `appsettings.Development.json` in development).
Any setting can also come from an environment variable, e.g. `Jwt__SigningKey`, `ConnectionStrings__Database`.

| Setting | What it does |
|---------|--------------|
| `ConnectionStrings:Database` | PostgreSQL connection string |
| `Jwt:SigningKey` | Secret used to sign access tokens, **at least 32 characters. Set your own in production.** |
| `Jwt:AccessTokenMinutes` / `Jwt:RefreshTokenDays` | Token lifetimes |
| `Database:ApplyMigrationsOnStartup` | Create/update tables when the API starts |
| `Database:SeedDemoData` | Add demo users and posts to an empty database |
| `Storage:RootPath` | Folder where uploaded images are saved |
| `RateLimiting:AuthRequestsPerMinute` | Login/sign-up attempts allowed per IP per minute |

Frontend: `NUXT_API_URL` (default `http://localhost:5080`) tells the Nuxt server where the API is.
See `frontend/.env.example`.

## Production notes

- Build the web app with `npm run build` and run it with `NUXT_API_URL=http://your-api:5080 node .output/server/index.mjs`.
  It serves the app and forwards `/api` and `/uploads` to the API (`frontend/server/routes/`), so the browser sees one website.
- The API trusts `X-Forwarded-For` only from the same machine (ASP.NET Core default). If Nuxt and the API run on
  different machines, add the Nuxt server to `ForwardedHeadersOptions.KnownProxies` in `ApiServices.cs`, otherwise
  all visitors share one login rate limit.
- Set a strong `Jwt:SigningKey` and serve everything over HTTPS (the refresh cookie is then marked `Secure`).
- Uploaded images are stored on the API server's disk. For several servers, implement `IFileStorage`
  for a cloud bucket (see `LocalFileStorage.cs`).
- Apply database migrations as part of your deployment (`dotnet ef database update`) or keep
  `Database:ApplyMigrationsOnStartup` on.
