# ADR 0001: Rename the app to "The Life"

- **Status:** accepted
- **Date:** 2026-09-27

## Context

The app's first name was very close to an existing product from a large company, and the UI borrowed that product's
look: its icon, its gradient and a similar script-style wordmark. That is harmless for a private learning project, but
it becomes a trademark problem as soon as the app is deployed publicly, put in an app store or shown widely.

## Decision

Rename everything now, while it is cheap, and give the app its own identity:

- The app is called **The Life**. The GitHub repo is `tuanhm11299/the-life-social-network`.
- Code: projects and namespaces `TheLife.*`, solution `backend/TheLife.slnx`.
- Runtime names: database, user and password `thelife`, container `thelife-postgres`, refresh cookie `thelife_refresh`,
  JWT issuer `thelife` and audience `the-life-web`.
- Visual identity:
  - a bold "The Life" wordmark in Outfit
  - a "TL" monogram (`AppMark.vue` and `public/favicon.svg`)
  - a teal→green brand gradient (`--color-brand-from/to` in `main.css`) for the story ring
  - Nuxt UI primary color `teal` (`app/app.config.ts`) to match
- Docs and code comments explain behavior in their own words and don't compare the app to other products.

## Consequences

- An existing local database volume was created with the old user name, so the API can't log in to it.
  Run `docker compose down -v` and then `docker compose up -d`: demo data re-seeds, and old uploads can be deleted.
- Anyone signed in before the rename has to log in again, because the refresh cookie and the JWT issuer changed.
- EF Core migrations only changed namespaces. Migration IDs are the same, so no new migration was needed.
- Local folders are renamed by hand to `the-life-social-network/`. GitHub redirects the old repo URL.
