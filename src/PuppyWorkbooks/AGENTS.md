# AGENTS.md

## Project overview

PuppyWorkbooks is a .NET 10 solution for defining and evaluating worksheet-style
Power Fx formulas. The repository includes the core workbook library, an XML-based
integration pipeline, desktop and CLI presentation projects, and xUnit tests.

## Repository layout

- `3-Services/PuppyWorkbooks/`: core workbook model, Power Fx interpreter, and XML serialization.
- `3-Services/PuppyWorkbooks.Integration/`: CSV/SQL integration pipeline and integration XML serializer.
- `5-Presentation/`: Avalonia, WinForms, WPF, WinUI, CLI, and view-model projects.
- `7-Tests/PuppyWorkbooks.Tests/`: xUnit tests and XML/CSV sample fixtures.

## Working conventions

- Target framework is `net10.0`; preserve nullable-reference annotations and implicit usings.
- Follow existing C# conventions: file-scoped namespaces, PascalCase public APIs, and async APIs ending in `Async`.
- Keep domain behavior in the service projects. Presentation projects should consume view models/services rather than duplicate interpreter or serialization logic.
- Keep integration XML parsing backward-compatible where aliases or legacy fields are already supported.
- When changing XML-backed models, update the relevant sample files and tests together.
- Do not commit IDE state from `.vs/` or `.idea/`, nor generated `bin/` and `obj/` output.

## Validation

Run the focused test project for core and integration changes:

```powershell
dotnet test 7-Tests/PuppyWorkbooks.Tests/PuppyWorkbooks.Tests.csproj
```

For a broader compile check, build the solution when the installed platform workloads support every UI project:

```powershell
dotnet build PuppyWorkbooks.slnx
```

Prefer focused tests for a changed behavior. Add or update an xUnit test in
`7-Tests/PuppyWorkbooks.Tests` for bug fixes and new core/integration behavior.
