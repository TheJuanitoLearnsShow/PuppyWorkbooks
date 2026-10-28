# AGENTS.md

## Purpose and current shape

PuppyWorkbooks is a .NET 10 solution for evaluating worksheet-style Power Fx formulas and executing XML-defined, record-oriented integrations. The checked-in solution contains:

- `3-Services/PuppyWorkbooks/` — worksheet model, Power Fx interpreter, custom functions, worksheet XML/JSON serialization, and `WorkSheet.xsd`.
- `3-Services/PuppyWorkbooks.Integration/` — integration model, XML serializer, runner, input/output providers, secret resolution, and integration schemas.
- `5-Presentation/CLI/PuppyWorkbooks.CLI/` — the `puppyworkbooks` global-tool entry point and command-line execution settings.
- `5-Presentation/PuppyWorkbooks.App.WinForms/` — Windows Forms worksheet editor using WebView2; its embedded editor is TypeScript under `wwwroot/worksheet-editor/`.
- `5-Presentation/MinimalInterpreterExamples/` — small interpreter examples.
- `7-Tests/PuppyWorkbooks.Tests/` — xUnit coverage and all XML/CSV/JSON fixtures.

`PuppyWorkbooks.slnx` is the source of truth for projects in the solution. There are folders reserved for other UI technologies, but they do not currently contain projects. Do not claim or build support for UI projects that are not in the solution.

## Start here

1. Inspect the model, serializer, runner, and closest existing test before changing behavior. The integration serializer intentionally supports legacy XML forms.
2. Keep behavior in the service projects. The CLI and WinForms app should call those services rather than reimplement formula evaluation, XML parsing, or pipeline logic.
3. Make the smallest coherent change. For a changed serialized contract, update the model, serializer, XSD, representative sample fixture, and tests together.
4. Preserve unrelated working-tree changes. Never add `.vs/`, `.idea/`, `bin/`, or `obj/` output, or generated editor folders such as `node_modules/` and `.parcel-cache/`.

The projects use `net10.0` (the WinForms app uses `net10.0-windows`), nullable reference types, and implicit usings. Use file-scoped namespaces, PascalCase public APIs, and an `Async` suffix on asynchronous APIs. Prefer collection expressions and other syntax already used by the surrounding code; do not do broad style rewrites.

## Code ownership and change map

- Formula evaluation or worksheet serialization: work in `3-Services/PuppyWorkbooks/`; extend `WorkbookInterpreter` and `FxExtentions/` for built-in/custom Power Fx behavior.
- Pipeline execution, record merging, debug data, mocks, or provider behavior: work in `3-Services/PuppyWorkbooks.Integration/Engine/` and `Providers/`.
- XML element/attribute compatibility: update `Models/`, `IntegrationXmlSerializer`, and `Schemas/Integration.xsd`. The serializer performs normalization beyond `XmlSerializer` attributes, so model changes alone are often insufficient.
- Command-line options and output: work in `5-Presentation/CLI/PuppyWorkbooks.CLI/`, keeping execution semantics in the services.
- WinForms/editor work: update the relevant C# view/view-model and, for editor behavior, its TypeScript source. The generated JavaScript under `wwwroot/worksheet-editor/js/generated/` is tracked and must be rebuilt when its TypeScript source changes. The source is built with Node/Parcel; `node_modules/` and `.parcel-cache/` are generated artifacts and not hand-edited.
- Tests and fixtures: add focused xUnit tests under `7-Tests/PuppyWorkbooks.Tests/`. Integration fixtures live in `SampleFiles/Integration/` and are copied to the test output directory.

## Worksheet and Power Fx contract

Standalone worksheet XML uses a `<WorkSheet>` root with `Name`, optional `Variables`, and ordered `Cells` of `WorkCell` items. A variable contains `Key` and `Value`; a cell contains `Id`, `Name`, `Formula`, and `Comments`.

