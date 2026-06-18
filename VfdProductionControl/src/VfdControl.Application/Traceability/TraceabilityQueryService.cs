using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;

namespace VfdControl.Application.Traceability;

public sealed class TraceabilityQueryService
{
    private readonly ITraceRepository _traceRepository;

    public TraceabilityQueryService(ITraceRepository traceRepository)
    {
        _traceRepository = traceRepository;
    }

    public Task<IReadOnlyList<StationSessionSummary>> QuerySessionsAsync(TraceabilitySessionQuery query, CancellationToken ct)
    {
        return _traceRepository.QuerySessionsAsync(query, ct);
    }

    public Task<IReadOnlyList<DeviceRunSummary>> QueryDeviceRunsAsync(DeviceRunQuery query, CancellationToken ct)
    {
        return _traceRepository.QueryDeviceRunsAsync(query, ct);
    }

    public Task<DeviceRunTrace?> GetDeviceRunTraceAsync(Guid deviceRunId, CancellationToken ct)
    {
        return _traceRepository.GetDeviceRunTraceAsync(deviceRunId, ct);
    }
}
