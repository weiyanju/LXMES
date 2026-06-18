using FluentAssertions;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;
using VfdControl.Domain.Stations;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Application.Tests.Execution;

public class SlotSchedulerTests
{
    [Fact]
    public async Task Selected_slots_start_independent_executions()
    {
        var workflow = new ControllableWorkflowEngine();
        var scheduler = new SlotScheduler(workflow, new SlotExecutionStateStore());
        var context = CreateSessionContext(slotCount: 2);

        var runTask = scheduler.RunAsync(context, CancellationToken.None);
        await workflow.WaitForStartedCountAsync(2);

        workflow.StartedDeviceRuns.Select(run => run.Slot.Id).Should().BeEquivalentTo(context.SlotBindings.Select(binding => binding.Slot.Id));

        workflow.CompleteAll(Conclusion.Pass);
        (await runTask).DeviceRuns.Should().HaveCount(2);
    }

    [Fact]
    public async Task Slot_failure_does_not_stop_other_slot_when_policy_is_single_slot()
    {
        var workflow = new SequenceWorkflowEngine([Conclusion.Fail, Conclusion.Pass]);
        var scheduler = new SlotScheduler(workflow, new SlotExecutionStateStore());
        var context = CreateSessionContext(slotCount: 2);

        var result = await scheduler.RunAsync(context, CancellationToken.None);

        result.Conclusion.Should().Be(Conclusion.Fail);
        result.DeviceRuns.Should().HaveCount(2);
        workflow.StartedDeviceRuns.Should().HaveCount(2);
    }

    [Fact]
    public async Task Pause_all_slots_policy_pauses_all_executions()
    {
        var stateStore = new SlotExecutionStateStore();
        var scheduler = new SlotScheduler(new SequenceWorkflowEngine([Conclusion.Pass]), stateStore);
        var sessionId = Guid.NewGuid();

        await scheduler.PauseAsync(sessionId);

        stateStore.IsPaused(sessionId).Should().BeTrue();
    }

    [Fact]
    public async Task Stop_session_requests_stop_for_all_running_slots()
    {
        var workflow = new ControllableWorkflowEngine();
        var stateStore = new SlotExecutionStateStore();
        var scheduler = new SlotScheduler(workflow, stateStore);
        var context = CreateSessionContext(slotCount: 2);

        var runTask = scheduler.RunAsync(context, CancellationToken.None);
        await workflow.WaitForStartedCountAsync(2);

        await scheduler.StopSessionAsync(context.SessionId);
        workflow.CompleteAll(Conclusion.Pass);
        await runTask;

        stateStore.IsSessionStopRequested(context.SessionId).Should().BeTrue();
        context.SlotBindings.Should().OnlyContain(binding => stateStore.IsSlotStopRequested(context.SessionId, binding.Slot.Id));
    }

    [Fact]
    public async Task Single_slot_serializes_its_own_execution_calls()
    {
        var workflow = new OverlapTrackingWorkflowEngine();
        var scheduler = new SlotScheduler(workflow, new SlotExecutionStateStore());
        var slot = CreateSlot(1);
        var context = CreateSessionContext([slot, slot]);

        await scheduler.RunAsync(context, CancellationToken.None);

        workflow.MaxConcurrentExecutionsBySlot[slot.Id].Should().Be(1);
    }

    private static StationSessionContext CreateSessionContext(int slotCount)
    {
        return CreateSessionContext(Enumerable.Range(1, slotCount).Select(CreateSlot).ToArray());
    }

    private static StationSessionContext CreateSessionContext(IReadOnlyList<StationSlot> slots)
    {
        var station = new Station(Guid.NewGuid(), "Station A");
        foreach (var slot in slots.DistinctBy(slot => slot.Id))
        {
            station.AddSlot(slot);
        }

        var planVersion = new ProcessPlanVersion(Guid.NewGuid(), versionNumber: 1, isExecutable: true);
        var bindings = slots
            .Select((slot, index) => new SlotBarcodeBinding(slot, Barcode.TryCreateVfd($"VFD2026060100{index + 1:00}").Value!))
            .ToArray();

        return new StationSessionContext(
            Guid.NewGuid(),
            station,
            EmployeeCode.TryCreate("EMP0001").Value!,
            planVersion,
            bindings);
    }

    private static StationSlot CreateSlot(int number)
    {
        return new StationSlot(
            Guid.NewGuid(),
            new SlotNumber(number),
            new SlotCommunicationConfig(new SerialPortName($"COM{number}"), new ModbusAddress((byte)number), 9600));
    }

    private sealed class SequenceWorkflowEngine : IWorkflowEngine
    {
        private readonly Queue<Conclusion> _conclusions;

        public SequenceWorkflowEngine(IEnumerable<Conclusion> conclusions)
        {
            _conclusions = new Queue<Conclusion>(conclusions);
        }

        public List<DeviceRunContext> StartedDeviceRuns { get; } = [];

        public Task<DeviceRunResult> ExecuteAsync(DeviceRunContext context, CancellationToken cancellationToken)
        {
            StartedDeviceRuns.Add(context);
            var conclusion = _conclusions.Count > 0 ? _conclusions.Dequeue() : Conclusion.Pass;
            return Task.FromResult(new DeviceRunResult(context.DeviceRunId, conclusion, []));
        }
    }

    private sealed class ControllableWorkflowEngine : IWorkflowEngine
    {
        private readonly List<TaskCompletionSource<DeviceRunResult>> _pendingRuns = [];
        private readonly TaskCompletionSource _twoStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<DeviceRunContext> StartedDeviceRuns { get; } = [];

        public Task<DeviceRunResult> ExecuteAsync(DeviceRunContext context, CancellationToken cancellationToken)
        {
            StartedDeviceRuns.Add(context);
            var completion = new TaskCompletionSource<DeviceRunResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingRuns.Add(completion);

            if (StartedDeviceRuns.Count >= 2)
            {
                _twoStarted.TrySetResult();
            }

            return completion.Task;
        }

        public Task WaitForStartedCountAsync(int count)
        {
            return StartedDeviceRuns.Count >= count ? Task.CompletedTask : _twoStarted.Task;
        }

        public void CompleteAll(Conclusion conclusion)
        {
            foreach (var pendingRun in _pendingRuns)
            {
                pendingRun.TrySetResult(new DeviceRunResult(Guid.NewGuid(), conclusion, []));
            }
        }
    }

    private sealed class OverlapTrackingWorkflowEngine : IWorkflowEngine
    {
        private readonly Dictionary<Guid, int> _activeExecutionsBySlot = [];

        public Dictionary<Guid, int> MaxConcurrentExecutionsBySlot { get; } = [];

        public async Task<DeviceRunResult> ExecuteAsync(DeviceRunContext context, CancellationToken cancellationToken)
        {
            var current = _activeExecutionsBySlot.GetValueOrDefault(context.Slot.Id) + 1;
            _activeExecutionsBySlot[context.Slot.Id] = current;
            MaxConcurrentExecutionsBySlot[context.Slot.Id] = Math.Max(MaxConcurrentExecutionsBySlot.GetValueOrDefault(context.Slot.Id), current);

            await Task.Delay(10, cancellationToken);

            _activeExecutionsBySlot[context.Slot.Id] -= 1;
            return new DeviceRunResult(context.DeviceRunId, Conclusion.Pass, []);
        }
    }
}
