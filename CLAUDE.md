# CLAUDE.md: context for AI-assisted sessions on The Life

The Life is a small photo-sharing social network built as a learning project: .NET 10 API + PostgreSQL, Nuxt 4 + Nuxt UI web app
(desktop and phone browsers). **Top priority: code a human can read, fix and extend without AI help.**
Match the existing style: small files, plain names, comments that explain *why*, no clever abstractions.

- Repo: https://github.com/tuanhm11299/the-life-social-network (default branch `main`)
- Local paths: `E:\MyPersonalProject\the-life-social-network` (Windows 10; use Git Bash or PowerShell) and
  `/Users/tuan.huynh/MyProjects/the-life-social-network` (macOS, Apple silicon; zsh). Docker Desktop required on both.
- Read first: `README.md` (run it), `docs/architecture.md` (how it's built + add-a-feature recipe), `docs/roadmap.md` (what's next)

## Run, test, check

```bash
docker compose up -d                                   # PostgreSQL (container thelife-postgres, port 5432)
cd backend && dotnet run --project src/TheLife.Api   # API on http://localhost:5080, docs at /scalar
cd frontend && npm run dev                             # web on http://localhost:3000 (proxies /api and /uploads)
cd backend && dotnet test                              # 50 tests; API tests use Testcontainers (Docker must run)
cd frontend && npm run typecheck                       # must stay clean
```

Demo login (seeded in Development): `demo` / `Password123!`.
New migration: `dotnet ef migrations add <Name> --project src/TheLife.Infrastructure --startup-project src/TheLife.Api --output-dir Persistence/Migrations` (run `dotnet tool restore` once).

## Architecture in one paragraph

Clean Architecture layers (`Domain` ← `Application` ← `Infrastructure`, `Api` on top) + vertical slices with CQRS:
one file per use case in `backend/src/TheLife.Application/Features/<Area>/<UseCase>.cs` holding the command/query
record, an optional FluentValidation validator and the handler. Handlers return `Result<T>` with `<Area>Errors`
(never throw for expected failures); they are auto-registered and wrapped in a validation decorator (no MediatR).
Endpoints inject `ICommandHandler<TCommand, TResponse>` directly (`Api/Endpoints/*Endpoints.cs`). Queries use
`AsNoTracking` + expression projections (`PostProjections`, `UserProjections`). Frontend: typed API modules in
`frontend/app/api/`, pages in `app/pages/`, components auto-imported by file name (`pathPrefix: false`).

## Gotchas already learned (don't rediscover them)

- **Proxy**: `/api/**` and `/uploads/**` are forwarded by `frontend/server/routes/*` → `server/utils/proxyToApi.ts`,
  which overwrites `X-Forwarded-For` with the socket IP. Do **not** switch back to Nitro `routeRules` proxy: it runs
  before server middleware, so the client IP couldn't be set and spoofed headers bypassed the API's login rate limit.
- **Nuxt Icon** is moved to `/_nuxt_icon` (`nuxt.config.ts`); its default `/api/_nuxt_icon` would hit the API proxy.
- **Refresh tokens** rotate on every refresh but stay valid for 30 s (`RefreshToken.RotationGracePeriod`); without
  that, several tabs opening at once logged each other out. Logout and password change revoke immediately.
- **Dev server quirks**: the first page load after a start or config change can be blank for a few seconds while
  Vite pre-bundles; wait and reload. After adding server routes or changing `nuxt.config.ts`, restart `nuxt dev`
  cleanly (a hot restart once left `runtimeConfig.apiUrl` undefined). A stale `nuxt dev` can keep port 3000.
- A running API locks DLLs, so stop it before `dotnet test`, `dotnet build` or `dotnet ef`.
- EF Core Relational is pinned to 10.0.12 in Infrastructure to avoid a version conflict with Npgsql's dependency.
- The app was renamed (ADR 0001). A database volume created before that uses an old user name and can't be used with `thelife`:
  run `docker compose down -v` and start again (demo data re-seeds).
- **ImageSharp is pinned to 3.1.x** (ADR 0002): version 4 fails Release builds without a Six Labors license key.
  Every upload goes through `Infrastructure/Images/ImageProcessor.cs` (metadata stripped, turned upright).
- **macOS differences** (nothing here is committed except this note; Windows is unaffected): the .NET SDK sits in
  `~/.dotnet`, exported as `DOTNET_ROOT` and added to `PATH` in `~/.zshrc`. Host port 5432 belongs to another
  project's container, so an untracked `docker-compose.override.yml` publishes PostgreSQL on **5434**
  (`ports: !override` - plain merging *adds* a port instead of replacing it). The API therefore needs its
  connection string passed in; it is set in the VS Code launch config and tasks, and in the terminal:
  `ConnectionStrings__Database='Host=localhost;Port=5434;Database=thelife;Username=thelife;Password=thelife' dotnet run --project src/TheLife.Api`
  (same for `dotnet ef`). In Rider, add that variable once to the `http` run configuration. Forget it and the API
  talks to 5432 - the other project's database - and fails with `28P01 password authentication failed`.

## Working agreements

- Work on a branch → push → open a PR → merge into `main` with a merge commit (the owner has asked for this flow).
  End commit messages with the Co-Authored-By line required by the session.
- Every backend change keeps `dotnet test` green; every frontend change keeps `npm run typecheck` green.
- New features follow the recipe in `docs/architecture.md`; record notable design decisions as ADRs in `docs/adr/`.
- Tick roadmap checkboxes and add a line to the progress log in `docs/roadmap.md` when an item is done.
- Agent skills (.NET, EF Core, tests, Vue, Nuxt, Nuxt UI) are copied into `.claude/skills/`, pinned to upstream
  commits: see `.claude/skills/README.md` for sources and how to update them. This file wins over a skill.

## Status (2026-09-23)

- MVP complete and merged (PR #1): auth, profiles, posts with carousel, feed, likes, comments, saves, stories,
  explore, search, notifications, settings; responsive UI verified in Chrome at desktop and phone widths.
- Roadmap merged (PR #2). **Next up: roadmap Phase 0**: strip EXIF on upload, thumbnails, GitHub Actions CI.
