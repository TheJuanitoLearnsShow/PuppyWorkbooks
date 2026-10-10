using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using PuppyWorkbooks.Integration.Models;
using PuppyWorkbooks.Integration.Providers;

namespace PuppyWorkbooks.Integration.Engine;

public sealed class IntegrationRunner
{
    private static readonly Regex InputTemplatePattern = new(
        @"\{\{\s*InputRecord\.([\w.]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly WorkbookInterpreter _interpreter = new();
    private readonly IntegrationRunnerOptions _options;
    private readonly MockManager _mockManager;
    private SecretManager? _secretManager;

    public IntegrationRunner(IntegrationRunnerOptions? options = null)
    {
        _options = options ?? new();
        _mockManager = new MockManager(_options);
    }

    public async Task<IntegrationResult> RunAsync(IntegrationDefinition definition, CancellationToken cancellationToken = default)
    {
        _secretManager = new SecretManager(definition.SecretManager);
        AssignDataToMemoryInputProviders(definition);
        var input = definition.Steps.OfType<InputStep>().FirstOrDefault();
        if (input is null) throw new InvalidOperationException("An integration must contain an IOInput step.");
        await using var inputProvider = CreateInput(input);
        var createdOutputs = new List<IOutputProvider>();
        var outputProviderCache = new Dictionary<string, IOutputProvider>(StringComparer.OrdinalIgnoreCase);

        IOutputProvider GetOrCreateOutput(OutputStep rawStep, IntegrationRecord? record)
        {
            var bound = BindOutputStep(rawStep, record);
            if (_mockManager.ShouldUseMock(bound))
            {
                var mockKey = $"mock:{bound.Id}";
                if (!outputProviderCache.TryGetValue(mockKey, out var mockProvider))
                {
                    mockProvider = new MockOutputProvider();
                    outputProviderCache[mockKey] = mockProvider;
                    createdOutputs.Add(mockProvider);
                }
                return mockProvider;
            }

            var cacheKey = GetOutputCacheKey(bound);
            if (!outputProviderCache.TryGetValue(cacheKey, out var provider))
            {
                provider = CreateOutput(bound);
                outputProviderCache[cacheKey] = provider;
                createdOutputs.Add(provider);
            }
            return provider;
        }

        try
        {
            var reduceSteps = GetReduceSteps(definition.Steps).ToList();
            var reduceStates = reduceSteps.ToDictionary(step => step, step => ValueBinder.ParseInitialState(step.InitialStateJson));
            var outputSteps = definition.Steps.OfType<OutputStep>().ToList();
            var deferOutput = reduceSteps.Count > 0;
            long read = 0, written = 0, excluded = 0;
            IntegrationRecord? final = null;
            var isDebug = _options.Debug;
            var debugRows = isDebug ? new List<IntegrationDebugRow>() : null;

            await ProcessTopLevelInput(inputProvider);

            await ProcessDeferredOutput();

            return new IntegrationResult(read, written, excluded, final)
            {
                DebugData = debugRows
            };

            async Task ProcessDeferredOutput()
            {
                if (deferOutput && final is not null)
                {
                    for (var outputIndex = 0; outputIndex < outputSteps.Count; outputIndex++)
                    {
                        var output = outputSteps[outputIndex];
                        var provider = GetOrCreateOutput(output, final);
                        var status = await provider.WriteAsync(final, cancellationToken);
                        final[output.Id + ".Status"] = status.Succeeded;
                        final[output.Id + ".StatusMessage"] = status.Message;
                        final[output.Id + ".AffectedRows"] = status.AffectedRows;
                        written += status.AffectedRows;
                    }
                }
            }

            async Task ProcessTopLevelInput(IInputProvider provider)
            {
                await foreach (var sourceRecord in provider.ReadAsync(cancellationToken: cancellationToken))
                {
                    read++;
                    var rowDebug = isDebug
                        ? new IntegrationDebugRow { InputRow = new Dictionary<string, object?>(sourceRecord.Values, StringComparer.OrdinalIgnoreCase) }
                        : null;
                    await ProcessStepsAsync(0, sourceRecord, rowDebug, sourceRecord);
                }
            }

            async Task ProcessStepsAsync(int stepIndex, IntegrationRecord record, IntegrationDebugRow? rowDebug, IntegrationRecord sourceRecord)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (stepIndex >= definition.Steps.Count)
                {
                    final = record;
                    if (rowDebug is not null) debugRows!.Add(rowDebug);
                    return;
                }

                var step = definition.Steps[stepIndex];
                switch (step)
                {
                    case InputStep inputStep when ReferenceEquals(inputStep, input):
                        IntegrationDebugger.DebugInputStep(rowDebug, inputStep, record);
                        await ProcessStepsAsync(stepIndex + 1, record, rowDebug, sourceRecord);
                        break;

                    case InputStep inputStep:
                        await ProcessChildInputProvider(cancellationToken, inputStep, record, rowDebug, stepIndex, sourceRecord);
                        break;

                    case MapStep map:
                        var (mapRecord, mapDebug) = await ExecuteMapStep(map, record, isDebug, cancellationToken);
                        IntegrationDebugger.DebugWorksheetStep(mapDebug, rowDebug);
                        await ProcessStepsAsync(stepIndex + 1, mapRecord, rowDebug, sourceRecord);
                        break;

                    case FilterStep filter:
                        var (keepFilter, filterDebug) = await ExecuteFilterStep(filter, record, isDebug, cancellationToken);
                        IntegrationDebugger.DebugWorksheetStep(filterDebug, rowDebug);
                        if (!keepFilter)
                        {
                            excluded++;
                            if (rowDebug is not null) debugRows!.Add(rowDebug);
                            return;
                        }
                        await ProcessStepsAsync(stepIndex + 1, record, rowDebug, sourceRecord);
                        break;

                    case ReduceStep reduce:
                        var (reduceRecord, nextState, reduceDebug) = await ExecuteReduceStep(reduce, record, reduceStates[reduce], isDebug, cancellationToken);
                        reduceStates[reduce] = nextState;
                        IntegrationDebugger.DebugWorksheetStep(reduceDebug, rowDebug);
                        await ProcessStepsAsync(stepIndex + 1, reduceRecord, rowDebug, sourceRecord);
                        break;

                    case SwitchStep @switch:
                        var (switchRecord, switchExcluded, switchDebug) = await ExecuteSwitchStep(@switch, record, reduceStates, isDebug, cancellationToken);
                        IntegrationDebugger.DebugWorksheetStep(switchDebug, rowDebug);
                        if (switchExcluded)
                        {
                            excluded++;
                            if (rowDebug is not null) debugRows!.Add(rowDebug);
                            return;
                        }
                        await ProcessStepsAsync(stepIndex + 1, switchRecord, rowDebug, sourceRecord);
                        break;

                    case OutputStep output when !deferOutput:
                        IntegrationDebugger.DebugOutputStep(rowDebug, output, record);
                        var outputProvider = GetOrCreateOutput(output, record);
                        var status = await outputProvider.WriteAsync(record, cancellationToken);
                        record[output.Id + ".Status"] = status.Succeeded;
                        record[output.Id + ".StatusMessage"] = status.Message;
                        record[output.Id + ".AffectedRows"] = status.AffectedRows;
                        written += status.AffectedRows;
                        await ProcessStepsAsync(stepIndex + 1, record, rowDebug, sourceRecord);
                        break;

                    case OutputStep output when deferOutput:
                        IntegrationDebugger.DebugOutputStep(rowDebug, output, record);
                        await ProcessStepsAsync(stepIndex + 1, record, rowDebug, sourceRecord);
                        break;
                }
            }
            async Task ProcessChildInputProvider(CancellationToken cancellationToken2, InputStep inputStep,
                IntegrationRecord record, IntegrationDebugRow? rowDebug, int stepIndex, IntegrationRecord sourceRecord)
            {
                await using var provider = CreateInput(inputStep, record);
                await foreach (var providerRecord in provider.ReadAsync(record, cancellationToken2))
                {
                    var merged = new Dictionary<string, object?>(record.Values, StringComparer.OrdinalIgnoreCase);
                    foreach (var field in providerRecord.Values)
                        merged[field.Key] = field.Value;
                    var combinedRecord = new IntegrationRecord(merged);
                    var childDebug = CloneDebugRow(rowDebug);
                    IntegrationDebugger.DebugInputStep(childDebug, inputStep, combinedRecord);
                    await ProcessStepsAsync(stepIndex + 1, combinedRecord, childDebug, sourceRecord);
                }
            }
        }
        finally
        {
            foreach (var output in createdOutputs) await output.DisposeAsync();
        }
    }

