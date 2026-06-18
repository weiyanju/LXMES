using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;

namespace VfdControl.Application.Operator;

public sealed class RunStatusQueryService
{
    private readonly ITraceRepository _traceRepository;

    public RunStatusQueryService(ITraceRepository traceRepository)
    {
        _traceRepository = traceRepository;
    }

    public Task<IReadOnlyList<DeviceRunSummary>> QueryDeviceRunsAsync(DeviceRunQuery query, CancellationToken ct)
    {
        return _traceRepository.QueryDeviceRunsAsync(query, ct);
    }
}
