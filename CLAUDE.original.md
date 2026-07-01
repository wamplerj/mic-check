# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

MicCheck is an open source project written in asp.net, c#, typescript and VueJS that manages feature flags, their projects, and their environments. 

## Planned Structure

- `src/admin/` — administrative components in VueJS
- `src/api/` - api and restful endpoints in .NET
- `tests/` — test suite
- `docs/` — documentation

## Best Practices

- Ensure all projects are using the latest LTS version of .NET as well as the latest supported nuget packages for that .NET version
- Ensure that langVersion is set to latest in all csproj files and nullable is enabled
- Code in all projects should be organized by feature or area instead of type.  (e.g. a features namespace with all feature related code in it or in child namespaces of it)
- All new feature requests should include corresponding unit tests that cover as much of the logic as possible.
- Any file modified should be evaluated for potential test cases and missing coverage areas.

# Coding
- Use descriptive names for all classes and method created.  Avoid generic names like Provider, Manager, Helper
- Coding should match formating and style rules in the .editorconfig file

## Testing
- Write unit tests in a BDD style, testing as much code end-to-end as possible without without touching external resources like databases or file systems (e.g. WhenAUserDoesSomething_ThenAThingAppears)
- Mock any external dependencies using Moq.
- Do not name any mocked objects with the word Mock in them
- Do not include any Arrange / Act / Assert comments in the code


## Claude
- Create all plans as .md file located in the `docs/` folder.  Admin in `docs/admin/` and API in `docs/api/`
- Divide up large plans into discrete chucks of functionality so each can be built and committed independently.
- When generating a plan from a .md file in /docs/ save the plan in the same folder with the same filename minus the .md extention, but with a _plan.md at the end
- When implementing a plan from a .md file in /docs/ save the summary of the plan in the same folder with the same filename minus the .md extention, but with a _output.md at the end


## Stack

The `.gitignore` is configured for a .NET/Visual Studio project (C#, NuGet, MSBuild). If this changes, update this file accordingly.