    private void AssignDataToMemoryInputProviders(IntegrationDefinition definition)
    {
        var inputData = _options.InputData;
        if (inputData is not null)
        {
            var inputStep = definition.Steps.OfType<InputStep>().FirstOrDefault();
            if (inputStep is not null && inputStep.Kind == InputKind.MemoryReader)
            {
                inputStep.Data = inputData;
            }
        }
    }


    private IInputProvider CreateInput(InputStep step, IntegrationRecord? inputRecord = null)
    {
        if (inputRecord is not null)
            step = BindInputStep(step, inputRecord);

        if (_mockManager.ShouldUseMock(step))
        {
            return _mockManager.CreateMockInput(step);
        }

        return step.Kind switch
        {
            InputKind.CSVReader when !string.IsNullOrWhiteSpace(step.FilePath) => new CsvInputProvider(step.FilePath),
            InputKind.JsonReader when !string.IsNullOrWhiteSpace(step.FilePath) => JsonInputProvider.FromFile(step.FilePath, step.JsonPath),
            InputKind.XmlReader when !string.IsNullOrWhiteSpace(step.FilePath) => new XmlInputProvider(step.FilePath, step.XmlItemElement),
            InputKind.FileSystemReader when !string.IsNullOrWhiteSpace(step.FilePath) => new FileSystemInputProvider(step.FilePath, step.AddFileSizeField, step.AddCreatedOnDate, step.AddLastModifiedDateField),
            InputKind.SqlReader when _options.ConnectionFactory is not null => new SqlInputProvider(_options.ConnectionFactory(step.ConnectionString), step.Query),
            InputKind.HttpReader when step.ResolvedHttpConfiguration is not null => new HttpInputProvider(step.ResolvedHttpConfiguration, step.Endpoint, step.HttpMethod, step.JsonPath, _options.HttpClientFactory),
            InputKind.MemoryReader => new MemoryInputProvider(step.Data),
            InputKind.SqlReader => throw new InvalidOperationException("SQL input requires ConnectionFactory; unsupported or missing input configuration."),
            InputKind.HttpReader => throw new InvalidOperationException("HTTP input requires a resolved HTTP configuration."),
            _ => throw new InvalidOperationException("Input provider configuration is missing or unsupported.")
        };
    }

