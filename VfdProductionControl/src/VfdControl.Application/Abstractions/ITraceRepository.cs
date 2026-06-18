using VfdControl.Application.Execution;
using VfdControl.Application.Traceability;

namespace VfdControl.Application.Abstractions;

public interface ITraceRepository
{
    Task SaveSessionStartedAsync(StationSessionSnapshot session, CancellationToken ct);

    Task SaveDeviceRunAsync(DeviceRunSnapshot run, CancellationToken ct);

    Task SaveStepRunAsync(StepRunSnapshot step, CancellationToken ct);

    Task SaveMeasurementResultAsync(MeasurementTrace measurement, CancellationToken ct);

    Task SaveComparisonResultAsync(ComparisonTrace comparison, CancellationToken ct);

    Task SaveCommandTraceAsync(CommandTraceSnapshot trace, CancellationToken ct);

    Task<IReadOnlyList<StationSessionSummary>> QuerySessionsAsync(TraceabilitySessionQuery query, CancellationToken ct);

    Task<IReadOnlyList<DeviceRunSummary>> QueryDeviceRunsAsync(DeviceRunQuery query, CancellationToken ct);

    Task<DeviceRunTrace?> GetDeviceRunTraceAsync(Guid deviceRunId, CancellationToken ct);
}
