using FluentAssertions;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Admin;
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

public class OperatorConsoleViewModelTests
{
    [Fact]
    public void Slot_step_progress_keeps_completed_result_detail_on_the_step_row()
    {
        var slot = SlotCardViewModel.CreateDemo(1);
        var runId = Guid.NewGuid();

        slot.ApplyStepProgress(new SlotStepProgressSnapshot(
            slot.Slot.Id,
            runId,
            2,
            "Read VFD voltage",
            Conclusion.Pass,
            "220.3 V / range 210.0 ~ 230.0"));

        var row = slot.StepRows.Single();
        row.ConclusionText.Should().Be("\u901A\u8FC7");
        row.DetailText.Should().Be("220.3 V / range 210.0 ~ 230.0");
    }

    [Fact]
    public void Slot_card_displays_delay_countdown_progress()
    {
        var slot = SlotCardViewModel.CreateDemo(1);
        var runId = Guid.NewGuid();

        slot.ApplyStepProgress(new SlotStepProgressSnapshot(
            slot.Slot.Id,
            runId,
            2,
            "稳定等待",
            Conclusion.None,
            "等待中",
            TimeSpan.FromMilliseconds(9500),
            IsRunning: true));

        slot.StepRows.Single().ConclusionText.Should().Be("等待中 9.5s");
        slot.LatestCommandText.Should().Contain("等待中 9.5s");
    }

    [Fact]
    public async Task Employee_scan_auto_selects_single_executable_plan()
    {
        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();

        viewModel.CurrentPrompt.Should().Be("扫描员工码开始生产会话。");
        viewModel.StatusMessage.Should().Be("模拟模式就绪。");
        viewModel.EmployeeCodeInput = "EMP0001";

        await viewModel.ScanEmployeeCommand.ExecuteAsync(null);

        viewModel.State.Should().Be(OperatorConsoleState.SelectingSlots);
        viewModel.EmployeeDisplay.Should().Be("EMP0001");
        viewModel.SelectedPlanDisplay.Should().Be("测试方案 - v1");
        viewModel.AvailablePlans.Should().ContainSingle();
    }

    [Fact]
    public async Task Scanner_employee_code_starts_operator_session()
    {
        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();

        await viewModel.ApplyScannedTextAsync("emp0001");

        viewModel.State.Should().Be(OperatorConsoleState.SelectingSlots);
        viewModel.EmployeeCodeInput.Should().Be("EMP0001");
        viewModel.EmployeeDisplay.Should().Be("EMP0001");
    }

    [Fact]
    public async Task Plan_selection_enables_slot_selection()
    {
        var viewModel = CreateViewModel();
        await StartEmployeeSessionAsync(viewModel);

        viewModel.SelectPlanCommand.Execute(viewModel.AvailablePlans[0]);

        viewModel.State.Should().Be(OperatorConsoleState.SelectingSlots);
        viewModel.SelectedPlanDisplay.Should().Be("测试方案 - v1");
        viewModel.SlotCards.Should().HaveCount(4);
    }

    [Fact]
    public async Task Operator_slot_board_refreshes_when_station_configuration_changes()
    {
        var station = CreateStation(1);
        var repository = new FakeStationRepository([station]);
        var notifier = new StationConfigurationChangeNotifier();
        var viewModel = CreateViewModel(stationRepository: repository, changeNotifier: notifier);
        var adminViewModel = new StationConfigViewModel(
            new StationConfigurationService(repository),
            new FakeSerialPortCatalog(["COM1", "COM2"]),
            notifier);

        await viewModel.InitializeAsync();
        await adminViewModel.LoadAsync();

        await adminViewModel.AddSlotCommand.ExecuteAsync(null);

        viewModel.SlotCards.Should().HaveCount(2);
        viewModel.SlotCards[1].PortName.Should().Be("COM2");
    }

