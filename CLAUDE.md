# CLAUDE.md

Guidance for Claude Code (claude.ai/code) in this repo.

## Project

MicCheck: open source. asp.net, c#, typescript, VueJS. Manage feature flags, projects, environments.

## Planned Structure

- `src/admin/` — VueJS admin components
- `src/api/` - .NET API + REST endpoints
- `tests/` — test suite
- `docs/` — documentation

## Best Practices

- Use latest LTS .NET + latest supported nuget packages for that version
- Set `langVersion` to latest in all csproj files; enable nullable
- Organize code by feature/area, not type (e.g. `features` namespace)
- New features need unit tests covering logic as much as possible (both nunit and jest)
- Modified file: check missing test coverage, all tests pass

# Coding
- Descriptive names all classes/methods. No generic: Provider, Manager, Helper
- Match formatting/style from `.editorconfig`
- Wrap lines at 220 chars, single line if fewer
- Interfaces implemented by single class → bottom of class file. Interface w/ multiple implementations → separate file.
- No tuples for return types. Prefer records or classes for multiple values
- No `sealed`
- Use `record` for data objects, `class` for objects with behavior. Avoid mutable state where possible.

## Testing
- Min 70% code coverage, target 90%. Unit tests focus end-user scenarios first.
- Don't write tests just for coverage. Call out missing coverage rather than cover stuff not valuable to end user.
- Code not cleanly unit-testable → mark `[ExcludeFromCodeCoverage]` or exclude namespace from coverage in .runsettings file
- BDD-style unit tests, end-to-end as possible, no external resources (DB, filesystem). e.g. `WhenAUserDoesSomething_ThenAThingAppears`
- Mock external deps w/ Moq
- Mock EntityFramework DBContexts via extracted interface + Moq. Don't rely on InMemory provider.
- New features need unit tests covering logic as much as possible
- Modified file: check missing test coverage
- No "Mock" in mocked object names
- No Arrange/Act/Assert comments
- All tests pass before commit

## Claude
- Plans = `.md` files in `docs/plans/`. Admin → `docs/plans/admin/`, API → `docs/plans/api/`
- Split large plans into discrete chunks — each buildable + committable independently
- Plan generated from `docs/plans/<name>.md` → save as `docs/plans/<name>_plan.md`
- Plan implemented from `docs/plans/<name>.md` → save summary as `docs/plans/<name>_output.md`

## Stack
`.gitignore` set for .NET/Visual Studio (C#, NuGet, MSBuild). Update if stack change.
