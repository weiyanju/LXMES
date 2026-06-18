using VfdControl.Application.Abstractions;
using VfdControl.Application.Traceability;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;
using VfdControl.Domain.Rules;
using VfdControl.Domain.ValueObjects;
using VfdControl.Application.Execution.StepExecutors;

namespace VfdControl.Application.Execution;

public sealed class WorkflowEngine : IWorkflowEngine
{
    private readonly ITraceRepository _traceRepository;
    private readonly StartStepExecutor _startExecutor;
    private readonly StopStepExecutor _stopExecutor;
    private readonly ReadMeasurementStepExecutor _readMeasurementExecutor;
    private readonly CompareMeasurementStepExecutor _compareMeasurementExecutor = new();
    private readonly ReadStringStepExecutor _readStringExecutor;

    public WorkflowEngine(IDeviceCommandClient deviceClient, ITraceRepository traceRepository)
    {
        _traceRepository = traceRepository;
        _startExecutor = new StartStepExecutor(deviceClient);
        _stopExecutor = new StopStepExecutor(deviceClient);
        _readMeasurementExecutor = new ReadMeasurementStepExecutor(deviceClient);
        _readStringExecutor = new ReadStringStepExecutor(deviceClient);
    }

    public async Task<DeviceRunResult> ExecuteAsync(DeviceRunContext context, CancellationToken cancellationToken)
    {
        var measurements = new Dictionary<string, MeasurementValue>(StringComparer.OrdinalIgnoreCase);
        var snapshots = new List<StepRunSnapshot>();
        var finalConclusion = Conclusion.Pass;

        foreach (var step in context.PlanVersion.Steps.OrderBy(step => step.Sequence))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stepRunId = Guid.NewGuid();
            var result = await ExecuteStepAsync(context, step, stepRunId, measurements, cancellationToken);
            var snapshot = new StepRunSnapshot(
                stepRunId,
                context.DeviceRunId,
                step.Sequence,
                step.Name,
                result.Conclusion,
                result.Message);
            snapshots.Add(snapshot);
            context.ProgressHandler?.Invoke(new SlotStepProgressSnapshot(
                context.Slot.Id,
                context.DeviceRunId,
                step.Sequence,
                step.Name,
                result.Conclusion,
                result.Message));
            await _traceRepository.SaveStepRunAsync(snapshot, cancellationToken);

            finalConclusion = Aggregate(finalConclusion, result.Conclusion, step.AffectsFinalConclusion);

            if (result.Conclusion == Conclusion.Fail && step.FailurePolicy.Action == FailureAction.StopSlotImmediately)
            {
                await _stopExecutor.ExecuteSafetyStopAsync(context, cancellationToken);
                break;
            }
        }

