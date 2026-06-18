using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VfdControl.Application.Engineering;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;
using VfdControl.Presentation.Admin;

namespace VfdControl.Presentation.Engineering;

public sealed partial class WorkflowEditorViewModel : ObservableObject
{
    private readonly ProcessPlanService _processPlanService;
    private readonly WorkflowDefinitionService _workflowDefinitionService;
    private readonly DeviceModelCatalog _deviceModelCatalog;
    private ProcessPlan? _currentPlan;
    private WorkflowStepRowViewModel? _observedSelectedStep;

    [ObservableProperty]
    private string _planName = "未选择方案";

    [ObservableProperty]
    private FailureAction _selectedFailureAction = FailureAction.ContinueAndMarkFail;

    [ObservableProperty]
    private string _statusMessage = "请选择测试方案。";

    [ObservableProperty]
    private WorkflowStepRowViewModel? _selectedStep;

    [ObservableProperty]
    private string _selectedDraftLogicalPointKey = "";

    [ObservableProperty]
    private string _draftStepName = "";

    [ObservableProperty]
    private string _draftStepValue = "";

    public WorkflowEditorViewModel(
        ProcessPlanService processPlanService,
        WorkflowDefinitionService workflowDefinitionService)
        : this(processPlanService, workflowDefinitionService, new DeviceModelCatalog())
    {
    }

    public WorkflowEditorViewModel(
        ProcessPlanService processPlanService,
        WorkflowDefinitionService workflowDefinitionService,
        DeviceModelCatalog deviceModelCatalog)
    {
        _processPlanService = processPlanService;
        _workflowDefinitionService = workflowDefinitionService;
        _deviceModelCatalog = deviceModelCatalog;
        _deviceModelCatalog.Changed += (_, _) => RefreshPointDrivenOptions();
        RefreshPointDrivenOptions();
    }

    public ObservableCollection<WorkflowStepRowViewModel> Steps { get; } = [];

    public ObservableCollection<LogicalPointOptionViewModel> LogicalPointOptions { get; } = [];

    public IReadOnlyList<string> ToleranceTypes { get; } = ["Absolute", "Percent"];

    public IReadOnlyList<LogicalPointOptionViewModel> SelectedLogicalPointOptions =>
        LogicalPointOptions
            .Where(point => point.IsReadable || point.IsWritable)
            .ToList();

    public IReadOnlyList<LogicalPointOptionViewModel> MeasurementPointOptions =>
        LogicalPointOptions
            .Where(point => point.IsReadable && point.IsMeasurement)
            .ToList();

    public IReadOnlyList<LogicalPointWriteOption> SelectedStepWriteValueOptions =>
        SelectedStep is null
            ? []
            : LogicalPointOptions
                .FirstOrDefault(point => point.LogicalKey == SelectedStep.Target)
                ?.WriteOptions
                ?? [];

    public IReadOnlyList<FailureActionOptionViewModel> FailureActions { get; } =
    [
        new(FailureAction.ContinueAndMarkFail, "继续并标记失败"),
        new(FailureAction.ContinueAsWarning, "继续并标记警告"),
        new(FailureAction.StopSlotImmediately, "立即停止槽位"),
        new(FailureAction.PauseAllSlots, "暂停全部槽位"),
        new(FailureAction.RetryThenStop, "重试后停止"),
        new(FailureAction.RequireOperatorConfirm, "需要人工确认")
    ];

