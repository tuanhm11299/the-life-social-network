# The Life roadmap

A plan for making The Life better **and** growing as an engineer. Each phase adds something users would notice
and teaches one core engineering skill. Phases build on each other, so the order matters: CI and tests first make
every later change safer.

Tick items off as you go (`- [x]`). Each item says where in the code to start and what "done" means.

- [Phase 0: Fix what's already weak](#phase-0-fix-whats-already-weak)
- [Phase 1: Testing and quality on the frontend](#phase-1-testing-and-quality-on-the-frontend)
- [Phase 2: Product features that stretch the design](#phase-2-product-features-that-stretch-the-design)
- [Phase 3: Real-time and asynchronous work](#phase-3-real-time-and-asynchronous-work)
- [Phase 4: Scale and observability](#phase-4-scale-and-observability)
- [Phase 5: Ship it for real](#phase-5-ship-it-for-real)
- [Habits for every phase](#habits-for-every-phase)
- [Progress log](#progress-log)

---

## Phase 0: Fix what's already weak

**Skill:** privacy by design, performance budgets, automation.

- [ ] **Strip photo metadata (EXIF) on upload.**
  `LocalFileStorage.SaveImageAsync` currently saves the original bytes, so photos taken on phones can leak the
  owner's GPS location. Re-encode images with [ImageSharp](https://github.com/SixLabors/ImageSharp) before saving.
  *Start:* `backend/src/TheLife.Infrastructure/Storage/LocalFileStorage.cs`.
  *Done when:* an uploaded photo with GPS data comes back without any EXIF, proven by a test.
- [ ] **Resize images and create thumbnails** (e.g. 1080px for the feed, 320px for grids).
  Grids (`PostGrid.vue`) currently download full-size photos. Add a thumbnail URL to `PostDto`.
  *Done when:* the explore page transfers under 1 MB on first load (check in the browser's Network tab / Lighthouse).
- [ ] **Continuous integration with GitHub Actions**: on every pull request run `dotnet test`
  (Testcontainers works on Linux runners), `npm run typecheck` and `npm run build`.
  *Done when:* a PR cannot be merged while checks are red (branch protection on `main`).

## Phase 1: Testing and quality on the frontend

**Skill:** the testing pyramid, testing behavior (not implementation), accessibility.

- [ ] **Unit tests with Vitest** for the logic-heavy code:
  `composables/useInfiniteList.ts` (paging, duplicates, reload races),
  `composables/usePostActions.ts` (optimistic update + rollback on error),
  `components/posts/RichText.vue` (mentions and hashtags).
- [ ] **End-to-end tests with Playwright** at desktop (1280px) and phone (390px) sizes:
  register → create post → like → comment → view a story → log out.
  *Done when:* the e2e suite runs in CI against a real API + database.
- [ ] **Accessibility pass**: everything usable with the keyboard only, screen-reader labels on icon buttons,
  color contrast in light and dark mode (use the axe browser extension).

## Phase 2: Product features that stretch the design

**Skill:** authorization, search, data ownership, writing down decisions.

For each feature, write a short decision record in `docs/adr/` (context → options → decision → consequences).

- [ ] **Email verification and password reset.** Add an `IEmailSender` port in `Application/Common/Abstractions`,
  a development implementation that logs emails, and time-limited single-use tokens (hashed, like refresh tokens).
- [ ] **Private accounts and follow requests.** The hardest one on this list: every query must answer
  "may this viewer see this?" (feed, profile posts, stories, comments, likes, explore, search).
  Consider one reusable filter (e.g. `VisiblePosts(viewerId)`) instead of repeating the rule in every handler.
- [ ] **Block and report users.** Blocked users disappear from feeds, search, comments and notifications.
  Notice whether the slice-per-use-case structure keeps this manageable, and write down what you learn.
- [ ] **Hashtag pages and better search.** `SearchUsers.cs` uses `Contains`, which can't use an index.
  Try PostgreSQL full-text search (`tsvector`) and the `pg_trgm` extension; compare query plans before and after.
- [ ] **Delete my account.** Remove the user's data and files (database cascades already exist; files don't).

## Phase 3: Real-time and asynchronous work

**Skill:** WebSockets, eventual consistency, reliable messaging.

- [ ] **Real-time notifications with SignalR.** `layouts/default.vue` polls the unread count every 60 seconds;
  push new notifications instead.
- [ ] **Direct messages** (one-to-one chat) on top of SignalR: conversations, unread state, typing indicator.
- [ ] **Domain events + the outbox pattern.** Handlers such as `LikePost.cs`, `AddComment.cs` and `FollowUser.cs`
  create notifications inline. Raise events instead, store them in an outbox table in the same transaction,
  and process them in a background worker. Learn why "save to the database and then send a message" can lose data.
- [ ] **Object storage.** Replace local disk with S3-compatible storage (MinIO in `docker-compose.yml` locally)
  and pre-signed upload URLs, so large uploads go straight to storage instead of through the API.
  Only a new `IFileStorage` implementation should be needed. That's the point of the port.

## Phase 4: Scale and observability

**Skill:** measure first, then optimize; system-design trade-offs.

- [ ] **Observability first**: OpenTelemetry traces, metrics and structured logs
  (the .NET Aspire dashboard is an easy start). Include database query timings.
- [ ] **Load test with [k6](https://k6.io)**: e.g. 1,000 virtual users scrolling feeds, liking and posting.
  Record the baseline (p95 latency, errors, database CPU) in the progress log.
- [ ] **Optimize what the measurements show**, for example:
  - run `EXPLAIN ANALYZE` on the feed query (`GetFeed.cs`) and add or adjust indexes;
  - store like/comment counts on the post instead of counting on every read (`PostProjections`);
  - cache hot data (profiles, counts) in Redis.
- [ ] **Compare feed strategies**: today the feed is built when it's read (fan-out on read). Try precomputed
  timelines (fan-out on write) and document the trade-offs in an ADR (writes vs. reads, celebrities with many followers).

## Phase 5: Ship it for real

**Skill:** deployment, operations, native-feeling mobile web.

- [ ] **Container images**: Dockerfiles for the API and the web app, and a full-stack `docker compose` profile.
- [ ] **Deploy** (e.g. Azure Container Apps + managed PostgreSQL): HTTPS, secrets outside config files,
  database migrations run by the deployment pipeline, not by the app at startup.
- [ ] **Installable app (PWA)** with a web app manifest, offline fallback page and web push notifications.
- [ ] **Operations**: error tracking (e.g. Sentry), uptime checks, and a `docs/runbook.md` explaining what to do when
  the site is down or the database is full.

---

## Habits for every phase

- **One use case per pull request**, with a test and a description of *why*, not just *what*.
- **Read before you change**: trace a request end to end first ([architecture guide](architecture.md) walks through one).
- **Break things on purpose**: stop the database mid-request, send two likes at the same moment, upload a 50 MB
  file, and see what happens. Then fix it and add a test.
- **Measure before optimizing**: never guess what's slow.
- **Keep the progress log below**: one short paragraph per finished item on what surprised you.
  It turns into great interview stories.

## Progress log

| Date | Item | What I learned |
|------|------|----------------|
| 2026-09-23 | MVP shipped (PR #1) | Clean Architecture + vertical slices, JWT with refresh-token rotation, Testcontainers |
| 2026-09-27 | Renamed the app to The Life ([ADR 0001](adr/0001-rename-to-the-life.md)) | Check a product name against existing trademarks before building on it; renaming early is cheap, `git mv` keeps history |
