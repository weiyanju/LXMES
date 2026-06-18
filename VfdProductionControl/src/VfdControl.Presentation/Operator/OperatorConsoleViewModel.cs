using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;
using VfdControl.Application.Operator;
using VfdControl.Application.Traceability;
using VfdControl.Presentation.Admin;
using VfdControl.Domain.Plans;
using VfdControl.Domain.Stations;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Presentation.Operator;

public sealed partial class OperatorConsoleViewModel : ObservableObject
{
    private readonly OperatorSessionService _operatorSessionService;
    private readonly PlanSelectionService _planSelectionService;
    private readonly SlotSelectionService _slotSelectionService;
    private readonly ProductionRunService _productionRunService;
    private readonly IStationRepository _stationRepository;
    private readonly ITraceRepository _traceRepository;
    private readonly StationConfigurationChangeNotifier _stationConfigurationChangeNotifier;
    private readonly List<string> _scannedBarcodes = [];
    private readonly List<SlotCardViewModel> _selectedSlotOrder = [];

    private OperatorSession? _operatorSession;
    private Station? _station;
    private ProcessPlanVersion? _selectedPlan;
    private SlotScanQueue? _scanQueue;
    private SlotBarcodeBindingSet? _bindingSet;
    private CancellationTokenSource? _elapsedTimerCts;
    private DateTimeOffset? _runStartedAt;
    private bool _isInitialized;

    [ObservableProperty]
    private OperatorConsoleState _state = OperatorConsoleState.WaitingEmployeeCode;

    [ObservableProperty]
    private string _employeeCodeInput = "";

    [ObservableProperty]
    private string _employeeDisplay = "未扫描";

    [ObservableProperty]
    private string _selectedPlanDisplay = "未选择方案";

    [ObservableProperty]
    private string _stationDisplay = "未加载工位";

    [ObservableProperty]
    private string _barcodeInput = "";

    [ObservableProperty]
    private string _currentPrompt = "扫描员工码开始生产会话。";

    [ObservableProperty]
    private string _statusMessage = "模拟模式就绪。";

    [ObservableProperty]
    private int _passCount;

    [ObservableProperty]
    private int _failCount;

    [ObservableProperty]
    private int _warningCount;

    [ObservableProperty]
    private string _elapsedDisplay = "00:00:00";

    [ObservableProperty]
    private PlanOptionViewModel? _selectedPlanOption;

    public OperatorConsoleViewModel(
        OperatorSessionService operatorSessionService,
        PlanSelectionService planSelectionService,
        SlotSelectionService slotSelectionService,
        ProductionRunService productionRunService,
        IStationRepository stationRepository,
        ITraceRepository traceRepository,
        StationConfigurationChangeNotifier stationConfigurationChangeNotifier)
    {
        _operatorSessionService = operatorSessionService;
        _planSelectionService = planSelectionService;
        _slotSelectionService = slotSelectionService;
        _productionRunService = productionRunService;
        _stationRepository = stationRepository;
        _traceRepository = traceRepository;
        _stationConfigurationChangeNotifier = stationConfigurationChangeNotifier;
        _stationConfigurationChangeNotifier.Changed += RefreshSlotConfigurationAsync;
    }

    public ObservableCollection<PlanOptionViewModel> AvailablePlans { get; } = [];

    public ObservableCollection<SlotCardViewModel> SlotCards { get; } = [];

    public ObservableCollection<DeviceRunTableRowViewModel> DetailRows { get; } = [];

    public ObservableCollection<InstructionLogRowViewModel> InstructionRows { get; } = [];

    public int SlotBoardColumnCount => Math.Clamp(SlotCards.Count, 1, 4);

    public string FlowStepDisplay => State switch
    {
        OperatorConsoleState.WaitingEmployeeCode => "1 / 7",
        OperatorConsoleState.SelectingPlan => "2 / 7",
        OperatorConsoleState.SelectingSlots => "3 / 7",
        OperatorConsoleState.ScanningBarcodes => "4 / 7",
        OperatorConsoleState.ConfirmingStart => "5 / 7",
        OperatorConsoleState.Running => "6 / 7",
        OperatorConsoleState.Completed => "7 / 7",
        _ => "1 / 7"
    };