    public Task LoadPlanAsync(ProcessPlan plan)
    {
        _currentPlan = plan;
        PlanName = plan.Name;
        Steps.Clear();
        var latestVersion = plan.Versions
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefault();

        if (latestVersion is not null)
        {
            foreach (var step in latestVersion.Steps.OrderBy(step => step.Sequence))
            {
                Steps.Add(new WorkflowStepRowViewModel(step));
            }
        }

        RenumberSteps();
        SelectedStep = Steps.FirstOrDefault();
        StatusMessage = latestVersion is null ? "当前方案暂无版本。" : $"已加载 v{latestVersion.VersionNumber}。";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void AddStartStep() => AddStep(_workflowDefinitionService.CreateStartStep(NextSequence()));

    [RelayCommand]
    private void AddDelayStep() => AddStep(_workflowDefinitionService.CreateDelayStep(NextSequence(), 5000));

    [RelayCommand]
    private void AddReadVfdMeasurementStep() => AddStep(_workflowDefinitionService.CreateReadVfdMeasurementStep(NextSequence()));

    [RelayCommand]
    private void AddReadInstrumentMeasurementStep() => AddStep(_workflowDefinitionService.CreateReadInstrumentMeasurementStep(NextSequence()));

    [RelayCommand]
    private void AddCompareMeasurementStep() => AddStep(_workflowDefinitionService.CreateCompareMeasurementStep(NextSequence(), SelectedFailureAction));

    [RelayCommand]
    private void AddStopStep() => AddStep(_workflowDefinitionService.CreateStopStep(NextSequence()));

    [RelayCommand]
    private void AddBlankStep()
    {
        var defaultPoint = LogicalPointOptions.FirstOrDefault(point => point.IsReadable && point.IsMeasurement)
            ?? LogicalPointOptions.FirstOrDefault(point => point.IsReadable)
            ?? LogicalPointOptions.FirstOrDefault(point => point.IsWritable);
        if (defaultPoint is not null)
        {
            AddStep(new ProcessStep(
                Guid.NewGuid(),
                NextSequence(),
                "\u65B0\u589E\u6B65\u9AA4",
                new StepCommand(InferCommandType(defaultPoint, ""), defaultPoint.LogicalKey, null),
                new StepFailurePolicy(SelectedFailureAction, 0),
                affectsFinalConclusion: true));
            return;
        }

        AddStep(new ProcessStep(
            Guid.NewGuid(),
            NextSequence(),
            "\u65B0\u589E\u6B65\u9AA4",
            new StepCommand("Delay", "Timer", "1000"),
            new StepFailurePolicy(SelectedFailureAction, 0),
            affectsFinalConclusion: true));
    }

    [RelayCommand]
    private void AddDraftLogicalPointStep()
    {
        var point = LogicalPointOptions.FirstOrDefault(point => point.LogicalKey == SelectedDraftLogicalPointKey);
        if (point is null)
        {
            StatusMessage = "\u8BF7\u5148\u9009\u62E9\u903B\u8F91\u70B9\u4F4D\u3002";
            return;
        }

        AddStep(CreateStepFromLogicalPoint(point, DraftStepName, DraftStepValue));
    }

    [RelayCommand]
    private void ValidateWorkflow()
    {
        StatusMessage = ValidateEnabledSteps(out var validationMessage)
            ? "\u65B9\u6848\u6B65\u9AA4\u5DF2\u6821\u9A8C\u3002"
            : validationMessage;
    }

    [RelayCommand]
    private async Task SaveVersionAsync(CancellationToken cancellationToken)
    {
        if (_currentPlan is null)
        {
            StatusMessage = "请先选择测试方案。";
            return;
        }

        var enabledSteps = Steps
            .Where(row => row.IsEnabled)
            .Select(row => row.ToProcessStep())
            .ToList();

        if (enabledSteps.Count == 0)
        {
            StatusMessage = "至少需要一个启用步骤。";
            return;
        }

        if (!ValidateEnabledSteps(out var validationMessage))
        {
            StatusMessage = validationMessage;
            return;
        }

        var result = await _processPlanService.SaveNewVersionAsync(
            _currentPlan.Id,
            enabledSteps,
            isExecutable: true,
            cancellationToken);

        StatusMessage = result.IsSuccess && result.Value is not null
            ? $"已保存为 v{result.Value.VersionNumber}。"
            : result.Message;
    }

    private bool ValidateEnabledSteps(out string message)
    {
        if (Steps.Count == 0)
        {
            message = "\u5F53\u524D\u65B9\u6848\u8FD8\u6CA1\u6709\u6B65\u9AA4\u3002";
            return false;
        }

        var enabledSteps = Steps.Where(step => step.IsEnabled).ToList();
        if (enabledSteps.Count == 0)
        {
            message = "\u81F3\u5C11\u9700\u8981\u4E00\u4E2A\u542F\u7528\u6B65\u9AA4\u3002";
            return false;
        }

        var missingWriteValueStep = enabledSteps.FirstOrDefault(step =>
            step.CommandType is "Start" or "Stop"
            && string.IsNullOrWhiteSpace(step.Value));
        if (missingWriteValueStep is not null)
        {
            message = $"\u6B65\u9AA4\u201C{missingWriteValueStep.Name}\u201D\u7F3A\u5C11\u5199\u5165\u547D\u4EE4\u3002";
            return false;
        }

        message = string.Empty;
        return true;
    }

    [RelayCommand]
    private void CopySelectedStep() => CopyStep(SelectedStep);

    [RelayCommand]
    private void RemoveSelectedStep() => RemoveStep(SelectedStep);

    [RelayCommand]
    private void MoveSelectedStepUp() => MoveStep(SelectedStep, -1);

    [RelayCommand]
    private void MoveSelectedStepDown() => MoveStep(SelectedStep, 1);

    [RelayCommand]
    private void CopyStep(WorkflowStepRowViewModel? step)
    {
        if (step is null)
        {
            return;
        }

        var index = Steps.IndexOf(step);
        if (index < 0)
        {
            return;
        }

        var copy = WorkflowStepRowViewModel.CopyOf(step);
        Steps.Insert(index + 1, copy);
        RenumberSteps();
        SelectedStep = copy;
        StatusMessage = "步骤已复制。";
    }

    [RelayCommand]
    private void RemoveStep(WorkflowStepRowViewModel? step)
    {
        if (step is null)
        {
            return;
        }

        var index = Steps.IndexOf(step);
        if (index < 0)
        {
            return;
        }

        Steps.RemoveAt(index);
        RenumberSteps();
        SelectedStep = Steps.Count == 0 ? null : Steps[Math.Max(0, index - 1)];
        StatusMessage = "步骤已删除。";
    }

    [RelayCommand]
    private void MoveStepUp(WorkflowStepRowViewModel? step) => MoveStep(step, -1);

    [RelayCommand]
    private void MoveStepDown(WorkflowStepRowViewModel? step) => MoveStep(step, 1);

    private int NextSequence() => Steps.Count + 1;

    private void AddStep(ProcessStep step)
    {
        var row = new WorkflowStepRowViewModel(step);
        Steps.Add(row);
        SelectedStep = row;
        StatusMessage = "步骤已添加。";
    }

    private void MoveStep(WorkflowStepRowViewModel? step, int offset)
    {
        if (step is null)
        {
            return;
        }

        var index = Steps.IndexOf(step);
        var newIndex = index + offset;
        if (index < 0 || newIndex < 0 || newIndex >= Steps.Count)
        {
            return;
        }

        Steps.Move(index, newIndex);
        RenumberSteps();
        SelectedStep = step;
        StatusMessage = "步骤顺序已更新。";
    }

    private void RenumberSteps()
    {
        for (var index = 0; index < Steps.Count; index++)
        {
            Steps[index].Sequence = index + 1;
        }
    }

    partial void OnSelectedStepChanged(WorkflowStepRowViewModel? value)
    {
        if (_observedSelectedStep is not null)
        {
            _observedSelectedStep.PropertyChanged -= OnSelectedStepPropertyChanged;
        }

        _observedSelectedStep = value;
        if (_observedSelectedStep is not null)
        {
            _observedSelectedStep.PropertyChanged += OnSelectedStepPropertyChanged;
        }

        OnPropertyChanged(nameof(SelectedLogicalPointOptions));
        OnPropertyChanged(nameof(MeasurementPointOptions));
        OnPropertyChanged(nameof(SelectedStepWriteValueOptions));
    }

    partial void OnSelectedDraftLogicalPointKeyChanged(string value)
    {
        DraftStepValue = "";
        UpdateDraftStepDefaults(updateName: true);
    }

    partial void OnDraftStepValueChanged(string value)
    {
        var point = LogicalPointOptions.FirstOrDefault(point => point.LogicalKey == SelectedDraftLogicalPointKey);
        if (point?.IsWritable == true)
        {
            UpdateDraftStepDefaults(updateName: true);
        }
    }

    private void OnSelectedStepPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(WorkflowStepRowViewModel.CommandType))
        {
            OnPropertyChanged(nameof(SelectedLogicalPointOptions));
            OnPropertyChanged(nameof(SelectedStepWriteValueOptions));
        }

