using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Engine;

public sealed class IntegrationDebugger
{
    public static void DebugWorksheetStep(IntegrationStepDebug? mapDebug, IntegrationDebugRow? rowDebug)
    {
        if (mapDebug is not null) rowDebug?.Steps.Add(mapDebug);
    }

    public static void DebugOutputStep(IntegrationDebugRow? rowDebug, OutputStep output, IntegrationRecord record)
    {
        var item = new IntegrationStepDebug
        {
            Id = output.Id,
            StepType = "IOOutput",
            Cells = new Dictionary<string, object?>(record.Values, StringComparer.OrdinalIgnoreCase)
        };
        rowDebug?.Steps.Add(item);
    }

    public static void DebugInputStep(IntegrationDebugRow? rowDebug, InputStep inputStep, IntegrationRecord record)
    {
        var item = new IntegrationStepDebug
        {
            Id = inputStep.Id,
            StepType = "IOInput",
            Cells = new Dictionary<string, object?>(record.Values, StringComparer.OrdinalIgnoreCase)
        };
        rowDebug?.Steps.Add(item);
    }

    public static IntegrationDebugRow? InitializeDebugRow(bool isDebug, IntegrationRecord sourceRecord, List<IntegrationDebugRow>? debugRows)
    {
        IntegrationDebugRow? rowDebug = null;
        if (isDebug)
        {
            rowDebug = new IntegrationDebugRow
            {
                InputRow = new Dictionary<string, object?>(sourceRecord.Values, StringComparer.OrdinalIgnoreCase)
            };
            debugRows!.Add(rowDebug);
        }

        return rowDebug;
    }

    public static IntegrationStepDebug? InitializeFilterDebug(FilterStep step, bool isDebug, Dictionary<string, object?> values)
    {
        return isDebug
            ? new IntegrationStepDebug
            {
                Id = step.Id,
                StepType = "Filter",
                Cells = values
            }
            : null;
    }

    public static IntegrationStepDebug? InitializeReduceDebugStep(ReduceStep step, bool isDebug, Dictionary<string, object?> values)
    {
        return isDebug
            ? new IntegrationStepDebug
            {
                Id = step.Id,
                StepType = "Reduce",
                Cells = values
            }
            : null;
    }

    public static IntegrationStepDebug? InitializeSwitchStepDebug(SwitchStep step, bool isDebug, Dictionary<string, object?> values)
    {
        return isDebug
            ? new IntegrationStepDebug
            {
                Id = step.Id,
                StepType = "Switch",
                Cells = values,
                Branches = []
            }
            : null;
    }

    public static IntegrationBranchDebug? IntegrationBranchDebug(bool isDebug, SwitchBranch branch, bool conditionMet,
        IntegrationStepDebug? switchDebug)
    {
        var branchDebug = isDebug
            ? new IntegrationBranchDebug
            {
                WorkCell = branch.WorkCell,
                Executed = conditionMet,
                Steps = []
            }
            : null;

        if (branchDebug is not null)
        {
            switchDebug!.Branches!.Add(branchDebug);
        }

        return branchDebug;
    }
}