using VfdControl.Application.Abstractions;
using VfdControl.Domain.Enums;

namespace VfdControl.Application.Execution;

public sealed class SlotScheduler : ISlotScheduler
{
    private readonly IWorkflowEngine _workflowEngine;
    private readonly SlotExecutionStateStore _stateStore;
    private readonly ITraceRepository? _traceRepository;
    private readonly Dictionary<Guid, SemaphoreSlim> _slotLocks = [];
    private readonly Dictionary<Guid, List<Guid>> _sessionSlots = [];
    private readonly object _sync = new();

    public SlotScheduler(IWorkflowEngine workflowEngine, SlotExecutionStateStore stateStore, ITraceRepository? traceRepository = null)
    {
        _workflowEngine = workflowEngine;
        _stateStore = stateStore;
        _traceRepository = traceRepository;
    }

    public async Task<StationSessionResult> RunAsync(StationSessionContext context, CancellationToken cancellationToken)
    {
        RegisterSessionSlots(context);
        if (_traceRepository is not null)
        {
            await _traceRepository.SaveSessionStartedAsync(
                new StationSessionSnapshot(
                    context.SessionId,
                    context.Station.Id,
                    context.OperatorCode.Value,
                    DateTimeOffset.UtcNow),
                cancellationToken);
        }

        var tasks = context.SlotBindings
            .Select(binding => RunSlotAsync(context, binding, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var conclusion = results.Any(result => result.Conclusion == Conclusion.Fail)
            ? Conclusion.Fail
            : results.Any(result => result.Conclusion == Conclusion.Warning)
                ? Conclusion.Warning
                : Conclusion.Pass;

        return new StationSessionResult(context.SessionId, conclusion, results);
    }

    public Task PauseAsync(Guid sessionId)
    {
        _stateStore.PauseSession(sessionId);
        return Task.CompletedTask;
    }

    public Task ResumeAsync(Guid sessionId)
    {
        _stateStore.ResumeSession(sessionId);
        return Task.CompletedTask;
    }

    public Task StopSlotAsync(Guid sessionId, Guid slotRunId)
    {
        _stateStore.StopSlot(sessionId, slotRunId);
        return Task.CompletedTask;
    }

    public Task StopSessionAsync(Guid sessionId)
    {
        var slotIds = GetSessionSlotIds(sessionId);
        _stateStore.StopSession(sessionId, slotIds);
        return Task.CompletedTask;
    }

    private async Task<DeviceRunResult> RunSlotAsync(
        StationSessionContext sessionContext,
        SlotBarcodeBinding binding,
        CancellationToken cancellationToken)
    {
        var slotLock = GetSlotLock(binding.Slot.Id);
        await slotLock.WaitAsync(cancellationToken);

        try
        {
            var context = new DeviceRunContext(
                sessionContext.SessionId,
                Guid.NewGuid(),
                binding.Slot,
                binding.Barcode,
                sessionContext.PlanVersion,
                sessionContext.ProgressHandler);

            var result = await _workflowEngine.ExecuteAsync(context, cancellationToken);
            if (_traceRepository is not null)
            {
                await _traceRepository.SaveDeviceRunAsync(
                    new DeviceRunSnapshot(
                        result.DeviceRunId,
                        sessionContext.SessionId,
                        binding.Slot.Id,
                        binding.Barcode.Value,
                        result.Conclusion),
                    cancellationToken);
            }

            return result;
        }
        finally
        {
            slotLock.Release();
        }
    }

    private SemaphoreSlim GetSlotLock(Guid slotId)
    {
        lock (_sync)
        {
            if (!_slotLocks.TryGetValue(slotId, out var slotLock))
            {
                slotLock = new SemaphoreSlim(1, 1);
                _slotLocks[slotId] = slotLock;
            }

            return slotLock;
        }
    }

    private void RegisterSessionSlots(StationSessionContext context)
    {
        lock (_sync)
        {
            _sessionSlots[context.SessionId] = context.SlotBindings
                .Select(binding => binding.Slot.Id)
                .Distinct()
                .ToList();
        }
    }

    private IReadOnlyList<Guid> GetSessionSlotIds(Guid sessionId)
    {
        lock (_sync)
        {
            return _sessionSlots.TryGetValue(sessionId, out var slotIds)
                ? slotIds.ToArray()
                : [];
        }
    }
}