    public string FlowStepName => State switch
    {
        OperatorConsoleState.WaitingEmployeeCode => "扫描员工码",
        OperatorConsoleState.SelectingPlan => "选择方案",
        OperatorConsoleState.SelectingSlots => "选择槽位",
        OperatorConsoleState.ScanningBarcodes => "绑定条码",
        OperatorConsoleState.ConfirmingStart => "确认启动",
        OperatorConsoleState.Running => "运行中",
        OperatorConsoleState.Completed => "已完成",
        _ => "准备"
    };

    public bool IsEmployeeActionVisible => State == OperatorConsoleState.WaitingEmployeeCode;

    public bool IsPlanActionVisible => State == OperatorConsoleState.SelectingPlan;

    public bool IsSlotActionVisible => State == OperatorConsoleState.SelectingSlots;

    public bool IsBarcodeActionVisible => State == OperatorConsoleState.ScanningBarcodes;

    public bool IsStartActionVisible => State == OperatorConsoleState.ConfirmingStart;

    public bool IsRunningActionVisible => State == OperatorConsoleState.Running;

    public bool IsCompletedActionVisible => State == OperatorConsoleState.Completed;

    public bool IsChangePlanVisible => State is OperatorConsoleState.SelectingSlots
        or OperatorConsoleState.ScanningBarcodes
        or OperatorConsoleState.ConfirmingStart;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        var stations = await _stationRepository.ListAsync(cancellationToken);
        _station = stations.FirstOrDefault();
        StationDisplay = _station?.Name ?? "未加载工位";

        await RefreshAvailablePlansAsync(cancellationToken);

        SelectedPlanOption = AvailablePlans.Count == 1 ? AvailablePlans[0] : null;

        SlotCards.Clear();
        if (_station is not null)
        {
            foreach (var slot in _station.Slots.OrderBy(slot => slot.Number.Value))
            {
                SlotCards.Add(new SlotCardViewModel(slot));
            }
        }

