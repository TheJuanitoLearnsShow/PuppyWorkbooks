---
name: puppyworkbooks-integration-xml
description: Create or edit PuppyWorkbooks integration XML definitions containing CSV or SQL I/O and worksheet-driven map, filter, reduce, or switch steps.
---

# PuppyWorkbooks integration XML

Use this skill when a request is to create, change, review, or validate an XML
integration for PuppyWorkbooks. Do not use it for a standalone `WorkSheet` XML
file unless that worksheet is being prepared for an integration step.

## Start from the repository examples

Read the sample closest to the requested behavior before editing:

- `7-Tests/PuppyWorkbooks.Tests/SampleFiles/Integration/TestIntegrationWithFileReferences.xml`
  for an input, map, filter, reduce, output, and referenced worksheets.
- `7-Tests/PuppyWorkbooks.Tests/SampleFiles/Integration/Map.xml`, `Filter.xml`, and
  `Reduce.xml` for standalone worksheet files.
- `7-Tests/PuppyWorkbooks.Tests/SampleFiles/Integration/Switch.xml` for inline
  switch worksheets and branches.

Keep the root as `Integration`, give the integration and every step a meaningful
`Name` or `Id`, and place every top-level step inside its single `Steps` element.
The runner uses the first `IOInput` as the input source and executes top-level
steps in declaration order.

## Step semantics and XML shape

- `IOInput` yields one record at a time. Use `Kind="CSVReader" FilePath="..."`
  for CSV. For `Kind="SqlReader"`, use `ConnectionString="..."` and a `Query`
  child; `FilePath` is not needed by the implementation for SQL despite the
  original XSD declaring it required.
- `Map` evaluates its worksheet against the current record. Every non-empty
  formula cell becomes a new record field using that cell's `Name`; it replaces
  the previous record. `OutputField` is accepted for compatibility but does not
  limit map output.
- `Filter` evaluates its worksheet and reads the final non-empty formula cell.
  It keeps a record when the value is true. `KeepWhenTrue="false"` reverses the
  decision.
- `Reduce` requires `InitialStateJson`, `OutputField`, and a worksheet. The
  worksheet receives `State` and the record; the named output cell, or the final
  formula cell if there is no matching name, becomes the next state. The record
  after the step contains only the `OutputField` state value. When any reduce
  exists, top-level outputs run once after all input records, using the final
  record.
- `Switch` evaluates its worksheet once for each record. Each `Branch` needs a
  `WorkCell` equal to a boolean worksheet-cell `Name`. All branches whose cells
  are true run, in XML order. Branches may contain `Map`, `Filter`, `Reduce`, or
  nested `Switch` steps; they cannot contain `IOInput` or `IOOutput`.
- `IOOutput` writes the current record and passes it forward with
  `<step-id>.Status`, `<step-id>.StatusMessage`, and `<step-id>.AffectedRows`.
  Use `Kind="CSVWriter" FilePath="..."` for CSV. For `Kind="SqlWriter"`, use
  `ConnectionString` plus `TableName`, or a `Query` child for a custom command.

Use an inline worksheet when it is specific to one integration; otherwise use a
separate `WorkSheet` XML file:

```xml
<Map Id="normalize">
  <Worksheet FilePath="worksheets/normalize.xml" />
</Map>
```

Referenced paths are relative to the integration XML. `Path`, `File`,
`Filename`, and `FileName` are supported aliases for `FilePath`, but write new
definitions with `FilePath`.

Every inline worksheet must contain `Cells` and at least one `WorkCell`. A
`WorkCell` has `Id`, `Name`, and `Formula`, in that order. XML-escape formulas,
for example write `&amp;` for Power Fx string concatenation and `&quot;` for a string
literal. Worksheets receive the current record as `InputRecord`; direct field
names are also provided for compatibility. Reduce worksheets additionally
receive `State`.

## Safe validation

First validate a referenced worksheet by running the CLI with its worksheet XML
path. Locate an existing `PuppyWorkbooks.CLI.exe`; if no built executable is
available, use the project command below from the repository root:

```powershell
dotnet run --project 5-Presentation/CLI/PuppyWorkbooks.CLI/PuppyWorkbooks.CLI.csproj -- path/to/worksheet.xml
```

To execute an integration without reading its configured CSV or SQL input,
declare mock CSV data on each input that will be selected and pass `ALL`:

```xml
<IOInput Id="source" Kind="SqlReader" ConnectionString="not-used-when-mocked">
  <MockCsv>
Name,Active,Amount
Alice,true,10
  </MockCsv>
</IOInput>
```

```powershell
dotnet run --project 5-Presentation/CLI/PuppyWorkbooks.CLI/PuppyWorkbooks.CLI.csproj -- path/to/integration.xml --use-mock-data-for-steps ALL --debug
```

`ALL` replaces input reads only. CSV outputs still write their configured file,
and SQL outputs still require an application-supplied database connection
factory, which the CLI does not provide. Before execution, direct outputs to a
disposable test file and do not validate a definition that has a real SQL output
through the CLI. `--debug` prints per-record input and worksheet-cell results
for inspection.

When a core integration behavior changes, add or update a focused xUnit test in
`7-Tests/PuppyWorkbooks.Tests` and run:

```powershell
dotnet test 7-Tests/PuppyWorkbooks.Tests/PuppyWorkbooks.Tests.csproj
```