        return new DeviceRunResult(context.DeviceRunId, finalConclusion, snapshots);
    }

    private async Task<RuleEvaluationResult> ExecuteStepAsync(
        DeviceRunContext context,
        ProcessStep step,
        Guid stepRunId,
        Dictionary<string, MeasurementValue> measurements,
        CancellationToken ct)
    {
        return step.Command.CommandType switch
        {
            "Start" => await ExecuteWriteAsync(context, step, stepRunId, _startExecutor.ExecuteAsync, ct),
            "Stop" => await ExecuteWriteAsync(context, step, stepRunId, _stopExecutor.ExecuteAsync, ct),
            "Delay" => await ExecuteDelayAsync(context, step, ct),
            "ReadMeasurement" => await ExecuteReadMeasurementAsync(context, step, stepRunId, measurements, ct),
            "CompareMeasurement" => await ExecuteCompareMeasurementAsync(step, stepRunId, measurements, ct),
            "ReadString" => await ExecuteReadStringAsync(context, step, stepRunId, ct),
            _ => new RuleEvaluationResult(Conclusion.Fail, $"Unsupported step command type '{step.Command.CommandType}'.", step.AffectsFinalConclusion)
        };
    }

    private async Task<RuleEvaluationResult> ExecuteWriteAsync(
        DeviceRunContext context,
        ProcessStep step,
        Guid stepRunId,
        Func<DeviceRunContext, ProcessStep, CancellationToken, Task<CommandResult>> execute,
        CancellationToken ct)
    {
        var result = await execute(context, step, ct);
        await SaveCommandTraceAsync(context, stepRunId, step.Command.CommandType, result, ct);

        return new RuleEvaluationResult(
            result.IsSuccess ? Conclusion.Pass : Conclusion.Fail,
            result.Message,
            step.AffectsFinalConclusion);
    }

    private async Task<RuleEvaluationResult> ExecuteDelayAsync(DeviceRunContext context, ProcessStep step, CancellationToken ct)
    {
        var delayMilliseconds = int.TryParse(step.Command.Value, out var parsed)
            ? Math.Max(0, parsed)
            : 0;
        var total = TimeSpan.FromMilliseconds(delayMilliseconds);
        var remaining = total;
        if (remaining > TimeSpan.Zero)
        {
            context.ProgressHandler?.Invoke(new SlotStepProgressSnapshot(
                context.Slot.Id,
                context.DeviceRunId,
                step.Sequence,
                step.Name,
                Conclusion.None,
                "\u7B49\u5F85\u4E2D",
                remaining,
                IsRunning: true));
        }

        var startedAt = DateTimeOffset.UtcNow;
        while (remaining > TimeSpan.Zero)
        {
            var interval = remaining < TimeSpan.FromMilliseconds(500)
                ? remaining
                : TimeSpan.FromMilliseconds(500);
            await Task.Delay(interval, ct);

            remaining = total - (DateTimeOffset.UtcNow - startedAt);
            if (remaining > TimeSpan.Zero)
            {
                context.ProgressHandler?.Invoke(new SlotStepProgressSnapshot(
                    context.Slot.Id,
                    context.DeviceRunId,
                    step.Sequence,
                    step.Name,
                    Conclusion.None,
                    "\u7B49\u5F85\u4E2D",
                    remaining,
                    IsRunning: true));
            }
        }

        return new RuleEvaluationResult(Conclusion.Pass, "Delay completed.", step.AffectsFinalConclusion);
    }

    private async Task<RuleEvaluationResult> ExecuteReadMeasurementAsync(
        DeviceRunContext context,
        ProcessStep step,
        Guid stepRunId,
        Dictionary<string, MeasurementValue> measurements,
        CancellationToken ct)
    {
        var result = await _readMeasurementExecutor.ExecuteAsync(context, step, ct);
        await SaveCommandTraceAsync(context, stepRunId, step.Command.Target, result, ct);

        if (!result.IsSuccess || result.Value is null)
        {
            return new RuleEvaluationResult(Conclusion.Fail, result.Message, step.AffectsFinalConclusion);
        }

        measurements[step.Command.Target] = result.Value;
        await _traceRepository.SaveMeasurementResultAsync(
            new MeasurementTrace(
                stepRunId,
                step.Command.Target,
                result.Value.NumericValue,
                result.Value.Unit,
                result.Value.Source),
            ct);

        if (step.Rule?.RuleType == StepRule.NumericRangeRuleType)
        {
            var rangeRule = new NumericRangeRule(
                step.Rule.LowerLimit,
                step.Rule.UpperLimit,
                step.AffectsFinalConclusion);
            return rangeRule.Evaluate(result.Value.NumericValue);
        }

        return new RuleEvaluationResult(Conclusion.Pass, result.Message, step.AffectsFinalConclusion);
    }

    private async Task<RuleEvaluationResult> ExecuteCompareMeasurementAsync(
        ProcessStep step,
        Guid stepRunId,
        IReadOnlyDictionary<string, MeasurementValue> measurements,
        CancellationToken ct)
    {
        var result = _compareMeasurementExecutor.Execute(step, measurements);
        var parts = step.Command.Target.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        await _traceRepository.SaveComparisonResultAsync(
            new ComparisonTrace(
                stepRunId,
                parts.ElementAtOrDefault(0) ?? string.Empty,
                parts.ElementAtOrDefault(1) ?? string.Empty,
                result.Conclusion,
                result.Message),
            ct);

        return result;
    }

    private async Task<RuleEvaluationResult> ExecuteReadStringAsync(
        DeviceRunContext context,
        ProcessStep step,
        Guid stepRunId,
        CancellationToken ct)
    {
        var result = await _readStringExecutor.ExecuteAsync(context, step, ct);
        await SaveCommandTraceAsync(context, stepRunId, step.Command.Target, result, ct);

        if (!result.IsSuccess || result.Value is null)
        {
            return new RuleEvaluationResult(Conclusion.Fail, result.Message, step.AffectsFinalConclusion);
        }

        var expectedValue = step.Rule?.RuleType == StepRule.StringEqualsRuleType
            ? step.Rule.ExpectedValue
            : step.Command.Value;
        if (expectedValue is null)
        {
            return new RuleEvaluationResult(Conclusion.Pass, result.Message, step.AffectsFinalConclusion);
        }

        var stringRule = StringMatchRule.Exact(expectedValue);
        return stringRule.Evaluate(result.Value);
    }

    private async Task SaveCommandTraceAsync(
        DeviceRunContext context,
        Guid stepRunId,
        string commandName,
        CommandResult result,
        CancellationToken ct)
    {
        var trace = new CommandTraceSnapshot(
            Guid.NewGuid(),
            stepRunId,
            context.Slot.Id,
            commandName,
            result.RequestJson,
            string.IsNullOrWhiteSpace(result.ResponseJson) || result.ResponseJson == "{}" ? result.Message : result.ResponseJson,
            result.IsSuccess,
            DateTimeOffset.UtcNow);

        await _traceRepository.SaveCommandTraceAsync(trace, ct);
    }

    private Task SaveCommandTraceAsync<T>(
        DeviceRunContext context,
        Guid stepRunId,
        string commandName,
        CommandResult<T> result,
        CancellationToken ct)
    {
        var trace = new CommandTraceSnapshot(
            Guid.NewGuid(),
            stepRunId,
            context.Slot.Id,
            commandName,
            result.RequestJson,
            string.IsNullOrWhiteSpace(result.ResponseJson) || result.ResponseJson == "{}" ? result.Message : result.ResponseJson,
            result.IsSuccess,
            DateTimeOffset.UtcNow);

        return _traceRepository.SaveCommandTraceAsync(trace, ct);
    }

    private static Conclusion Aggregate(Conclusion current, Conclusion next, bool affectsFinalConclusion)
    {
        if (!affectsFinalConclusion && next != Conclusion.Warning)
        {
            return current;
        }

        if (next == Conclusion.Fail && affectsFinalConclusion)
        {
            return Conclusion.Fail;
        }

        if (next == Conclusion.Warning && current != Conclusion.Fail)
        {
            return Conclusion.Warning;
        }

        return current;
    }
}
