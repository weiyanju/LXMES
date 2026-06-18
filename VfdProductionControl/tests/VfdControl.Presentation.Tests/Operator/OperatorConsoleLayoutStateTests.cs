using FluentAssertions;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;
using VfdControl.Application.Operator;
using VfdControl.Application.Traceability;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;
using VfdControl.Domain.Stations;
using VfdControl.Domain.ValueObjects;
using VfdControl.Presentation.Admin;
using VfdControl.Presentation.Operator;

namespace VfdControl.Presentation.Tests.Operator;

public class OperatorConsoleLayoutStateTests
{
    [Fact]
    public async Task Current_action_flags_follow_operator_flow()
    {
        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();

        viewModel.FlowStepDisplay.Should().Be("1 / 7");
        viewModel.IsEmployeeActionVisible.Should().BeTrue();
        viewModel.EmployeeCodeInput = "EMP0001";
        await viewModel.ScanEmployeeCommand.ExecuteAsync(null);

        viewModel.FlowStepDisplay.Should().Be("3 / 7");
        viewModel.IsSlotActionVisible.Should().BeTrue();
        viewModel.IsPlanActionVisible.Should().BeFalse();
    }

    [Fact]
    public void Slot_card_defaults_use_readable_chinese_copy()
    {
        var slot = SlotCardViewModel.CreateDemo(1);

        slot.StatusText.Should().Be("待选择");
        slot.BarcodeDisplay.Should().Be("未绑定条码");
    }

    [Fact]
    public async Task Selecting_plan_option_from_combo_advances_without_extra_click()
    {
        var viewModel = CreateViewModel(planCount: 2);
        await viewModel.InitializeAsync();

        viewModel.EmployeeCodeInput = "EMP0001";
        await viewModel.ScanEmployeeCommand.ExecuteAsync(null);

        viewModel.State.Should().Be(OperatorConsoleState.SelectingPlan);
        viewModel.SelectedPlanOption.Should().BeNull();

        viewModel.SelectedPlanOption = viewModel.AvailablePlans[1];

        viewModel.State.Should().Be(OperatorConsoleState.SelectingSlots);
        viewModel.SelectedPlanDisplay.Should().Contain("演示方案 2");
    }

    private static OperatorConsoleViewModel CreateViewModel(int planCount = 1)
    {
        var station = new Station(Guid.NewGuid(), "演示工位");
        station.AddSlot(new StationSlot(
            Guid.NewGuid(),
            new SlotNumber(1),
            new SlotCommunicationConfig(new SerialPortName("COM1"), new ModbusAddress(1), 9600)));
        var plans = Enumerable.Range(1, planCount)
            .Select(index =>
            {
                var plan = new ProcessPlan(Guid.NewGuid(), $"VFD 生产测试演示方案 {index}");
                plan.AddVersion(new ProcessPlanVersion(Guid.NewGuid(), index, isExecutable: true));
                return plan;
            })
            .ToList();

        return new OperatorConsoleViewModel(
            new OperatorSessionService(),
            new PlanSelectionService(new FakeProcessPlanRepository(plans)),
            new SlotSelectionService(),
            new ProductionRunService(new NoOpSlotScheduler()),
            new FakeStationRepository([station]),
            new EmptyTraceRepository(),
            new StationConfigurationChangeNotifier());
    }

    private sealed class FakeStationRepository : IStationRepository
    {
        private readonly IReadOnlyList<Station> _stations;

        public FakeStationRepository(IReadOnlyList<Station> stations)
        {
            _stations = stations;
        }

        public Task<IReadOnlyList<Station>> ListAsync(CancellationToken ct) => Task.FromResult(_stations);

        public Task<Station?> GetAsync(Guid stationId, CancellationToken ct)
        {
            return Task.FromResult(_stations.SingleOrDefault(station => station.Id == stationId));
        }

        public Task SaveAsync(Station station, CancellationToken ct) => Task.CompletedTask;
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

    private sealed class NoOpSlotScheduler : ISlotScheduler
    {
        public Task<StationSessionResult> RunAsync(StationSessionContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(new StationSessionResult(context.SessionId, Conclusion.Pass, []));
        }

        public Task PauseAsync(Guid sessionId) => Task.CompletedTask;

        public Task ResumeAsync(Guid sessionId) => Task.CompletedTask;

        public Task StopSlotAsync(Guid sessionId, Guid slotRunId) => Task.CompletedTask;

        public Task StopSessionAsync(Guid sessionId) => Task.CompletedTask;
    }

    private sealed class EmptyTraceRepository : ITraceRepository
    {
        public Task SaveSessionStartedAsync(StationSessionSnapshot session, CancellationToken ct) => Task.CompletedTask;

        public Task SaveDeviceRunAsync(DeviceRunSnapshot run, CancellationToken ct) => Task.CompletedTask;

        public Task SaveStepRunAsync(StepRunSnapshot step, CancellationToken ct) => Task.CompletedTask;

        public Task SaveMeasurementResultAsync(MeasurementTrace measurement, CancellationToken ct) => Task.CompletedTask;

        public Task SaveComparisonResultAsync(ComparisonTrace comparison, CancellationToken ct) => Task.CompletedTask;

        public Task SaveCommandTraceAsync(CommandTraceSnapshot trace, CancellationToken ct) => Task.CompletedTask;

        public Task<IReadOnlyList<StationSessionSummary>> QuerySessionsAsync(TraceabilitySessionQuery query, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<StationSessionSummary>>([]);
        }

        public Task<IReadOnlyList<DeviceRunSummary>> QueryDeviceRunsAsync(DeviceRunQuery query, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<DeviceRunSummary>>([]);
        }

        public Task<DeviceRunTrace?> GetDeviceRunTraceAsync(Guid deviceRunId, CancellationToken ct)
        {
            return Task.FromResult<DeviceRunTrace?>(null);
        }
    }
}
