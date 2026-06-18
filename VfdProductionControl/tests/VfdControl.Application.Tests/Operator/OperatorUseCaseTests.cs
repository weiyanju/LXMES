using FluentAssertions;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;
using VfdControl.Application.Operator;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;
using VfdControl.Domain.Stations;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Application.Tests.Operator;

public class OperatorUseCaseTests
{
    [Fact]
    public void Employee_code_creates_operator_session()
    {
        var service = new OperatorSessionService();

        var result = service.StartSession("EMP0001");

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmployeeCode.Value.Should().Be("EMP0001");
    }

    [Fact]
    public void Invalid_employee_code_is_rejected()
    {
        var service = new OperatorSessionService();

        var result = service.StartSession("bad");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Executable_plans_are_returned()
    {
        var executable = new ProcessPlanVersion(Guid.NewGuid(), 1, isExecutable: true);
        var draft = new ProcessPlanVersion(Guid.NewGuid(), 2, isExecutable: false);
        var plan = new ProcessPlan(Guid.NewGuid(), "Demo");
        plan.AddVersion(executable);
        plan.AddVersion(draft);
        var service = new PlanSelectionService(new FakeProcessPlanRepository([plan]));

        var versions = await service.GetExecutablePlansAsync(CancellationToken.None);

        versions.Should().ContainSingle().Which.Should().Be(executable);
    }

    [Fact]
    public void Selected_slots_determine_barcode_scan_order()
    {
        var station = CreateStation(slotCount: 4);
        var service = new SlotSelectionService();

        var result = service.CreateScanQueue(station, [3, 1]);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Slots.Select(slot => slot.Number.Value).Should().Equal(3, 1);
    }

    [Fact]
    public void Scanned_vfd_barcodes_bind_to_selected_slots_in_order()
    {
        var station = CreateStation(slotCount: 4);
        var service = new SlotSelectionService();
        var queue = service.CreateScanQueue(station, [3, 1]).Value!;

        var result = service.BindBarcodes(queue, ["VFD202606010001", "VFD202606010002"]);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Bindings.Select(binding => binding.Slot.Number.Value).Should().Equal(3, 1);
        result.Value.Bindings.Select(binding => binding.Barcode.Value).Should().Equal("VFD202606010001", "VFD202606010002");
    }

    [Fact]
    public async Task Production_run_starts_scheduler_with_selected_plan_and_slots()
    {
        var scheduler = new CapturingSlotScheduler();
        var service = new ProductionRunService(scheduler);
        var station = CreateStation(slotCount: 2);
        var planVersion = new ProcessPlanVersion(Guid.NewGuid(), 1, isExecutable: true);
        var operatorSession = new OperatorSession(Guid.NewGuid(), EmployeeCode.TryCreate("EMP0001").Value!, DateTimeOffset.UtcNow);
        var bindings = station.Slots
            .Select((slot, index) => new SlotBarcodeBinding(slot, Barcode.TryCreateVfd($"VFD2026060100{index + 1:00}").Value!))
            .ToArray();

        var result = await service.StartAsync(operatorSession, station, planVersion, bindings, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        scheduler.CapturedContext.Should().NotBeNull();
        scheduler.CapturedContext!.PlanVersion.Should().Be(planVersion);
        scheduler.CapturedContext.SlotBindings.Should().Equal(bindings);
    }

    private static Station CreateStation(int slotCount)
    {
        var station = new Station(Guid.NewGuid(), "Demo Station");
        for (var number = 1; number <= slotCount; number++)
        {
            station.AddSlot(new StationSlot(
                Guid.NewGuid(),
                new SlotNumber(number),
                new SlotCommunicationConfig(new SerialPortName($"COM{number}"), new ModbusAddress((byte)number), 9600)));
        }

        return station;
    }

    private sealed class FakeProcessPlanRepository : IProcessPlanRepository
    {
        private readonly IReadOnlyList<ProcessPlan> _plans;

        public FakeProcessPlanRepository(IReadOnlyList<ProcessPlan> plans)
        {
            _plans = plans;
        }

        public Task<IReadOnlyList<ProcessPlan>> ListAsync(CancellationToken ct) => Task.FromResult(_plans);

        public Task<IReadOnlyList<ProcessPlanVersion>> ListExecutableVersionsAsync(CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<ProcessPlanVersion>>(
                _plans.SelectMany(plan => plan.Versions).Where(version => version.IsExecutable).ToList());
        }

        public Task<ProcessPlan?> GetAsync(Guid planId, CancellationToken ct)
        {
            return Task.FromResult(_plans.SingleOrDefault(plan => plan.Id == planId));
        }

        public Task SaveAsync(ProcessPlan plan, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class CapturingSlotScheduler : ISlotScheduler
    {
        public StationSessionContext? CapturedContext { get; private set; }

        public Task<StationSessionResult> RunAsync(StationSessionContext context, CancellationToken cancellationToken)
        {
            CapturedContext = context;
            return Task.FromResult(new StationSessionResult(context.SessionId, Conclusion.Pass, []));
        }

        public Task PauseAsync(Guid sessionId) => Task.CompletedTask;

        public Task ResumeAsync(Guid sessionId) => Task.CompletedTask;

        public Task StopSlotAsync(Guid sessionId, Guid slotRunId) => Task.CompletedTask;

        public Task StopSessionAsync(Guid sessionId) => Task.CompletedTask;
    }
}
