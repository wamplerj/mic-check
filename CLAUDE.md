# CLAUDE.md

Guidance for Claude Code (claude.ai/code) when working in this repo.

## Project

MicCheck: open source. asp.net, c#, typescript, VueJS. Manages feature flags, projects, environments.

## Planned Structure

- `src/admin/` — VueJS admin components
- `src/api/` - .NET API + REST endpoints
- `tests/` — test suite
- `docs/` — documentation

## Best Practices

- Use latest LTS .NET + latest supported nuget packages for that version
- Set `langVersion` to latest in all csproj files; enable nullable
- Organize code by feature/area, not type (e.g. `features` namespace)
- New features need unit tests covering as much logic as possible (both nunit and jest)
- Any modified file: evaluate for missing test coverage and that all tests pass

# Coding
- Descriptive names for all classes/methods. No generic names: Provider, Manager, Helper
- Match formatting/style from `.editorconfig`
- Wrap lines at 220 characters, leave single line if fewer
- Place interfaces that are implemented by a single class at the bottom of the class file.  An interface with multiple implementations of an interface should be in a seperate file.
- Do not use tuples for return types.  Prefer records or classes for multiple values
- Do not use `sealed` on classes- 
- Use `record` for data objects, `class` for objects with behavior. Avoid mutable state where possible.

## Testing
- BDD-style unit tests, end-to-end as possible, no external resources (DB, filesystem). e.g. `WhenAUserDoesSomething_ThenAThingAppears`
- Mock external deps with Moq
- New features need unit tests covering as much logic as possible
- Any modified file: evaluate for missing test coverage- 
- No "Mock" in mocked object names
- No Arrange/Act/Assert comments
- All tests should pass before commit

## Claude
- Plans = `.md` files in `docs/plans/`. Admin → `docs/plans/admin/`, API → `docs/plans/api/`
- Split large plans into discrete chunks — each buildable + committable independently
- Plan generated from `docs/plans/<name>.md` → save as `docs/plans/<name>_plan.md`
- Plan implemented from `docs/plans/<name>.md` → save summary as `docs/plans/<name>_output.md`

## Stack
`.gitignore` configured for .NET/Visual Studio (C#, NuGet, MSBuild). Update if stack changes.