        if (args.PropertyName == nameof(WorkflowStepRowViewModel.Target))
        {
            ApplyLogicalPointCommandDefaults();
            OnPropertyChanged(nameof(SelectedStepWriteValueOptions));
        }

        if (args.PropertyName == nameof(WorkflowStepRowViewModel.Value))
        {
            ApplyWriteCommandDefaults();
        }
    }

    private void RefreshPointDrivenOptions()
    {
        LogicalPointOptions.Clear();
        foreach (var point in _deviceModelCatalog.LogicalPoints.Select(LogicalPointOptionViewModel.FromPoint))
        {
            LogicalPointOptions.Add(point);
        }

        EnsureDraftStepSelection();
        OnPropertyChanged(nameof(SelectedLogicalPointOptions));
        OnPropertyChanged(nameof(MeasurementPointOptions));
        OnPropertyChanged(nameof(SelectedStepWriteValueOptions));
    }

    private void ApplyLogicalPointCommandDefaults()
    {
        if (SelectedStep is null)
        {
            return;
        }

        var point = LogicalPointOptions.FirstOrDefault(point => point.LogicalKey == SelectedStep.Target);
        if (point is null)
        {
            return;
        }

        SelectedStep.CommandType = InferCommandType(point, SelectedStep.Value);
    }

    private void ApplyWriteCommandDefaults()
    {
        if (SelectedStep is null)
        {
            return;
        }

        var point = LogicalPointOptions.FirstOrDefault(point => point.LogicalKey == SelectedStep.Target);
        if (point?.IsWritable == true)
        {
            SelectedStep.CommandType = InferCommandType(point, SelectedStep.Value);
        }
    }

    private static string InferCommandType(LogicalPointOptionViewModel point, string? value)
    {
        if (point.IsWriteFunction)
        {
            return IsStopWriteValue(point.LogicalKey, value) ? "Stop" : "Start";
        }

        if (point.IsReadFunction && point.IsNumericDataType)
        {
            return "ReadMeasurement";
        }

        return "ReadString";
    }

    private void EnsureDraftStepSelection()
    {
        if (LogicalPointOptions.Any(point => point.LogicalKey == SelectedDraftLogicalPointKey))
        {
            return;
        }

        var defaultPoint = LogicalPointOptions.FirstOrDefault(point => point.IsReadable && point.IsMeasurement)
            ?? LogicalPointOptions.FirstOrDefault(point => point.IsReadable)
            ?? LogicalPointOptions.FirstOrDefault(point => point.IsWritable);

        SelectedDraftLogicalPointKey = defaultPoint?.LogicalKey ?? "";
        UpdateDraftStepDefaults(updateName: true);
    }

    private void UpdateDraftStepDefaults(bool updateName)
    {
        if (!updateName)
        {
            return;
        }

        var point = LogicalPointOptions.FirstOrDefault(point => point.LogicalKey == SelectedDraftLogicalPointKey);
        if (point is not null)
        {
            DraftStepName = CreateDefaultStepName(point, DraftStepValue);
        }
    }

    private static string CreateDefaultStepName(LogicalPointOptionViewModel point, string? value)
    {
        if (point.IsWritable)
        {
            var writeName = ResolveWriteOptionDisplay(point.LogicalKey, point.WriteOptions, value);
            return string.IsNullOrWhiteSpace(writeName)
                ? $"\u5199\u5165 {point.DisplayName}"
                : $"\u5199\u5165 {writeName}";
        }

        return point.IsMeasurement ? $"\u8BFB\u53D6 {point.DisplayName}" : $"\u786E\u8BA4 {point.DisplayName}";
    }

    private ProcessStep CreateStepFromLogicalPoint(
        LogicalPointOptionViewModel point,
        string? displayName,
        string? value)
    {
        var normalizedValue = string.IsNullOrWhiteSpace(value) ? null : value;
        var commandType = InferCommandType(point, normalizedValue);
        return new ProcessStep(
            Guid.NewGuid(),
            NextSequence(),
            string.IsNullOrWhiteSpace(displayName) ? CreateDefaultStepName(point, normalizedValue) : displayName.Trim(),
            new StepCommand(commandType, point.LogicalKey, CommandValueForStep(commandType, normalizedValue)),
            new StepFailurePolicy(SelectedFailureAction, 0),
            affectsFinalConclusion: true,
            RuleForStep(commandType, normalizedValue));
    }

    private static string? CommandValueForStep(string commandType, string? value)
    {
        return commandType is "ReadMeasurement" or "ReadString"
            ? null
            : string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static StepRule? RuleForStep(string commandType, string? value)
    {
        if (commandType == "ReadString" && !string.IsNullOrWhiteSpace(value))
        {
            return StepRule.StringEquals(value.Trim());
        }

        return null;
    }

    private static bool IsStopWriteValue(string logicalKey, string? value)
    {
        return logicalKey.Equals("Vfd:Control", StringComparison.OrdinalIgnoreCase)
            && (value?.Trim() is "5" or "6");
    }

    private static string ResolveWriteOptionDisplay(
        string logicalKey,
        IReadOnlyList<LogicalPointWriteOption> options,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var normalizedValue = value.Trim();
        var option = options.FirstOrDefault(option =>
            option.Value.Equals(normalizedValue, StringComparison.OrdinalIgnoreCase));
        if (option is not null)
        {
            return option.DisplayName;
        }

        return logicalKey.Equals("Vfd:Control", StringComparison.OrdinalIgnoreCase)
            ? WorkflowStepRowViewModel.ResolveVfdControlCommandDisplay(normalizedValue)
            : "";
    }
}