        OnPropertyChanged(nameof(SlotBoardColumnCount));
    }

    public async Task RefreshSlotConfigurationAsync(CancellationToken cancellationToken = default)
    {
        if (State == OperatorConsoleState.Running)
        {
            StatusMessage = "\u69FD\u4F4D\u914D\u7F6E\u5DF2\u53D8\u66F4\uFF0C\u5F53\u524D\u6D4B\u8BD5\u7ED3\u675F\u540E\u5237\u65B0\u3002";
            return;
        }

        var stations = await _stationRepository.ListAsync(cancellationToken);
        _station = stations.FirstOrDefault();
        StationDisplay = _station?.Name ?? "\u672A\u52A0\u8F7D\u5DE5\u4F4D";
        _selectedSlotOrder.Clear();

        SlotCards.Clear();
        if (_station is not null)
        {
            foreach (var slot in _station.Slots.OrderBy(slot => slot.Number.Value))
            {
                SlotCards.Add(new SlotCardViewModel(slot));
            }
        }

        OnPropertyChanged(nameof(SlotBoardColumnCount));
    }

    public async Task ApplyScannedTextAsync(string scannedText)
    {
        var normalized = scannedText.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (State == OperatorConsoleState.WaitingEmployeeCode)
        {
            if (!EmployeeCode.TryCreate(normalized).IsSuccess)
            {
                StatusMessage = "请扫描有效员工码。";
                return;
            }

            EmployeeCodeInput = normalized;
            await ScanEmployeeAsync();
            return;
        }

        if (State == OperatorConsoleState.ScanningBarcodes)
        {
            if (!Barcode.TryCreateVfd(normalized).IsSuccess)
            {
                StatusMessage = "请扫描有效 VFD 条码。";
                return;
            }

            BarcodeInput = normalized;
            ScanBarcode();
            return;
        }

        StatusMessage = "当前阶段不接收扫码输入。";
    }

    [RelayCommand]
    private Task ScanEmployeeAsync()
    {
        var result = _operatorSessionService.StartSession(EmployeeCodeInput);
        if (!result.IsSuccess || result.Value is null)
        {
            StatusMessage = string.IsNullOrWhiteSpace(result.Message) ? "员工码被拒绝。" : result.Message;
            return Task.CompletedTask;
        }

        _operatorSession = result.Value;
        EmployeeDisplay = result.Value.EmployeeCode.Value;
        State = OperatorConsoleState.SelectingPlan;
        CurrentPrompt = "选择可执行测试方案。";
        StatusMessage = "员工会话已开始。";
        if (AvailablePlans.Count == 1)
        {
            SelectPlan(AvailablePlans[0]);
            StatusMessage = "已自动选择唯一可执行方案。";
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void SelectPlan(PlanOptionViewModel? plan)
    {
        if (plan is null)
        {
            return;
        }

        _selectedPlan = plan.PlanVersion;
        SelectedPlanDisplay = plan.DisplayName;
        State = OperatorConsoleState.SelectingSlots;
        CurrentPrompt = "选择本次参与测试的工位槽位。";
        StatusMessage = "测试方案已选择。";
    }

    [RelayCommand]
    private async Task ChangePlanAsync(CancellationToken cancellationToken)
    {
        if (!IsChangePlanVisible)
        {
            return;
        }

        _selectedPlan = null;
        _scanQueue = null;
        _bindingSet = null;
        _selectedSlotOrder.Clear();
        _scannedBarcodes.Clear();
        BarcodeInput = "";
        SelectedPlanOption = null;
        SelectedPlanDisplay = "\u672A\u9009\u62E9\u65B9\u6848";
        foreach (var slot in SlotCards)
        {
            slot.ResetForPlanChange();
        }

        await RefreshAvailablePlansAsync(cancellationToken);
        State = OperatorConsoleState.SelectingPlan;
        CurrentPrompt = "\u9009\u62E9\u53EF\u6267\u884C\u6D4B\u8BD5\u65B9\u6848\u3002";
        StatusMessage = "\u5DF2\u8FD4\u56DE\u65B9\u6848\u9009\u62E9\uFF0C\u8BF7\u91CD\u65B0\u9009\u62E9\u6D4B\u8BD5\u65B9\u6848\u3002";
    }

    [RelayCommand]
    private void ToggleSlot(SlotCardViewModel? slot)
    {
        if (slot is null || State != OperatorConsoleState.SelectingSlots)
        {
            return;
        }

        if (!slot.CanSelect)
        {
            StatusMessage = "\u8BE5\u69FD\u4F4D\u7F3A\u5C11\u4E32\u53E3\uFF0C\u8BF7\u5148\u5728\u7CFB\u7EDF\u7BA1\u7406\u4E2D\u5206\u914D\u3002";
            return;
        }

        slot.IsSelected = !slot.IsSelected;
        slot.StatusText = slot.IsSelected ? "已选择" : "待选择";
        if (slot.IsSelected)
        {
            _selectedSlotOrder.Add(slot);
        }
        else
        {
            _selectedSlotOrder.Remove(slot);
        }
    }

    [RelayCommand]
    private void ConfirmSlots()
    {
        if (_station is null)
        {
            StatusMessage = "工位尚未加载。";
            return;
        }

        var selectedNumbers = _selectedSlotOrder
            .Select(slot => slot.SlotNumber)
            .ToList();

        var result = _slotSelectionService.CreateScanQueue(_station, selectedNumbers);
        if (!result.IsSuccess || result.Value is null)
        {
            StatusMessage = string.IsNullOrWhiteSpace(result.Message) ? "槽位选择失败。" : result.Message;
            return;
        }

        _scanQueue = result.Value;
        _scannedBarcodes.Clear();
        foreach (var slot in SelectedSlotCards())
        {
            if (_selectedPlan is not null)
            {
                slot.LoadPlanSteps(_selectedPlan.Steps);
            }

            slot.MarkWaitingBarcode();
        }

        State = OperatorConsoleState.ScanningBarcodes;
        CurrentPrompt = BuildBarcodePrompt();
        StatusMessage = "请按所选槽位顺序扫描 VFD 条码。";
    }

    [RelayCommand]
    private void ScanBarcode()
    {
        if (_scanQueue is null || State != OperatorConsoleState.ScanningBarcodes)
        {
            return;
        }

        _scannedBarcodes.Add(BarcodeInput);
        var currentSlot = SelectedSlotCards().ElementAt(_scannedBarcodes.Count - 1);
        currentSlot.Barcode = BarcodeInput.Trim().ToUpperInvariant();
        currentSlot.MarkQueued();
        BarcodeInput = "";

        if (_scannedBarcodes.Count < _scanQueue.Slots.Count)
        {
            CurrentPrompt = BuildBarcodePrompt();
            return;
        }

        var result = _slotSelectionService.BindBarcodes(_scanQueue, _scannedBarcodes);
        if (!result.IsSuccess || result.Value is null)
        {
            StatusMessage = string.IsNullOrWhiteSpace(result.Message) ? "条码绑定失败。" : result.Message;
            return;
        }

        _bindingSet = result.Value;
        State = OperatorConsoleState.ConfirmingStart;
        CurrentPrompt = "所有槽位就绪后确认开始。";
        StatusMessage = "条码已绑定到所选槽位。";
    }

    [RelayCommand]
    private async Task StartRunAsync(CancellationToken cancellationToken)
    {
        if (_operatorSession is null || _station is null || _selectedPlan is null || _bindingSet is null)
        {
            StatusMessage = "开始前需要员工会话、工位、方案和条码。";
            return;
        }

        State = OperatorConsoleState.Running;
        CurrentPrompt = "生产测试正在运行。";
        InstructionRows.Clear();
        PassCount = 0;
        FailCount = 0;
        WarningCount = 0;
        StartElapsedTimer();
        foreach (var slot in SelectedSlotCards())
        {
            slot.MarkRunning();
        }

        var result = await _productionRunService.StartAsync(
            _operatorSession,
            _station,
            _selectedPlan,
            _bindingSet.Bindings,
            cancellationToken,
            ApplyStepProgress);

        if (!result.IsSuccess || result.Value is null)
        {
            StopElapsedTimer();
            StatusMessage = string.IsNullOrWhiteSpace(result.Message) ? "生产测试启动失败。" : result.Message;
            return;
        }

        DetailRows.Clear();
        var selectedCards = SelectedSlotCards().ToList();
        for (var index = 0; index < selectedCards.Count; index++)
        {
            var run = result.Value.DeviceRuns.ElementAtOrDefault(index);
            if (run is null)
            {
                continue;
            }

            var trace = await _traceRepository.GetDeviceRunTraceAsync(run.DeviceRunId, cancellationToken);
            if (trace is null)
            {
                selectedCards[index].ApplyRunResult(run);
                AddInstructionRows(selectedCards[index], run);
            }
            else
            {
                selectedCards[index].ApplyRunTrace(trace);
                AddInstructionRows(selectedCards[index], trace);
            }

            DetailRows.Add(new DeviceRunTableRowViewModel(
                selectedCards[index].Title,
                selectedCards[index].Barcode,
                run.Conclusion));
        }

        StopElapsedTimer();
        PassCount = selectedCards.Count(card => card.StatusText == "通过");
        FailCount = selectedCards.Count(card => card.StatusText == "失败");
        WarningCount = selectedCards.Count(card => card.StatusText == "警告");
        State = OperatorConsoleState.Completed;
        CurrentPrompt = "测试完成，请查看槽位步骤、比对信息和最终结论。";
        StatusMessage = $"会话结论：{ToDisplayText(result.Value.Conclusion)}。";
    }

    private void StartElapsedTimer()
    {
        _runStartedAt = DateTimeOffset.UtcNow;
        ElapsedDisplay = FormatElapsed(TimeSpan.Zero);
        _elapsedTimerCts?.Cancel();
        _elapsedTimerCts = new CancellationTokenSource();
        _ = UpdateElapsedDisplayAsync(_elapsedTimerCts.Token);
    }

    private void StopElapsedTimer()
    {
        _elapsedTimerCts?.Cancel();
        if (_runStartedAt is not null)
        {
            ElapsedDisplay = FormatElapsed(DateTimeOffset.UtcNow - _runStartedAt.Value);
        }
    }

    private async Task UpdateElapsedDisplayAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (_runStartedAt is not null)
                {
                    ElapsedDisplay = FormatElapsed(DateTimeOffset.UtcNow - _runStartedAt.Value);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        return elapsed.ToString(@"hh\:mm\:ss");
    }

    private void AddInstructionRows(SlotCardViewModel slot, DeviceRunResult run)
    {
        if (run.Steps.Count == 0)
        {
                InstructionRows.Add(new InstructionLogRowViewModel(
                    InstructionRows.Count + 1,
                slot.Title,
                $"最终结果 {ToDisplayText(run.Conclusion)}",
                slot.BarcodeDisplay,
                "无步骤记录"));
            return;
        }

        foreach (var step in run.Steps.OrderBy(step => step.Sequence))
        {
            InstructionRows.Add(new InstructionLogRowViewModel(
                InstructionRows.Count + 1,
                slot.Title,
                $"{step.StepName} {ToDisplayText(step.Conclusion)}",
                slot.BarcodeDisplay,
                $"步骤 {step.Sequence}"));
            }
    }

    private void AddInstructionRows(SlotCardViewModel slot, DeviceRunTrace trace)
    {
        var added = false;
        foreach (var step in trace.Steps.OrderBy(step => step.Sequence))
        {
            foreach (var command in step.CommandTraces.OrderBy(command => command.CreatedAt))
            {
                InstructionRows.Add(new InstructionLogRowViewModel(
                    InstructionRows.Count + 1,
                    command.CommandName,
                    $"{step.StepName} {(command.IsSuccess ? "成功" : "失败")}",
                    command.RequestJson,
                    command.ResponseJson));
                added = true;
            }

            foreach (var comparison in step.Comparisons)
            {
                InstructionRows.Add(new InstructionLogRowViewModel(
                    InstructionRows.Count + 1,
                    slot.Title,
                    comparison.Message,
                    comparison.LeftKey,
                    comparison.RightKey));
                added = true;
            }

            foreach (var measurement in step.Measurements)
            {
                InstructionRows.Add(new InstructionLogRowViewModel(
                    InstructionRows.Count + 1,
                    slot.Title,
                    $"{measurement.Key} {measurement.NumericValue:0.00}{measurement.Unit}",
                    measurement.Source.ToString(),
                    step.StepName));
                added = true;
            }
        }

        if (!added)
        {
            AddInstructionRows(slot, new DeviceRunResult(trace.DeviceRunId, trace.Conclusion, trace.Steps
                .Select(step => new StepRunSnapshot(step.StepRunId, trace.DeviceRunId, step.Sequence, step.StepName, step.Conclusion, step.Message))
                .ToList()));
        }
    }

    private void ApplyStepProgress(SlotStepProgressSnapshot progress)
    {
        var slot = SlotCards.SingleOrDefault(card => card.Slot.Id == progress.SlotId);
        slot?.ApplyStepProgress(progress);
    }

    private string BuildBarcodePrompt()
    {
        if (_scanQueue is null || _scannedBarcodes.Count >= _scanQueue.Slots.Count)
        {
            return "所有所选槽位的条码均已扫描。";
        }

        var slot = _scanQueue.Slots[_scannedBarcodes.Count];
        return $"请扫描 {slot.DisplayName}的 VFD 条码。";
    }

    private IEnumerable<SlotCardViewModel> SelectedSlotCards()
    {
        return _selectedSlotOrder;
    }

    private async Task RefreshAvailablePlansAsync(CancellationToken cancellationToken)
    {
        AvailablePlans.Clear();
        var plans = await _planSelectionService.GetExecutablePlanOptionsAsync(cancellationToken);
        foreach (var plan in plans)
        {
            AvailablePlans.Add(new PlanOptionViewModel(plan));
        }
    }

    private static string ToDisplayText(Domain.Enums.Conclusion conclusion)
    {
        return conclusion switch
        {
            Domain.Enums.Conclusion.Pass => "通过",
            Domain.Enums.Conclusion.Fail => "失败",
            Domain.Enums.Conclusion.Warning => "警告",
            _ => "无结论"
        };
    }

    partial void OnStateChanged(OperatorConsoleState value)
    {
        OnPropertyChanged(nameof(FlowStepDisplay));
        OnPropertyChanged(nameof(FlowStepName));
        OnPropertyChanged(nameof(IsEmployeeActionVisible));
        OnPropertyChanged(nameof(IsPlanActionVisible));
        OnPropertyChanged(nameof(IsSlotActionVisible));
        OnPropertyChanged(nameof(IsBarcodeActionVisible));
        OnPropertyChanged(nameof(IsStartActionVisible));
        OnPropertyChanged(nameof(IsRunningActionVisible));
        OnPropertyChanged(nameof(IsCompletedActionVisible));
        OnPropertyChanged(nameof(IsChangePlanVisible));
    }

    partial void OnSelectedPlanOptionChanged(PlanOptionViewModel? value)
    {
        if (value is not null && State == OperatorConsoleState.SelectingPlan)
        {
            SelectPlan(value);
        }
    }
}
