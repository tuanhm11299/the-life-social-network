# ADR 0001: Rename InstaLite to "The Life"

- **Status:** accepted
- **Date:** 2026-09-27

## Context

The app started as "InstaLite". That name is very close to Meta's own "Instagram Lite", and Meta's brand guidelines
ask third parties not to use "Insta" or "Gram" in product names. The UI also borrowed Instagram's look: its camera
glyph (favicon and collapsed sidebar), its yellow→pink→purple gradient (favicon and story ring) and a script font
similar to its wordmark. That is harmless for a private learning project, but it would be a problem as soon as the app
is deployed publicly, put in an app store or shown widely.

## Decision

Rename everything now, while it is cheap:

- The app is called **The Life**. The GitHub repo is `tuanhm11299/the-life-social-network`.
- Code: projects and namespaces `InstaLite.*` → `TheLife.*`, solution `backend/TheLife.slnx`.
- Runtime names: database, user and password `thelife`, container `thelife-postgres`, refresh cookie `thelife_refresh`,
  JWT issuer `thelife` and audience `the-life-web`.
- Our own identity instead of Instagram's: a bold "The Life" wordmark in Outfit, a "TL" monogram
  (`AppMark.vue` and `public/favicon.svg`), and a teal→green brand gradient (`--color-brand-from/to` in `main.css`)
  for the story ring. The Nuxt UI primary color moves from Instagram-like pink to `teal` (`app/app.config.ts`)
  to match.

Code comments that compare behavior to Instagram (for example "double-tap only likes, like on Instagram") stay,
because they help readers. The README says the project is not affiliated with Instagram or Meta.

## Consequences

- An existing local database volume was created with the old `instalite` user, so the API can't log in to it.
  Run `docker compose down -v` and then `docker compose up -d`: demo data re-seeds, and old uploads can be deleted.
- Anyone signed in before the rename has to log in again, because the refresh cookie and the JWT issuer changed.
- EF Core migrations only changed namespaces. Migration IDs are the same, so no new migration was needed.
- Local folders (`instalite/`) are renamed by hand to `the-life-social-network/`. GitHub redirects the old repo URL.