public sealed record LogicalPointOptionViewModel(
    string LogicalKey,
    string DisplayName,
    string AccessMode,
    string FunctionCode,
    string RegisterAddress,
    string DataType,
    bool IsCustom,
    IReadOnlyList<LogicalPointWriteOption> WriteOptions)
{
    public string DisplayNameWithKey => $"{DisplayName} ({LogicalKey})";

    public bool IsReadable => IsReadFunction || AccessMode == "\u8BFB\u53D6";

    public bool IsWritable => IsWriteFunction || AccessMode == "\u5199\u5165";

    public bool IsReadFunction => FunctionCode == "03";

    public bool IsWriteFunction => FunctionCode == "06";

    public bool IsNumericDataType => DataType is "Decimal" or "Number" or "Enum" or "UInt16" or "UInt32" or "UInt64" or "Int16" or "Int32" or "Int64" or "Integer" or "Double" or "Single" or "Float";

    public bool IsStringDataType => DataType == "String";

    public bool IsMeasurement => IsReadFunction && IsNumericDataType && DataType != "Enum";

    public static LogicalPointOptionViewModel FromPoint(LogicalPointRowViewModel point)
    {
        return new LogicalPointOptionViewModel(
            point.LogicalKey,
            point.DisplayName,
            point.AccessMode,
            point.FunctionCode,
            point.RegisterAddress,
            point.DataType,
            point.IsCustom,
            point.WriteOptions.ToList());
    }
}