    private string ResolveFieldOrValue(string staticValue, string fromField, IntegrationRecord? inputRecord)
    {
        if (!string.IsNullOrWhiteSpace(fromField))
        {
            if (inputRecord is not null)
            {
                var field = inputRecord.Values.FirstOrDefault(pair => string.Equals(pair.Key, fromField, StringComparison.OrdinalIgnoreCase));
                if (field.Key is not null)
                {
                    return FormatInputValue(field.Value);
                }
                throw new InvalidOperationException($"Step references missing input field '{fromField}'.");
            }
        }

        return ResolveValue(staticValue, inputRecord);
    }

    private string ResolveValue(string value, IntegrationRecord? inputRecord = null)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var resolved = _secretManager!.Resolve(value);
        if (inputRecord is null) return resolved;
        
        return InputTemplatePattern.Replace(resolved, match =>
        {
            var fieldName = match.Groups[1].Value;
            var field = inputRecord.Values.FirstOrDefault(pair => string.Equals(pair.Key, fieldName, StringComparison.OrdinalIgnoreCase));
            if (field.Key is null)
                throw new InvalidOperationException($"Input step references missing input field '{fieldName}'.");
            return FormatInputValue(field.Value);
        });
    }

    private InputStep BindInputStep(InputStep step, IntegrationRecord? inputRecord)
    {
        var bound = new InputStep
        {
            Id = step.Id,
            Kind = step.Kind,
            FilePath = ResolveFieldOrValue(step.FilePath, step.FilePathFromField, inputRecord),
            FilePathFromField = step.FilePathFromField,
            ConnectionString = ResolveFieldOrValue(step.ConnectionString, step.ConnectionStringFromField, inputRecord),
            ConnectionStringFromField = step.ConnectionStringFromField,
            TableName = ResolveFieldOrValue(step.TableName, step.TableNameFromField, inputRecord),
            TableNameFromField = step.TableNameFromField,
            Query = ResolveFieldOrValue(step.Query, step.QueryFromField, inputRecord),
            QueryFromField = step.QueryFromField,
            Data = step.Data,
            MockCsvFilePath = ResolveValue(step.MockCsvFilePath, inputRecord),
            MockCsv = ResolveValue(step.MockCsv, inputRecord),
            MockData = ResolveValue(step.MockData, inputRecord),
            HttpConfiguration = step.HttpConfiguration,
            Endpoint = ResolveFieldOrValue(step.Endpoint, step.EndpointFromField, inputRecord),
            EndpointFromField = step.EndpointFromField,
            HttpMethod = ResolveValue(step.HttpMethod, inputRecord),
            JsonPath = ResolveFieldOrValue(step.JsonPath, step.JsonPathFromField, inputRecord),
            JsonPathFromField = step.JsonPathFromField,
            XmlItemElement = ResolveFieldOrValue(step.XmlItemElement, step.XmlItemElementFromField, inputRecord),
            XmlItemElementFromField = step.XmlItemElementFromField,
            XmlRootElement = ResolveFieldOrValue(step.XmlRootElement, step.XmlRootElementFromField, inputRecord),
            XmlRootElementFromField = step.XmlRootElementFromField,
            XmlRecordElement = ResolveFieldOrValue(step.XmlRecordElement, step.XmlRecordElementFromField, inputRecord),
            XmlRecordElementFromField = step.XmlRecordElementFromField,
            AddFileSizeField = step.AddFileSizeField,
            AddCreatedOnDate = step.AddCreatedOnDate,
            AddLastModifiedDateField = step.AddLastModifiedDateField,
            ResolvedHttpConfiguration = BindHttpConfiguration(step.ResolvedHttpConfiguration, inputRecord)
        };

        foreach (var (name, source) in step.MockDataSources)
        {
            bound.MockDataSources[name] = new MockDataSource
            {
                Name = source.Name,
                FilePath = ResolveValue(source.FilePath, inputRecord),
                Content = ResolveValue(source.Content, inputRecord)
            };
        }

        return bound;
    }

    private OutputStep BindOutputStep(OutputStep step, IntegrationRecord? inputRecord)
    {
        return new OutputStep
        {
            Id = step.Id,
            Kind = step.Kind,
            FilePath = ResolveFieldOrValue(step.FilePath, step.FilePathFromField, inputRecord),
            FilePathFromField = step.FilePathFromField,
            ConnectionString = ResolveFieldOrValue(step.ConnectionString, step.ConnectionStringFromField, inputRecord),
            ConnectionStringFromField = step.ConnectionStringFromField,
            TableName = ResolveFieldOrValue(step.TableName, step.TableNameFromField, inputRecord),
            TableNameFromField = step.TableNameFromField,
            Query = ResolveFieldOrValue(step.Query, step.QueryFromField, inputRecord),
            QueryFromField = step.QueryFromField,
            HttpConfiguration = step.HttpConfiguration,
            Endpoint = ResolveFieldOrValue(step.Endpoint, step.EndpointFromField, inputRecord),
            EndpointFromField = step.EndpointFromField,
            HttpMethod = ResolveValue(step.HttpMethod, inputRecord),
            PayloadFormat = step.PayloadFormat,
            JsonPath = ResolveFieldOrValue(step.JsonPath, step.JsonPathFromField, inputRecord),
            JsonPathFromField = step.JsonPathFromField,
            XmlItemElement = ResolveFieldOrValue(step.XmlItemElement, step.XmlItemElementFromField, inputRecord),
            XmlItemElementFromField = step.XmlItemElementFromField,
            XmlRootElement = ResolveFieldOrValue(step.XmlRootElement, step.XmlRootElementFromField, inputRecord),
            XmlRootElementFromField = step.XmlRootElementFromField,
            XmlRecordElement = ResolveFieldOrValue(step.XmlRecordElement, step.XmlRecordElementFromField, inputRecord),
            XmlRecordElementFromField = step.XmlRecordElementFromField,
            ResolvedHttpConfiguration = BindHttpConfiguration(step.ResolvedHttpConfiguration, inputRecord)
        };
    }

    private HttpProviderSettings? BindHttpConfiguration(HttpProviderSettings? settings, IntegrationRecord? inputRecord)
    {
        if (settings is null) return null;
        return new HttpProviderSettings
        {
            Name = settings.Name,
            BaseUrl = ResolveFieldOrValue(settings.BaseUrl, settings.BaseUrlFromField, inputRecord),
            BaseUrlFromField = settings.BaseUrlFromField,
            HttpClientName = ResolveValue(settings.HttpClientName, inputRecord),
            OAuthClientId = ResolveFieldOrValue(settings.OAuthClientId, settings.OAuthClientIdFromField, inputRecord),
            OAuthClientIdFromField = settings.OAuthClientIdFromField,
            OAuthClientSecret = ResolveFieldOrValue(settings.OAuthClientSecret, settings.OAuthClientSecretFromField, inputRecord),
            OAuthClientSecretFromField = settings.OAuthClientSecretFromField,
            OAuthScope = ResolveFieldOrValue(settings.OAuthScope, settings.OAuthScopeFromField, inputRecord),
            OAuthScopeFromField = settings.OAuthScopeFromField,
            OAuthTokenUrl = ResolveFieldOrValue(settings.OAuthTokenUrl, settings.OAuthTokenUrlFromField, inputRecord),
            OAuthTokenUrlFromField = settings.OAuthTokenUrlFromField,
            OAuthHttpClientName = ResolveValue(settings.OAuthHttpClientName, inputRecord),
            ClientCertificateThumbprint = ResolveFieldOrValue(settings.ClientCertificateThumbprint, settings.ClientCertificateThumbprintFromField, inputRecord),
            ClientCertificateThumbprintFromField = settings.ClientCertificateThumbprintFromField,
            Headers = settings.Headers.Select(header => new HttpHeader
            {
                Name = ResolveValue(header.Name, inputRecord),
                Value = ResolveFieldOrValue(header.Value, header.ValueFromField, inputRecord),
                ValueFromField = header.ValueFromField
            }).ToList()
        };
    }

    private static string GetOutputCacheKey(OutputStep step) => step.Kind switch
    {
        OutputKind.CSVWriter => $"csv:{step.Id}:{step.FilePath}",
        OutputKind.JsonWriter => $"json:{step.Id}:{step.FilePath}",
        OutputKind.XmlWriter => $"xml:{step.Id}:{step.FilePath}:{step.XmlRootElement}:{step.XmlRecordElement}",
        OutputKind.SqlWriter => $"sql:{step.Id}:{step.ConnectionString}:{step.TableName}:{step.Query}",
        OutputKind.HttpWriter => $"http:{step.Id}:{step.Endpoint}:{step.HttpMethod}:{step.PayloadFormat}:{step.ResolvedHttpConfiguration?.BaseUrl}:{step.ResolvedHttpConfiguration?.OAuthClientId}:{step.ResolvedHttpConfiguration?.OAuthTokenUrl}:{string.Join(";", step.ResolvedHttpConfiguration?.Headers.Select(h => $"{h.Name}={h.Value}") ?? [])}",
        _ => $"{step.Kind}:{step.Id}"
    };

    private static string FormatInputValue(object? value) => value switch
    {
        null => string.Empty,
        JsonElement { ValueKind: JsonValueKind.String } json => json.GetString() ?? string.Empty,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };

    private static IntegrationDebugRow? CloneDebugRow(IntegrationDebugRow? rowDebug)
    {
        if (rowDebug is null) return null;
        return new IntegrationDebugRow
        {
            InputRow = new Dictionary<string, object?>(rowDebug.InputRow, StringComparer.OrdinalIgnoreCase),
            Steps = [.. rowDebug.Steps]
        };
    }

    private IOutputProvider CreateOutput(OutputStep step)
    {
        if (_mockManager.ShouldUseMock(step))
        {
            return new MockOutputProvider();
        }

        return step.Kind switch
        {
            OutputKind.CSVWriter when !string.IsNullOrWhiteSpace(step.FilePath) => new CsvOutputProvider(step.FilePath),
            OutputKind.JsonWriter when !string.IsNullOrWhiteSpace(step.FilePath) => new JsonOutputProvider(step.FilePath),
            OutputKind.XmlWriter when !string.IsNullOrWhiteSpace(step.FilePath) => new XmlOutputProvider(step.FilePath, step.XmlRootElement, step.XmlRecordElement),
            OutputKind.SqlWriter when _options.ConnectionFactory is not null => new SqlOutputProvider(_options.ConnectionFactory(step.ConnectionString), step.TableName, step.Query),
            OutputKind.HttpWriter when step.ResolvedHttpConfiguration is not null => new HttpOutputProvider(step.ResolvedHttpConfiguration, step.Endpoint, step.HttpMethod, step.PayloadFormat, _options.HttpClientFactory),
            OutputKind.SqlWriter => throw new InvalidOperationException("SQL output requires ConnectionFactory; unsupported or missing output configuration."),
            OutputKind.HttpWriter => throw new InvalidOperationException("HTTP output requires a resolved HTTP configuration."),
            _ => throw new InvalidOperationException("Output provider configuration is missing or unsupported.")
        };
    }

    private async Task<(IntegrationRecord Record, IntegrationStepDebug? Debug)> ExecuteMapStep(
        MapStep step,
        IntegrationRecord record,
        bool isDebug,
        CancellationToken token)
    {
        var values = await EvaluateCells(step.Worksheet, record, token);
        var stepDebug = isDebug
            ? new IntegrationStepDebug
            {
                Id = step.Id,
                StepType = "Map",
                Cells = values
            }
            : null;
        return (new IntegrationRecord(values), stepDebug);
    }

    private async Task<(bool ShouldKeepRecord, IntegrationStepDebug? Debug)> ExecuteFilterStep(
        FilterStep step,
        IntegrationRecord record,
        bool isDebug,
        CancellationToken token)
    {
        var values = await EvaluateCells(step.Worksheet, record, token);
        var stepDebug = IntegrationDebugger.InitializeFilterDebug(step, isDebug, values);
        var last = values.Values.LastOrDefault();
        var keep = ValueBinder.ToBoolean(last);
        var shouldKeep = step.KeepWhenTrue ? keep : !keep;
        return (shouldKeep, stepDebug);
    }

    private async Task<(IntegrationRecord Record, object? State, IntegrationStepDebug? Debug)> ExecuteReduceStep(
        ReduceStep step,
        IntegrationRecord record,
        object? state,
        bool isDebug,
        CancellationToken token)
    {
        var values = await EvaluateCells(step.Worksheet, record, token,
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["State"] = state });
        var stepDebug = IntegrationDebugger.InitializeReduceDebugStep(step, isDebug, values);
        var next = values.TryGetValue(step.OutputField, out var fieldVal)
            ? fieldVal
            : values.Values.LastOrDefault();
        return (ValueBinder.CreateStateRecord(step.OutputField, next), next, stepDebug);
    }

    private async Task<(IntegrationRecord Record, bool Excluded, IntegrationStepDebug? Debug)> ExecuteSwitchStep(
        SwitchStep step,
        IntegrationRecord record,
        Dictionary<ReduceStep, object?> reduceStates,
        bool isDebug,
        CancellationToken token)
    {
        var values = await EvaluateCells(step.Worksheet, record, token);
        var switchDebug = IntegrationDebugger.InitializeSwitchStepDebug(step, isDebug, values);

        foreach (var branch in step.Branches)
        {
            if (string.IsNullOrWhiteSpace(branch.WorkCell))
                throw new InvalidOperationException($"Switch '{step.Id}' has a branch without a WorkCell attribute.");
            if (!values.TryGetValue(branch.WorkCell, out var value))
                throw new InvalidOperationException($"Switch '{step.Id}' does not contain a worksheet cell named '{branch.WorkCell}'.");

            var conditionMet = ValueBinder.ToBoolean(value);
            var branchDebug = IntegrationDebugger.IntegrationBranchDebug(isDebug, branch, conditionMet, switchDebug);

            if (!conditionMet) continue;

            foreach (var branchStep in branch.Steps)
            {
                token.ThrowIfCancellationRequested();
                switch (branchStep)
                {
                    case MapStep map:
                        var (mapRecord, mapDebug) = await ExecuteMapStep(map, record, isDebug, token);
                        record = mapRecord;
                        branchDebug?.AddStep(mapDebug);
                        break;

                    case FilterStep filter:
                        var (keepFilter, filterDebug) = await ExecuteFilterStep(filter, record, isDebug, token);
                        branchDebug?.AddStep(filterDebug);
                        if (!keepFilter) return (record, true, switchDebug);
                        break;

                    case ReduceStep reduce:
                        var (reduceRecord, newState, reduceDebug) = await ExecuteReduceStep(reduce, record, reduceStates[reduce], isDebug, token);
                        reduceStates[reduce] = newState;
                        record = reduceRecord;
                        branchDebug?.AddStep(reduceDebug);
                        break;

                    case SwitchStep nested:
                        var (nestedRecord, nestedExcluded, nestedDebug) = await ExecuteSwitchStep(nested, record, reduceStates, isDebug, token);
                        record = nestedRecord;
                        branchDebug?.AddStep(nestedDebug);
                        if (nestedExcluded) return (record, true, switchDebug);
                        break;

                    case InputStep or OutputStep:
                        throw new InvalidOperationException("Input and output steps are not valid inside a switch branch.");
                }
            }
        }
        return (record, false, switchDebug);
    }

    private static IEnumerable<ReduceStep> GetReduceSteps(IEnumerable<IntegrationStep> steps)
    {
        foreach (var step in steps)
        {
            if (step is ReduceStep reduce) yield return reduce;
            if (step is SwitchStep @switch)
                foreach (var branch in @switch.Branches)
                    foreach (var nested in GetReduceSteps(branch.Steps)) yield return nested;
        }
    }

    private async Task<Dictionary<string, object?>> EvaluateCells(WorkSheet? worksheet,
        IntegrationRecord record, CancellationToken token,
        IReadOnlyDictionary<string, object?>? additionalBindings = null)
    {
        if (worksheet is null) throw new InvalidOperationException("A worksheet is required for this step.");
        var outputCells = worksheet.Cells
            .Where(c => !string.IsNullOrWhiteSpace(c.Formula))
            .ToList();
        var copy = new WorkSheet { Name = worksheet.Name, Cells =
            [.. worksheet.Cells.Select(c => new WorkCell(c.Id, c.Name, c.Formula, c.Comments))],
            Variables = new Dictionary<string, string>(worksheet.Variables, StringComparer.OrdinalIgnoreCase)
        };
        ValueBinder.BindRecord(copy, record);
        if (additionalBindings is not null)
            foreach (var binding in additionalBindings)
                ValueBinder.BindValue(copy, binding.Key, binding.Value);
        var values = await _interpreter.EvaluateCellsAsync(copy, token);
        var ordered = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in outputCells)
        {
            if (values.TryGetValue(cell.Name, out var cellVal))
            {
                ordered[cell.Name] = cellVal;
            }
        }
        return ordered;
    }
}
