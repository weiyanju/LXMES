using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;
using VfdControl.Application.Traceability;
using VfdControl.Domain.Enums;

namespace VfdControl.Infrastructure.InMemory;

public sealed class InMemoryTraceRepository : ITraceRepository
{
    public List<StationSessionSnapshot> Sessions { get; } = [];

    public List<DeviceRunSnapshot> DeviceRuns { get; } = [];

    public List<StepRunSnapshot> StepRuns { get; } = [];

    public List<MeasurementTrace> Measurements { get; } = [];

    public List<ComparisonTrace> Comparisons { get; } = [];

    public List<CommandTraceSnapshot> CommandTraces { get; } = [];

    public Task SaveSessionStartedAsync(StationSessionSnapshot session, CancellationToken ct)
    {
        Sessions.Add(session);
        return Task.CompletedTask;
    }

    public Task SaveDeviceRunAsync(DeviceRunSnapshot run, CancellationToken ct)
    {
        DeviceRuns.Add(run);
        return Task.CompletedTask;
    }

    public Task SaveStepRunAsync(StepRunSnapshot step, CancellationToken ct)
    {
        StepRuns.Add(step);
        return Task.CompletedTask;
    }

    public Task SaveMeasurementResultAsync(MeasurementTrace measurement, CancellationToken ct)
    {
        Measurements.Add(measurement);
        return Task.CompletedTask;
    }

    public Task SaveComparisonResultAsync(ComparisonTrace comparison, CancellationToken ct)
    {
        Comparisons.Add(comparison);
        return Task.CompletedTask;
    }

    public Task SaveCommandTraceAsync(CommandTraceSnapshot trace, CancellationToken ct)
    {
        CommandTraces.Add(trace);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StationSessionSummary>> QuerySessionsAsync(TraceabilitySessionQuery query, CancellationToken ct)
    {
        var results = Sessions
            .Where(session => query.From is null || session.StartedAt >= query.From)
            .Where(session => query.To is null || session.StartedAt <= query.To)
            .Where(session => query.Barcode is null || DeviceRuns.Any(run =>
                run.SessionId == session.SessionId &&
                string.Equals(run.Barcode, query.Barcode, StringComparison.OrdinalIgnoreCase)))
            .Where(session => query.Conclusion is null || DeviceRuns.Any(run =>
                run.SessionId == session.SessionId &&
                run.Conclusion == query.Conclusion))
            .OrderByDescending(session => session.StartedAt)
            .Select(session =>
            {
                var sessionRuns = DeviceRuns.Where(run => run.SessionId == session.SessionId).ToList();
                return new StationSessionSummary(
                    session.SessionId,
                    session.StationId,
                    session.OperatorCode,
                    session.StartedAt,
                    AggregateConclusion(sessionRuns),
                    sessionRuns.Count);
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<StationSessionSummary>>(results);
    }

    public Task<IReadOnlyList<DeviceRunSummary>> QueryDeviceRunsAsync(DeviceRunQuery query, CancellationToken ct)
    {
        var results = DeviceRuns
            .Where(run => query.Barcode is null || string.Equals(run.Barcode, query.Barcode, StringComparison.OrdinalIgnoreCase))
            .Where(run => query.Conclusion is null || run.Conclusion == query.Conclusion)
            .Where(run => MatchesSessionDateRange(run, query))
            .Select(run => new DeviceRunSummary(run.DeviceRunId, run.SessionId, run.Barcode, run.Conclusion, StartedAtFor(run)))
            .OrderByDescending(run => run.StartedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<DeviceRunSummary>>(results);
    }

    public Task<DeviceRunTrace?> GetDeviceRunTraceAsync(Guid deviceRunId, CancellationToken ct)
    {
        var run = DeviceRuns.SingleOrDefault(run => run.DeviceRunId == deviceRunId);
        if (run is null)
        {
            return Task.FromResult<DeviceRunTrace?>(null);
        }

        var steps = StepRuns
            .Where(step => step.DeviceRunId == deviceRunId)
            .OrderBy(step => step.Sequence)
            .Select(step => new StepRunTrace(
                step.StepRunId,
                step.DeviceRunId,
                step.Sequence,
                step.StepName,
                step.Conclusion,
                Measurements.Where(measurement => measurement.StepRunId == step.StepRunId).ToList(),
                Comparisons.Where(comparison => comparison.StepRunId == step.StepRunId).ToList(),
                CommandTraces.Where(trace => trace.StepRunId == step.StepRunId).OrderBy(trace => trace.CreatedAt).ToList(),
                step.Message))
            .ToList();

        return Task.FromResult<DeviceRunTrace?>(new DeviceRunTrace(
            run.DeviceRunId,
            run.SessionId,
            run.SlotId,
            run.Barcode,
            run.Conclusion,
            StartedAtFor(run),
            steps));
    }

    private static Conclusion? AggregateConclusion(IReadOnlyList<DeviceRunSnapshot> runs)
    {
        if (runs.Count == 0)
        {
            return null;
        }

        if (runs.Any(run => run.Conclusion == Conclusion.Fail))
        {
            return Conclusion.Fail;
        }

        return runs.Any(run => run.Conclusion == Conclusion.Warning)
            ? Conclusion.Warning
            : Conclusion.Pass;
    }

    private bool MatchesSessionDateRange(DeviceRunSnapshot run, DeviceRunQuery query)
    {
        var startedAt = StartedAtFor(run);
        return (query.From is null || startedAt >= query.From) &&
               (query.To is null || startedAt <= query.To);
    }

    private DateTimeOffset StartedAtFor(DeviceRunSnapshot run)
    {
        return Sessions.SingleOrDefault(session => session.SessionId == run.SessionId)?.StartedAt ?? DateTimeOffset.MinValue;
    }
}