public sealed class WorkflowStepRowViewModel : ObservableObject
{
    private int _sequence;
    private string _name;
    private string _commandType;
    private string _target;
    private string _value;
    private string _lowerLimit;
    private string _upperLimit;
    private string _expectedText;
    private string _compareLeftTarget;
    private string _compareRightTarget;
    private string _toleranceType;
    private string _toleranceValue;
    private FailureAction _failureAction;
    private int _maxRetries;
    private bool _affectsFinalConclusion;
    private bool _isEnabled = true;

    public WorkflowStepRowViewModel(ProcessStep step)
    {
        Id = step.Id;
        _sequence = step.Sequence;
        _name = step.Name;
        _commandType = step.Command.CommandType;
        _target = step.Command.Target;
        _value = step.Command.Value ?? "";
        var compareTargets = ParseCompareTargets(step.Command.Target);
        _compareLeftTarget = compareTargets.Left;
        _compareRightTarget = compareTargets.Right;
        var tolerance = ParseTolerance(step.Command.Value);
        _toleranceType = tolerance.Type;
        _toleranceValue = tolerance.Value;
        _lowerLimit = FormatNumber(step.Rule?.RuleType == StepRule.NumericRangeRuleType ? step.Rule.LowerLimit : null);
        _upperLimit = FormatNumber(step.Rule?.RuleType == StepRule.NumericRangeRuleType ? step.Rule.UpperLimit : null);
        _expectedText = step.Rule?.RuleType == StepRule.StringEqualsRuleType
            ? step.Rule.ExpectedValue ?? ""
            : step.Command.CommandType == "ReadString"
                ? step.Command.Value ?? ""
                : "";
        _failureAction = step.FailurePolicy.Action;
        _maxRetries = step.FailurePolicy.MaxRetries;
        _affectsFinalConclusion = step.AffectsFinalConclusion;
    }

    public Guid Id { get; }

    public int Sequence
    {
        get => _sequence;
        set => SetProperty(ref _sequence, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string CommandType
    {
        get => _commandType;
        set
        {
            if (SetProperty(ref _commandType, value))
            {
                OnPropertyChanged(nameof(ParameterSummary));
                OnPropertyChanged(nameof(TargetSummary));
                OnPropertyChanged(nameof(ConditionSummary));
                OnPropertyChanged(nameof(StepTypeDisplay));
                OnPropertyChanged(nameof(SinglePointVisibility));
                OnPropertyChanged(nameof(GenericValueVisibility));
                OnPropertyChanged(nameof(WriteValueVisibility));
                OnPropertyChanged(nameof(NumericRangeVisibility));
                OnPropertyChanged(nameof(StringExpectedVisibility));
                OnPropertyChanged(nameof(CompareSettingsVisibility));
                OnPropertyChanged(nameof(GenericValueLabel));
            }
        }
    }

    public string Target
    {
        get => _target;
        set
        {
            if (SetProperty(ref _target, value))
            {
                if (CommandType == "CompareMeasurement")
                {
                    SyncCompareTargetsFromTarget();
                }

                OnPropertyChanged(nameof(ParameterSummary));
                OnPropertyChanged(nameof(TargetSummary));
                OnPropertyChanged(nameof(ConditionSummary));
                OnPropertyChanged(nameof(WriteValueVisibility));
            }
        }
    }

    public string Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
            {
                if (CommandType == "CompareMeasurement")
                {
                    SyncToleranceFromValue();
                }

                OnPropertyChanged(nameof(ParameterSummary));
                OnPropertyChanged(nameof(ConditionSummary));
            }
        }
    }

