# Agent skills

Claude Code loads every `<name>/SKILL.md` in this folder as a project skill. It reads a skill's description all
the time and loads the full text only when a task matches it.

The skills are **copied in, not installed** as plugins. That way they are pinned to a known version, anyone can
read exactly what the agent follows, and they work the same on macOS and Windows with no install step. Every folder
is an unchanged copy of the upstream folder, plus that repo's `LICENSE` (all MIT).

| Skill | Helps with | Source (repo @ commit, path) |
|---|---|---|
| `minimal-api-file-upload` | Upload endpoints (roadmap Phase 0: EXIF, thumbnails) | [dotnet/skills][dotnet] @ `a55fbf4`, `plugins/dotnet-aspnetcore/skills/minimal-api-file-upload` |
| `dotnet-webapi` | ASP.NET Core Web API patterns | [dotnet/skills][dotnet] @ `a55fbf4`, `plugins/dotnet-aspnetcore/skills/dotnet-webapi` |
| `optimizing-ef-core-queries` | Slow or chatty EF Core queries | [dotnet/skills][dotnet] @ `a55fbf4`, `plugins/dotnet-data/skills/optimizing-ef-core-queries` |
| `run-tests` | The right `dotnet test` command and filters | [dotnet/skills][dotnet] @ `a55fbf4`, `plugins/dotnet-test/skills/run-tests` |
| `test-anti-patterns` | Auditing test quality | [dotnet/skills][dotnet] @ `a55fbf4`, `plugins/dotnet-test/skills/test-anti-patterns` |
| `csharp-refactoring` | Behavior-preserving C# refactors | [dotnet/skills][dotnet] @ `a55fbf4`, `plugins/dotnet/skills/csharp-refactoring` |
| `nuxt-ui` | Nuxt UI 4 components, forms, theming | [nuxt/ui][nuxtui] @ `e8756fd` (branch `v4`), `skills/nuxt-ui` |
| `vue` | Vue 3 core, Composition API | [antfu/skills][antfu] @ `e98e476`, `skills/vue` |
| `nuxt` | Nuxt 4 routing, data fetching, config | [antfu/skills][antfu] @ `e98e476`, `skills/nuxt` |
| `nitro` | Nuxt server routes (our `/api` proxy lives there) | [antfu/skills][antfu] @ `e98e476`, `skills/nitro` |

Full commits: dotnet/skills `a55fbf42c36a37b94ec07291bd79cb6b6bf3a04d`, nuxt/ui
`e8756fd3a8aaab3cb434f1dfe8d32559330f38a9`, antfu/skills `e98e476e315f068f72d53bd3afb34fdd4d5851c3` (copied 2026-09-27).

[dotnet]: https://github.com/dotnet/skills
[nuxtui]: https://github.com/nuxt/ui/tree/v4/skills/nuxt-ui
[antfu]: https://github.com/antfu/skills

## Good to know

- `CLAUDE.md` and `docs/architecture.md` win over a skill when they disagree. For example, a skill may suggest a
  Nitro `routeRules` proxy, which this project deliberately avoids.
- The dotnet test skills sometimes point to sibling skills we didn't copy (e.g. `test-gap-analysis`,
  `writing-mstest-tests`); that is expected. Our tests use xUnit, not MSTest.
- `onmax/nuxt-skills` was left out because it has no license, so it can't be copied into this repo.

## Updating or adding a skill

1. Read the new upstream `SKILL.md` and its files first: a skill is instructions the agent will follow.
2. Replace the whole folder with the upstream copy at the new commit (keep the `LICENSE` file).
3. Update the commit in the table above.