    [Fact]
    public async Task Operator_slot_board_uses_editable_slot_display_name()
    {
        var station = CreateStation(1);
        station.Slots[0].UpdateDisplayName("\u8001\u5316\u4F4D A");
        var viewModel = CreateViewModel(stationRepository: new FakeStationRepository([station]));

        await viewModel.InitializeAsync();

        viewModel.SlotCards.Should().ContainSingle();
        viewModel.SlotCards[0].Title.Should().Be("\u8001\u5316\u4F4D A");
    }

    [Fact]
    public async Task Selected_slots_define_barcode_prompts()
    {
        var viewModel = CreateViewModel();
        await SelectPlanAsync(viewModel);

        viewModel.ToggleSlotCommand.Execute(viewModel.SlotCards[2]);
        viewModel.ToggleSlotCommand.Execute(viewModel.SlotCards[0]);
        viewModel.ConfirmSlotsCommand.Execute(null);

        viewModel.State.Should().Be(OperatorConsoleState.ScanningBarcodes);
        viewModel.CurrentPrompt.Should().Be("请扫描 3 号槽位的 VFD 条码。");
    }

    [Fact]
    public async Task Confirmed_slots_show_plan_steps_before_run_starts()
    {
        var viewModel = CreateViewModel();
        await SelectPlanAsync(viewModel);

        viewModel.ToggleSlotCommand.Execute(viewModel.SlotCards[2]);
        viewModel.ConfirmSlotsCommand.Execute(null);

        viewModel.SlotCards[2].StepRows.Should().HaveCount(3);
        viewModel.SlotCards[2].StepRows[0].DisplayName.Should().Be("1. 变频器安装确认");
        viewModel.SlotCards[2].StepRows[0].ConclusionText.Should().Be("待执行");
        viewModel.SlotCards[2].CurrentComparisonText.Should().Be("比对信息：待执行");
    }

    [Fact]
    public async Task Change_plan_returns_to_plan_selection_and_clears_pending_slot_state()
    {
        var viewModel = CreateViewModel();
        await SelectSlotsAsync(viewModel);
        viewModel.BarcodeInput = "VFD202606010001";
        viewModel.ScanBarcodeCommand.Execute(null);

        await viewModel.ChangePlanCommand.ExecuteAsync(null);

        viewModel.State.Should().Be(OperatorConsoleState.SelectingPlan);
        viewModel.IsChangePlanVisible.Should().BeFalse();
        viewModel.SelectedPlanOption.Should().BeNull();
        viewModel.SelectedPlanDisplay.Should().Be("\u672A\u9009\u62E9\u65B9\u6848");
        viewModel.BarcodeInput.Should().Be("");
        viewModel.SlotCards[2].IsSelected.Should().BeFalse();
        viewModel.SlotCards[2].Barcode.Should().Be("");
        viewModel.SlotCards[2].StepRows.Should().BeEmpty();
    }