    public string LowerLimit
    {
        get => _lowerLimit;
        set
        {
            if (SetProperty(ref _lowerLimit, value))
            {
                OnPropertyChanged(nameof(ParameterSummary));
                OnPropertyChanged(nameof(ConditionSummary));
            }
        }
    }

    public string UpperLimit
    {
        get => _upperLimit;
        set
        {
            if (SetProperty(ref _upperLimit, value))
            {
                OnPropertyChanged(nameof(ParameterSummary));
                OnPropertyChanged(nameof(ConditionSummary));
            }
        }
    }

    public string ExpectedText
    {
        get => _expectedText;
        set
        {
            if (SetProperty(ref _expectedText, value))
            {
                OnPropertyChanged(nameof(ParameterSummary));
                OnPropertyChanged(nameof(ConditionSummary));
            }
        }
    }

    public string CompareLeftTarget
    {
        get => _compareLeftTarget;
        set
        {
            if (SetProperty(ref _compareLeftTarget, value))
            {
                UpdateCompareTargetFromFields();
                OnPropertyChanged(nameof(ParameterSummary));
                OnPropertyChanged(nameof(TargetSummary));
            }
        }
    }

    public string CompareRightTarget
    {
        get => _compareRightTarget;
        set
        {
            if (SetProperty(ref _compareRightTarget, value))
            {
                UpdateCompareTargetFromFields();
                OnPropertyChanged(nameof(ParameterSummary));
                OnPropertyChanged(nameof(TargetSummary));
            }
        }
    }

    public string ToleranceType
    {
        get => _toleranceType;
        set
        {
            if (SetProperty(ref _toleranceType, value))
            {
                UpdateToleranceValueFromFields();
                OnPropertyChanged(nameof(ParameterSummary));
                OnPropertyChanged(nameof(ConditionSummary));
            }
        }
    }

    public string ToleranceValue
    {
        get => _toleranceValue;
        set
        {
            if (SetProperty(ref _toleranceValue, value))
            {
                UpdateToleranceValueFromFields();
                OnPropertyChanged(nameof(ParameterSummary));
                OnPropertyChanged(nameof(ConditionSummary));
            }
        }
    }

    public FailureAction FailureAction
    {
        get => _failureAction;
        set
        {
            if (SetProperty(ref _failureAction, value))
            {
                OnPropertyChanged(nameof(FailureActionDisplay));
            }
        }
    }

    public int MaxRetries
    {
        get => _maxRetries;
        set => SetProperty(ref _maxRetries, Math.Max(0, value));
    }