- Formulas have no leading `=`.
- XML-escape formula content (`&amp;`, `&quot;`, `&lt;`, and `&gt;`).
- Cell order matters. Formula cells can reference earlier cells by name.
- In integration worksheets, use `InputRecord.FieldName` for the current row and `State` in reduce steps. Direct input-field variables remain supported for compatibility.
- Custom functions currently include `AddTax`, `FileLines`, and `AsyncSample`. Add tests for both valid values and error/blank behavior when extending them.

## Integration XML contract

The canonical integration root is `<Integration>` with optional `HttpConfigurations`, optional `SecretManager`, and `Steps`. Prefer the concrete provider element names that serialization emits:

- Inputs: `MemoryInput`, `CsvInput`, `SqlInput`, `HttpInput`, `JsonInput`, `XmlInput`, and `FileSystemInput`.
- Transform steps: `Map`, `Filter`, `Reduce`, `Switch`, and `RunIntegration`.
- Outputs: `CsvOutput`, `SqlOutput`, `HttpOutput`, `JsonOutput`, `XmlOutput`, and `MemoryOutput`.

`IOInput Kind="..."` and `IOOutput Kind="..."` are accepted legacy aliases. Keep that normalization backward-compatible unless a breaking change is explicitly requested. Nested `Switch` branch steps use the same rules. `RunIntegration` runs a child integration for every incoming row and supplies it to the child's first `MemoryInput`; `FromField` narrows the supplied value.

Worksheets may be inline or referenced with `<Worksheet FilePath="..." />`. Referenced worksheet and mock-data paths are resolved relative to the integration file. `Path`, `File`, `Filename`, and `FileName` aliases are intentionally accepted. Maintain them when changing reference parsing.

Pipeline semantics worth preserving:

- The first input establishes rows. A later input runs per incoming row, merges its rows into that row, and values from the later input win on name collisions.
- A `Map` adds every named formula-cell result to the record; `OutputField` remains only for compatibility.
- A `Filter` evaluates its final non-empty formula cell. `KeepWhenTrue` controls whether a true result is retained.
- A `Reduce` receives `State` and `InputRecord`; its final cell supplies the next state and `OutputField` names the state field.
- An output appends `<step-id>.Status`, `.StatusMessage`, and `.AffectedRows` to the record passed to following steps.

Use `<MockDataSources>` (or the supported legacy mock forms) on input steps for safe tests. Run with `UseMockDataForSteps = "ALL"` or a comma-separated input/output step list; `ALL` also suppresses real outputs. A named scenario selects a matching mock source and falls back to the first one if no match exists.

Keep credentials out of XML fixtures. The integration supports environment, Windows Credential Manager, and Azure Key Vault secret definitions; use placeholder values and mocks in test data.

When changing integration behavior or XML compatibility, keep `3-Services/PuppyWorkbooks.Integration/README.md` aligned with the serializer and schema: it documents the canonical runtime semantics and is the easiest place for agentic changes to drift.

## Validation

Run the focused test project for service, XML, CLI, or fixture changes:

```powershell
dotnet test 7-Tests/PuppyWorkbooks.Tests/PuppyWorkbooks.Tests.csproj
```

For a narrow iteration, filter by the closest test class, for example:

```powershell
dotnet test 7-Tests/PuppyWorkbooks.Tests/PuppyWorkbooks.Tests.csproj --filter FullyQualifiedName~IntegrationTests
```

The schema tests validate representative integration and worksheet samples. Add a schema test and a runtime/round-trip test when introducing XML syntax. Build the solution only when the installed Windows and WebView2 prerequisites are available:

```powershell
dotnet build PuppyWorkbooks.slnx
```

To manually exercise an installed CLI tool without real dependencies, pass an integration fixture and mock flags:

```powershell
puppyworkbooks path/to/integration.xml --use-mock-data-for-steps ALL --debug
puppyworkbooks path/to/integration.xml --use-mock-data-for-steps inputId --scenario ScenarioA
```

If TypeScript editor sources changed, run `npm run build` from `5-Presentation/PuppyWorkbooks.App.WinForms/wwwroot/worksheet-editor/` and include the regenerated tracked JavaScript. Do not treat generated `bin/` or `obj/` files as source changes.