    [Fact]
    public async Task Change_plan_refreshes_executable_plan_options()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "Refreshable plan");
        var version = new ProcessPlanVersion(Guid.NewGuid(), 1, isExecutable: true);
        AddDemoSteps(version);
        plan.AddVersion(version);
        var repository = new FakeProcessPlanRepository([plan]);
        var viewModel = CreateViewModel(processPlanRepository: repository);
        await SelectSlotsAsync(viewModel);

        var newVersion = new ProcessPlanVersion(Guid.NewGuid(), 2, isExecutable: true);
        AddDemoSteps(newVersion);
        plan.AddVersion(newVersion);

        await viewModel.ChangePlanCommand.ExecuteAsync(null);

        viewModel.State.Should().Be(OperatorConsoleState.SelectingPlan);
        viewModel.AvailablePlans.Select(item => item.DisplayName)
            .Should()
            .Contain("Refreshable plan - v2");
    }

    [Fact]
    public async Task Barcodes_bind_to_slot_cards_in_order()
    {
        var viewModel = CreateViewModel();
        await SelectSlotsAsync(viewModel);

        viewModel.BarcodeInput = "VFD202606010001";
        viewModel.ScanBarcodeCommand.Execute(null);
        viewModel.BarcodeInput = "VFD202606010002";
        viewModel.ScanBarcodeCommand.Execute(null);

        viewModel.State.Should().Be(OperatorConsoleState.ConfirmingStart);
        viewModel.SlotCards[2].Barcode.Should().Be("VFD202606010001");
        viewModel.SlotCards[0].Barcode.Should().Be("VFD202606010002");
    }

    [Fact]
    public async Task Scanner_vfd_barcode_binds_current_slot_when_waiting_for_barcodes()
    {
        var viewModel = CreateViewModel();
        await SelectSlotsAsync(viewModel);

        await viewModel.ApplyScannedTextAsync("vfd202606010001");

        viewModel.State.Should().Be(OperatorConsoleState.ScanningBarcodes);
        viewModel.SlotCards[2].Barcode.Should().Be("VFD202606010001");
        viewModel.BarcodeInput.Should().Be("");
    }

    [Fact]
    public async Task Start_command_calls_production_service()
    {
        var scheduler = new CapturingSlotScheduler();
        var viewModel = CreateViewModel(scheduler);
        await BindBarcodesAsync(viewModel);

        await viewModel.StartRunCommand.ExecuteAsync(null);

        scheduler.RunCount.Should().Be(1);
        viewModel.State.Should().Be(OperatorConsoleState.Completed);
        viewModel.DetailRows.Should().NotBeEmpty();
        viewModel.DetailRows[0].SlotName.Should().Be("3 号槽位");
    }

    [Fact]
    public async Task Completed_run_maps_step_results_to_selected_slot_cards()
    {
        var scheduler = new CapturingSlotScheduler(
            [
                new StepRunSnapshot(Guid.NewGuid(), Guid.NewGuid(), 1, "变频器安装确认", Conclusion.Pass),
                new StepRunSnapshot(Guid.NewGuid(), Guid.NewGuid(), 2, "电压上电", Conclusion.Pass),
                new StepRunSnapshot(Guid.NewGuid(), Guid.NewGuid(), 3, "测量比对", Conclusion.Fail)
            ],
            Conclusion.Fail);
        var viewModel = CreateViewModel(scheduler);
        await BindBarcodesAsync(viewModel);

        await viewModel.StartRunCommand.ExecuteAsync(null);

        var firstSelectedCard = viewModel.SlotCards[2];
        firstSelectedCard.StepRows.Should().HaveCount(3);
        firstSelectedCard.StepRows[0].DisplayName.Should().Be("1. 变频器安装确认");
        firstSelectedCard.StepRows[0].ConclusionText.Should().Be("通过");
        firstSelectedCard.StepRows[2].ConclusionText.Should().Be("失败");
        firstSelectedCard.CurrentComparisonText.Should().Be("测量比对：失败");
        firstSelectedCard.FinalConclusionText.Should().Be("最终结果：失败");
    }

    [Fact]
    public async Task Slot_progress_callback_updates_preloaded_step_rows()
    {
        var scheduler = new CapturingSlotScheduler(
            [
                new StepRunSnapshot(Guid.NewGuid(), Guid.NewGuid(), 1, "变频器安装确认", Conclusion.Pass),
                new StepRunSnapshot(Guid.NewGuid(), Guid.NewGuid(), 2, "电压上电", Conclusion.Pass)
            ],
            Conclusion.Pass,
            emitProgress: true);
        var viewModel = CreateViewModel(scheduler);
        await BindBarcodesAsync(viewModel);

        await viewModel.StartRunCommand.ExecuteAsync(null);

        var firstSelectedCard = viewModel.SlotCards[2];
        firstSelectedCard.StepRows[0].ConclusionText.Should().Be("通过");
        firstSelectedCard.LatestCommandText.Should().Contain("电压上电");
    }


    [Fact]
    public async Task Completed_run_updates_console_counters_and_instruction_log()
    {
        var traceRepository = new FakeTraceRepository();
        var scheduler = new CapturingSlotScheduler(
            [
                new StepRunSnapshot(Guid.NewGuid(), Guid.NewGuid(), 1, "变频器安装确认", Conclusion.Pass),
                new StepRunSnapshot(Guid.NewGuid(), Guid.NewGuid(), 2, "电压上电", Conclusion.Pass)
            ],
            Conclusion.Pass,
            traceRepository);
        var viewModel = CreateViewModel(scheduler, traceRepository);
        await BindBarcodesAsync(viewModel);

        await viewModel.StartRunCommand.ExecuteAsync(null);

        viewModel.PassCount.Should().Be(2);
        viewModel.FailCount.Should().Be(0);
        viewModel.InstructionRows.Should().NotBeEmpty();
        viewModel.InstructionRows[0].Command.Should().Be("3 号槽位");
        viewModel.InstructionRows[0].Result.Should().Contain("通过");
    }

    [Fact]
    public async Task Completed_run_uses_trace_commands_measurements_and_comparisons_on_console()
    {
        var traceRepository = new FakeTraceRepository();
        var scheduler = new CapturingSlotScheduler(
            [
                new StepRunSnapshot(Guid.NewGuid(), Guid.NewGuid(), 1, "读取电压", Conclusion.Pass),
                new StepRunSnapshot(Guid.NewGuid(), Guid.NewGuid(), 2, "测量比对", Conclusion.Fail)
            ],
            Conclusion.Fail,
            traceRepository,
            includeDetailedTrace: true);
        var viewModel = CreateViewModel(scheduler, traceRepository);
        await BindBarcodesAsync(viewModel);

        await viewModel.StartRunCommand.ExecuteAsync(null);

        var firstSelectedCard = viewModel.SlotCards[2];
        firstSelectedCard.CurrentComparisonText.Should().Contain("目标 220.00V");
        firstSelectedCard.CurrentComparisonText.Should().Contain("实测 219.00V");
        firstSelectedCard.LatestCommandText.Should().Be("指令：ReadVoltage 失败");
        viewModel.InstructionRows.Should().Contain(row =>
            row.Command == "ReadVoltage" &&
            row.Result.Contains("失败") &&
            row.Send == "{\"target\":\"voltage\"}" &&
            row.Receive == "{\"value\":219.00}");
    }

    [Fact]
    public async Task Completed_run_keeps_step_rule_message_on_step_row_when_trace_is_loaded()
    {
        var traceRepository = new FakeTraceRepository();
        var scheduler = new CapturingSlotScheduler(
            [
                new StepRunSnapshot(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    1,
                    "Read VFD voltage",
                    Conclusion.Pass,
                    "Value 220.3 is within range.")
            ],
            Conclusion.Pass,
            traceRepository);
        var viewModel = CreateViewModel(scheduler, traceRepository);
        await BindBarcodesAsync(viewModel);

        await viewModel.StartRunCommand.ExecuteAsync(null);

        viewModel.SlotCards[2].StepRows[0].DetailText.Should().Be("Value 220.3 is within range.");
    }

    [Fact]
    public async Task Running_timer_updates_elapsed_display()
    {
        var scheduler = new CapturingSlotScheduler([], Conclusion.Pass, delay: TimeSpan.FromSeconds(1.1));
        var viewModel = CreateViewModel(scheduler);
        await BindBarcodesAsync(viewModel);

        await viewModel.StartRunCommand.ExecuteAsync(null);

        viewModel.ElapsedDisplay.Should().NotBe("00:00:00");
    }

    [Theory]
    [InlineData(Conclusion.Pass, "#DFF5E9", "#EFFAF4")]
    [InlineData(Conclusion.Fail, "#F9DEDE", "#FDF1F1")]
    [InlineData(Conclusion.Warning, "#FFF2CC", "#FFF9E8")]
    public void Slot_card_color_maps_conclusions(Conclusion conclusion, string expectedStatusColor, string expectedCardColor)
    {
        var card = SlotCardViewModel.CreateDemo(1);

        card.ApplyConclusion(conclusion);

        card.StatusColor.Should().Be(expectedStatusColor);
        card.CardBackgroundColor.Should().Be(expectedCardColor);
    }

    [Fact]
    public void Running_slot_card_uses_running_status_color()
    {
        var card = SlotCardViewModel.CreateDemo(1);

        card.MarkRunning();

        card.StatusColor.Should().Be("#D8E8FF");
        card.StatusText.Should().Be("运行中");
    }

    private static async Task StartEmployeeSessionAsync(OperatorConsoleViewModel viewModel)
    {
        await viewModel.InitializeAsync();
        viewModel.EmployeeCodeInput = "EMP0001";
        await viewModel.ScanEmployeeCommand.ExecuteAsync(null);
    }

    private static async Task SelectPlanAsync(OperatorConsoleViewModel viewModel)
    {
        await StartEmployeeSessionAsync(viewModel);
        viewModel.SelectPlanCommand.Execute(viewModel.AvailablePlans[0]);
    }

    private static async Task SelectSlotsAsync(OperatorConsoleViewModel viewModel)
    {
        await SelectPlanAsync(viewModel);
        viewModel.ToggleSlotCommand.Execute(viewModel.SlotCards[2]);
        viewModel.ToggleSlotCommand.Execute(viewModel.SlotCards[0]);
        viewModel.ConfirmSlotsCommand.Execute(null);
    }

    private static async Task BindBarcodesAsync(OperatorConsoleViewModel viewModel)
    {
        await SelectSlotsAsync(viewModel);
        viewModel.BarcodeInput = "VFD202606010001";
        viewModel.ScanBarcodeCommand.Execute(null);
        viewModel.BarcodeInput = "VFD202606010002";
        viewModel.ScanBarcodeCommand.Execute(null);
    }

    private static OperatorConsoleViewModel CreateViewModel(
        CapturingSlotScheduler? scheduler = null,
        FakeTraceRepository? traceRepository = null,
        IStationRepository? stationRepository = null,
        StationConfigurationChangeNotifier? changeNotifier = null,
        IProcessPlanRepository? processPlanRepository = null)
    {
        var station = CreateStation(4);
        var plan = new ProcessPlan(Guid.NewGuid(), "测试方案");
        var version = new ProcessPlanVersion(Guid.NewGuid(), 1, isExecutable: true);
        AddDemoSteps(version);
        plan.AddVersion(version);
        traceRepository ??= new FakeTraceRepository();
        scheduler ??= new CapturingSlotScheduler();

        return new OperatorConsoleViewModel(
            new OperatorSessionService(),
            new PlanSelectionService(processPlanRepository ?? new FakeProcessPlanRepository([plan])),
            new SlotSelectionService(),
            new ProductionRunService(scheduler),
            stationRepository ?? new FakeStationRepository([station]),
            traceRepository,
            changeNotifier ?? new StationConfigurationChangeNotifier());
    }

    private static void AddDemoSteps(ProcessPlanVersion version)
    {
        version.AddStep(CreateStep(1, "变频器安装确认", "Start"));
        version.AddStep(CreateStep(2, "电压上电", "ReadMeasurement"));
        version.AddStep(CreateStep(3, "测量比对", "CompareMeasurement"));
    }

    private static ProcessStep CreateStep(int sequence, string name, string commandType)
    {
        return new ProcessStep(
            Guid.NewGuid(),
            sequence,
            name,
            new StepCommand(commandType, name),
            new StepFailurePolicy(FailureAction.ContinueAndMarkFail),
            affectsFinalConclusion: true);
    }

    private static Station CreateStation(int slotCount)
    {
        var station = new Station(Guid.NewGuid(), "演示工位");
        for (var number = 1; number <= slotCount; number++)
        {
            station.AddSlot(new StationSlot(
                Guid.NewGuid(),
                new SlotNumber(number),
                new SlotCommunicationConfig(new SerialPortName($"COM{number}"), new ModbusAddress((byte)number), 9600)));
        }

        return station;
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

    private sealed class FakeSerialPortCatalog : ISerialPortCatalog
    {
        private readonly IReadOnlyList<string> _ports;

        public FakeSerialPortCatalog(IReadOnlyList<string> ports)
        {
            _ports = ports;
        }

        public IReadOnlyList<string> ListPortNames() => _ports;
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
        private readonly IReadOnlyList<StepRunSnapshot> _steps;
        private readonly Conclusion _conclusion;
        private readonly FakeTraceRepository? _traceRepository;
        private readonly bool _includeDetailedTrace;
        private readonly bool _emitProgress;
        private readonly TimeSpan _delay;

        public CapturingSlotScheduler()
            : this([], Conclusion.Pass)
        {
        }

        public CapturingSlotScheduler(
            IReadOnlyList<StepRunSnapshot> steps,
            Conclusion conclusion,
            FakeTraceRepository? traceRepository = null,
            bool includeDetailedTrace = false,
            bool emitProgress = false,
            TimeSpan? delay = null)
        {
            _steps = steps;
            _conclusion = conclusion;
            _traceRepository = traceRepository;
            _includeDetailedTrace = includeDetailedTrace;
            _emitProgress = emitProgress;
            _delay = delay ?? TimeSpan.Zero;
        }

        public int RunCount { get; private set; }

        public async Task<StationSessionResult> RunAsync(StationSessionContext context, CancellationToken cancellationToken)
        {
            RunCount++;
            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, cancellationToken);
            }

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

            var runs = context.SlotBindings
                .Select(binding =>
                {
                    var deviceRunId = Guid.NewGuid();
                    var steps = _steps.Count == 0
                        ? [new StepRunSnapshot(Guid.NewGuid(), deviceRunId, 1, "变频器安装确认", _conclusion)]
                        : _steps.Select(step => step with { DeviceRunId = deviceRunId }).ToList();
                    if (_emitProgress)
                    {
                        foreach (var step in steps)
                        {
                            context.ProgressHandler?.Invoke(new SlotStepProgressSnapshot(
                                binding.Slot.Id,
                                deviceRunId,
                                step.Sequence,
                                step.StepName,
                                step.Conclusion,
                                $"{step.StepName} {step.Conclusion}"));
                        }
                    }

                    SaveTrace(context, binding, deviceRunId, steps, cancellationToken).GetAwaiter().GetResult();
                    return new DeviceRunResult(deviceRunId, _conclusion, steps);
                })
                .ToList();

            return new StationSessionResult(context.SessionId, _conclusion, runs);
        }

        private async Task SaveTrace(
            StationSessionContext context,
            SlotBarcodeBinding binding,
            Guid deviceRunId,
            IReadOnlyList<StepRunSnapshot> steps,
            CancellationToken cancellationToken)
        {
            if (_traceRepository is null)
            {
                return;
            }

            await _traceRepository.SaveDeviceRunAsync(
                new DeviceRunSnapshot(
                    deviceRunId,
                    context.SessionId,
                    binding.Slot.Id,
                    binding.Barcode.Value,
                    _conclusion),
                cancellationToken);

            foreach (var step in steps)
            {
                await _traceRepository.SaveStepRunAsync(step, cancellationToken);
            }

            if (!_includeDetailedTrace || steps.Count == 0)
            {
                return;
            }

            var measurementStep = steps[0];
            await _traceRepository.SaveMeasurementResultAsync(
                new MeasurementTrace(measurementStep.StepRunId, "voltage", 219.00, "V", MeasurementSource.Vfd),
                cancellationToken);

            var compareStep = steps[^1];
            await _traceRepository.SaveComparisonResultAsync(
                new ComparisonTrace(compareStep.StepRunId, "目标 220.00V", "实测 219.00V", Conclusion.Fail, "误差 1.00V，失败"),
                cancellationToken);
            await _traceRepository.SaveCommandTraceAsync(
                new CommandTraceSnapshot(
                    Guid.NewGuid(),
                    compareStep.StepRunId,
                    binding.Slot.Id,
                    "ReadVoltage",
                    "{\"target\":\"voltage\"}",
                    "{\"value\":219.00}",
                    false,
                    DateTimeOffset.UtcNow),
                cancellationToken);
        }

        public Task PauseAsync(Guid sessionId) => Task.CompletedTask;

        public Task ResumeAsync(Guid sessionId) => Task.CompletedTask;

        public Task StopSlotAsync(Guid sessionId, Guid slotRunId) => Task.CompletedTask;

        public Task StopSessionAsync(Guid sessionId) => Task.CompletedTask;
    }

    private sealed class FakeTraceRepository : ITraceRepository
    {
        private readonly List<StationSessionSnapshot> _sessions = [];
        private readonly List<DeviceRunSnapshot> _deviceRuns = [];
        private readonly List<StepRunSnapshot> _steps = [];
        private readonly List<MeasurementTrace> _measurements = [];
        private readonly List<ComparisonTrace> _comparisons = [];
        private readonly List<CommandTraceSnapshot> _commands = [];

        public Task SaveSessionStartedAsync(StationSessionSnapshot session, CancellationToken ct)
        {
            _sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task SaveDeviceRunAsync(DeviceRunSnapshot run, CancellationToken ct)
        {
            _deviceRuns.Add(run);
            return Task.CompletedTask;
        }

        public Task SaveStepRunAsync(StepRunSnapshot step, CancellationToken ct)
        {
            _steps.Add(step);
            return Task.CompletedTask;
        }

        public Task SaveMeasurementResultAsync(MeasurementTrace measurement, CancellationToken ct)
        {
            _measurements.Add(measurement);
            return Task.CompletedTask;
        }

        public Task SaveComparisonResultAsync(ComparisonTrace comparison, CancellationToken ct)
        {
            _comparisons.Add(comparison);
            return Task.CompletedTask;
        }

        public Task SaveCommandTraceAsync(CommandTraceSnapshot trace, CancellationToken ct)
        {
            _commands.Add(trace);
            return Task.CompletedTask;
        }

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
            var run = _deviceRuns.SingleOrDefault(run => run.DeviceRunId == deviceRunId);
            if (run is null)
            {
                return Task.FromResult<DeviceRunTrace?>(null);
            }

            var steps = _steps
                .Where(step => step.DeviceRunId == deviceRunId)
                .OrderBy(step => step.Sequence)
                .Select(step => new StepRunTrace(
                    step.StepRunId,
                    step.DeviceRunId,
                    step.Sequence,
                    step.StepName,
                    step.Conclusion,
                    _measurements.Where(measurement => measurement.StepRunId == step.StepRunId).ToList(),
                    _comparisons.Where(comparison => comparison.StepRunId == step.StepRunId).ToList(),
                    _commands.Where(command => command.StepRunId == step.StepRunId).ToList(),
                    step.Message))
                .ToList();

            return Task.FromResult<DeviceRunTrace?>(new DeviceRunTrace(
                run.DeviceRunId,
                run.SessionId,
                run.SlotId,
                run.Barcode,
                run.Conclusion,
                _sessions.SingleOrDefault(session => session.SessionId == run.SessionId)?.StartedAt ?? DateTimeOffset.UtcNow,
                steps));
        }
    }
}