    public bool AffectsFinalConclusion
    {
        get => _affectsFinalConclusion;
        set => SetProperty(ref _affectsFinalConclusion, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string SinglePointVisibility => CommandType == "CompareMeasurement"
        ? "Collapsed"
        : "Visible";

    public string GenericValueVisibility => CommandType is "ReadMeasurement" or "ReadString" or "CompareMeasurement"
        or "Start" or "Stop"
        ? "Collapsed"
        : "Visible";

    public string WriteValueVisibility => CommandType is "Start" or "Stop"
        ? "Visible"
        : "Collapsed";

    public string NumericRangeVisibility => CommandType == "ReadMeasurement"
        ? "Visible"
        : "Collapsed";

    public string StringExpectedVisibility => CommandType == "ReadString"
        ? "Visible"
        : "Collapsed";

    public string CompareSettingsVisibility => CommandType == "CompareMeasurement"
        ? "Visible"
        : "Collapsed";

    public string StepTypeDisplay => CommandType switch
    {
        "Start" or "Stop" => "\u5199\u5165\u70B9\u4F4D",
        "Delay" => "\u5EF6\u8FDF",
        "ReadMeasurement" or "ReadString" => "\u8BFB\u53D6\u70B9\u4F4D",
        "CompareMeasurement" => "\u70B9\u4F4D\u8BFB\u6570\u6BD4\u5BF9",
        _ => "\u81EA\u5B9A\u4E49\u6B65\u9AA4"
    };

    public string GenericValueLabel => CommandType switch
    {
        "Delay" => "\u7B49\u5F85\u65F6\u957F(ms)",
        "CompareMeasurement" => "\u5BB9\u5DEE\u8BBE\u7F6E",
        "Start" or "Stop" => "\u5199\u5165\u503C",
        _ => "\u547D\u4EE4\u503C"
    };

    public string TargetSummary => CommandType == "CompareMeasurement"
        ? $"{CompareLeftTarget} vs {CompareRightTarget}"
        : Target;

    public string ConditionSummary
    {
        get
        {
            return CommandType switch
            {
                "ReadMeasurement" => FormatRangeSummary(defaultText: "-"),
                "ReadString" => string.IsNullOrWhiteSpace(ExpectedText) ? "-" : $"= {ExpectedText}",
                "CompareMeasurement" => FormatToleranceSummary(),
                "Delay" => string.IsNullOrWhiteSpace(Value) ? "-" : $"{Value} ms",
                "Start" or "Stop" => FormatWriteConditionSummary(),
                _ => string.IsNullOrWhiteSpace(Value) ? "-" : Value
            };
        }
    }

    public string ParameterSummary
    {
        get
        {
            if (CommandType == "ReadMeasurement")
            {
                var range = FormatRangeSummary(defaultText: "");
                return string.IsNullOrWhiteSpace(range) ? Target : $"{Target} / {range}";
            }

            if (CommandType == "ReadString")
            {
                return string.IsNullOrWhiteSpace(ExpectedText) ? Target : $"{Target} = {ExpectedText}";
            }

            if (CommandType == "CompareMeasurement")
            {
                return $"{CompareLeftTarget} vs {CompareRightTarget} / {ToleranceType}:{ToleranceValue}";
            }

            if (CommandType is "Start" or "Stop")
            {
                var writeValue = FormatWriteValueSummary();
                return string.IsNullOrWhiteSpace(writeValue)
                    ? Target
                    : $"{Target} / {writeValue}";
            }

            return string.IsNullOrWhiteSpace(Value)
                ? Target
                : $"{Target} / {Value}";
        }
    }

    public string FailureActionDisplay => FailureActionDisplayText(FailureAction);

    public static WorkflowStepRowViewModel CopyOf(WorkflowStepRowViewModel source)
    {
        return new WorkflowStepRowViewModel(source.ToProcessStep())
        {
            Sequence = source.Sequence + 1,
            Name = source.Name,
            CommandType = source.CommandType,
            Target = source.Target,
            Value = source.Value,
            CompareLeftTarget = source.CompareLeftTarget,
            CompareRightTarget = source.CompareRightTarget,
            ToleranceType = source.ToleranceType,
            ToleranceValue = source.ToleranceValue,
            LowerLimit = source.LowerLimit,
            UpperLimit = source.UpperLimit,
            ExpectedText = source.ExpectedText,
            FailureAction = source.FailureAction,
            MaxRetries = source.MaxRetries,
            AffectsFinalConclusion = source.AffectsFinalConclusion,
            IsEnabled = source.IsEnabled
        };
    }

    public ProcessStep ToProcessStep()
    {
        return new ProcessStep(
            Id,
            Sequence,
            Name,
            new StepCommand(CommandType, Target, CommandValueForStep()),
            new StepFailurePolicy(FailureAction, MaxRetries),
            AffectsFinalConclusion,
            RuleForStep());
    }

    private string? CommandValueForStep()
    {
        if (CommandType is "ReadMeasurement" or "ReadString")
        {
            return null;
        }

        if (CommandType == "CompareMeasurement")
        {
            return $"{ToleranceType}:{ToleranceValue}";
        }

        return string.IsNullOrWhiteSpace(Value) ? null : Value;
    }

    private StepRule? RuleForStep()
    {
        if (CommandType == "ReadMeasurement")
        {
            var hasLower = TryParseOptionalNumber(LowerLimit, out var lowerLimit);
            var hasUpper = TryParseOptionalNumber(UpperLimit, out var upperLimit);
            return hasLower || hasUpper ? StepRule.NumericRange(lowerLimit, upperLimit) : null;
        }

        if (CommandType == "ReadString" && !string.IsNullOrWhiteSpace(ExpectedText))
        {
            return StepRule.StringEquals(ExpectedText.Trim());
        }

        return null;
    }

    private void SyncCompareTargetsFromTarget()
    {
        var compareTargets = ParseCompareTargets(Target);
        SetProperty(ref _compareLeftTarget, compareTargets.Left, nameof(CompareLeftTarget));
        SetProperty(ref _compareRightTarget, compareTargets.Right, nameof(CompareRightTarget));
    }

    private void SyncToleranceFromValue()
    {
        var tolerance = ParseTolerance(Value);
        SetProperty(ref _toleranceType, tolerance.Type, nameof(ToleranceType));
        SetProperty(ref _toleranceValue, tolerance.Value, nameof(ToleranceValue));
    }

    private void UpdateCompareTargetFromFields()
    {
        var target = string.IsNullOrWhiteSpace(CompareLeftTarget) && string.IsNullOrWhiteSpace(CompareRightTarget)
            ? ""
            : $"{CompareLeftTarget}|{CompareRightTarget}";
        SetProperty(ref _target, target, nameof(Target));
    }

    private void UpdateToleranceValueFromFields()
    {
        SetProperty(ref _value, $"{ToleranceType}:{ToleranceValue}", nameof(Value));
    }

    private static (string Left, string Right) ParseCompareTargets(string target)
    {
        var parts = target.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return (
            parts.ElementAtOrDefault(0) ?? "",
            parts.ElementAtOrDefault(1) ?? "");
    }

    private static (string Type, string Value) ParseTolerance(string? value)
    {
        var parts = (value ?? "Absolute:0").Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return (
            parts.ElementAtOrDefault(0) ?? "Absolute",
            parts.ElementAtOrDefault(1) ?? "0");
    }

    private string FormatRangeSummary()
    {
        return FormatRangeSummary(defaultText: "");
    }

    private string FormatRangeSummary(string defaultText)
    {
        var lower = string.IsNullOrWhiteSpace(LowerLimit) ? "-\u221E" : LowerLimit.Trim();
        var upper = string.IsNullOrWhiteSpace(UpperLimit) ? "+\u221E" : UpperLimit.Trim();
        return string.IsNullOrWhiteSpace(LowerLimit) && string.IsNullOrWhiteSpace(UpperLimit)
            ? defaultText
            : $"{lower} ~ {upper}";
    }

    private string FormatToleranceSummary()
    {
        return ToleranceType.Equals("Percent", StringComparison.OrdinalIgnoreCase)
            ? $"\u767E\u5206\u6BD4 \u00B1{ToleranceValue}%"
            : $"\u7EDD\u5BF9\u503C \u00B1{ToleranceValue}";
    }

    private string FormatWriteConditionSummary()
    {
        if (string.IsNullOrWhiteSpace(Value))
        {
            return CommandType == "Stop" ? "\u5199\u5165\u505C\u6B62" : "\u5199\u5165\u542F\u52A8";
        }

        var display = Target.Equals("Vfd:Control", StringComparison.OrdinalIgnoreCase)
            ? ResolveVfdControlCommandDisplay(Value)
            : "";
        return string.IsNullOrWhiteSpace(display)
            ? $"\u5199\u5165\u503C {Value.Trim()}"
            : $"\u5199\u5165{display}";
    }

    private string FormatWriteValueSummary()
    {
        if (string.IsNullOrWhiteSpace(Value))
        {
            return "";
        }

        var display = Target.Equals("Vfd:Control", StringComparison.OrdinalIgnoreCase)
            ? ResolveVfdControlCommandDisplay(Value)
            : "";
        return string.IsNullOrWhiteSpace(display)
            ? Value.Trim()
            : $"{display} ({Value.Trim()})";
    }

    public static string ResolveVfdControlCommandDisplay(string? value)
    {
        return value?.Trim() switch
        {
            "1" => "\u6B63\u8F6C\u8FD0\u884C",
            "2" => "\u53CD\u8F6C\u8FD0\u884C",
            "3" => "\u6B63\u8F6C\u70B9\u52A8",
            "4" => "\u53CD\u8F6C\u70B9\u52A8",
            "5" => "\u81EA\u7531\u505C\u673A",
            "6" => "\u51CF\u901F\u505C\u673A",
            "7" => "\u6545\u969C\u590D\u4F4D",
            _ => ""
        };
    }

    private static bool TryParseOptionalNumber(string text, out double? value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = null;
            return false;
        }

        if (double.TryParse(text.Trim(), out var parsed))
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }

    private static string FormatNumber(double? value)
    {
        return value?.ToString("0.##########") ?? "";
    }

    private static string FailureActionDisplayText(FailureAction action)
    {
        return action switch
        {
            FailureAction.ContinueAndMarkFail => "继续并标记失败",
            FailureAction.ContinueAsWarning => "继续并标记警告",
            FailureAction.StopSlotImmediately => "立即停止槽位",
            FailureAction.PauseAllSlots => "暂停全部槽位",
            FailureAction.RetryThenStop => "重试后停止",
            FailureAction.RequireOperatorConfirm => "需要人工确认",
            _ => action.ToString()
        };
    }
}

public sealed record FailureActionOptionViewModel(FailureAction Value, string DisplayName);
