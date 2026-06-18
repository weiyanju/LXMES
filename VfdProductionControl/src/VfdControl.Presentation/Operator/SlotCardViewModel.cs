using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VfdControl.Application.Execution;
using VfdControl.Application.Traceability;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;
using VfdControl.Domain.Stations;

namespace VfdControl.Presentation.Operator;

public sealed partial class SlotCardViewModel : ObservableObject
{
    private const string NeutralColor = "#E8F2F7";
    private const string ReadyColor = "#D7EEF9";
    private const string RunningColor = "#D8E8FF";
    private const string PassColor = "#DFF5E9";
    private const string FailColor = "#F9DEDE";
    private const string WarningColor = "#FFF2CC";
    private const string NeutralCardColor = "#FEFFFF";
    private const string ReadyCardColor = "#F4FBFE";
    private const string RunningCardColor = "#F3F7FF";
    private const string PassCardColor = "#EFFAF4";
    private const string FailCardColor = "#FDF1F1";
    private const string WarningCardColor = "#FFF9E8";

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _barcode = "";

    [ObservableProperty]
    private string _statusText = "待选择";

    [ObservableProperty]
    private string _statusColor = NeutralColor;

    [ObservableProperty]
    private string _cardBackgroundColor = NeutralCardColor;

    [ObservableProperty]
    private string _currentComparisonText = "比对信息：待执行";

    [ObservableProperty]
    private string _finalConclusionText = "最终结果：待执行";

    [ObservableProperty]
    private string _latestCommandText = "指令：待发送";

    public SlotCardViewModel(StationSlot slot)
    {
        Slot = slot;
        SlotNumber = slot.Number.Value;
        PortName = slot.CommunicationConfig.PortDisplay;
        CanSelect = slot.CommunicationConfig.HasPort;
        if (!CanSelect)
        {
            StatusText = "\u7F3A\u5C11\u4E32\u53E3";
        }
    }

    public StationSlot Slot { get; }

    public int SlotNumber { get; }

    public string Title => Slot.DisplayName;

    public string PortName { get; }

    public bool CanSelect { get; }

    public string BarcodeDisplay => string.IsNullOrWhiteSpace(Barcode) ? "未绑定条码" : Barcode;

    public ObservableCollection<SlotStepRowViewModel> StepRows { get; } = [];

    public void MarkWaitingBarcode()
    {
        StatusText = "等待条码";
        StatusColor = NeutralColor;
        CardBackgroundColor = NeutralCardColor;
        LatestCommandText = "指令：等待条码绑定";
    }

    public void MarkQueued()
    {
        StatusText = "已就绪";
        StatusColor = ReadyColor;
        CardBackgroundColor = ReadyCardColor;
        LatestCommandText = "指令：等待启动";
    }

    public void MarkRunning()
    {
        StatusText = "运行中";
        StatusColor = RunningColor;
        CardBackgroundColor = RunningCardColor;
        CurrentComparisonText = "比对信息：运行中";
        FinalConclusionText = "最终结果：计算中";
        LatestCommandText = "指令：测试执行中";
    }

    public void ApplyConclusion(Conclusion conclusion)
    {
        StatusText = ToDisplayText(conclusion);
        StatusColor = conclusion switch
        {
            Conclusion.Pass => PassColor,
            Conclusion.Fail => FailColor,
            Conclusion.Warning => WarningColor,
            _ => NeutralColor
        };
        CardBackgroundColor = conclusion switch
        {
            Conclusion.Pass => PassCardColor,
            Conclusion.Fail => FailCardColor,
            Conclusion.Warning => WarningCardColor,
            _ => NeutralCardColor
        };
        FinalConclusionText = $"最终结果：{ToDisplayText(conclusion)}";
    }

    public void ApplyRunResult(DeviceRunResult result)
    {
        StepRows.Clear();
        foreach (var step in result.Steps.OrderBy(step => step.Sequence))
        {
            StepRows.Add(new SlotStepRowViewModel(step.Sequence, step.StepName, step.Conclusion));
        }

        var comparisonStep = result.Steps.LastOrDefault(step =>
            step.StepName.Contains("比对", StringComparison.OrdinalIgnoreCase) ||
            step.StepName.Contains("Compare", StringComparison.OrdinalIgnoreCase));
        var summaryStep = comparisonStep ?? result.Steps.LastOrDefault();
        CurrentComparisonText = summaryStep is null
            ? "比对信息：无步骤结果"
            : $"{summaryStep.StepName}：{ToDisplayText(summaryStep.Conclusion)}";
        LatestCommandText = summaryStep is null
            ? "指令：无执行记录"
            : $"指令：{summaryStep.StepName}";
        ApplyConclusion(result.Conclusion);
    }

    public void ApplyRunTrace(DeviceRunTrace trace)
    {
        StepRows.Clear();
        foreach (var step in trace.Steps.OrderBy(step => step.Sequence))
        {
            StepRows.Add(new SlotStepRowViewModel(
                step.Sequence,
                step.StepName,
                step.Conclusion,
                BuildStepDetailText(step)));
        }

        var comparison = trace.Steps
            .SelectMany(step => step.Comparisons)
            .LastOrDefault();
        if (comparison is not null)
        {
            CurrentComparisonText = $"{comparison.LeftKey} / {comparison.RightKey}：{comparison.Message}";
        }
        else
        {
            var measurement = trace.Steps
                .SelectMany(step => step.Measurements)
                .LastOrDefault();
            CurrentComparisonText = measurement is null
                ? "比对信息：无步骤结果"
                : $"{measurement.Key}：{measurement.NumericValue:0.00}{measurement.Unit}";
        }

        var command = trace.Steps
            .SelectMany(step => step.CommandTraces)
            .OrderBy(command => command.CreatedAt)
            .LastOrDefault();
        LatestCommandText = command is null
            ? "指令：无执行记录"
            : $"指令：{command.CommandName} {(command.IsSuccess ? "成功" : "失败")}";
        ApplyConclusion(trace.Conclusion);
    }

    public void LoadPlanSteps(IEnumerable<ProcessStep> steps)
    {
        StepRows.Clear();
        foreach (var step in steps.OrderBy(step => step.Sequence))
        {
            StepRows.Add(new SlotStepRowViewModel(step.Sequence, step.Name, Conclusion.None));
        }

        CurrentComparisonText = "比对信息：待执行";
        FinalConclusionText = "最终结果：待执行";
        LatestCommandText = "指令：待发送";
    }

    public void ResetForPlanChange()
    {
        IsSelected = false;
        Barcode = "";
        StepRows.Clear();
        StatusText = CanSelect ? "\u5F85\u9009\u62E9" : "\u7F3A\u5C11\u4E32\u53E3";
        StatusColor = NeutralColor;
        CardBackgroundColor = NeutralCardColor;
        CurrentComparisonText = "\u6BD4\u5BF9\u4FE1\u606F\uFF1A\u5F85\u6267\u884C";
        FinalConclusionText = "\u6700\u7EC8\u7ED3\u679C\uFF1A\u5F85\u6267\u884C";
        LatestCommandText = "\u6307\u4EE4\uFF1A\u5F85\u53D1\u9001";
    }

    public void ApplyStepProgress(SlotStepProgressSnapshot progress)
    {
        var row = StepRows.SingleOrDefault(row => row.Sequence == progress.Sequence);
        var statusText = FormatProgressStatus(progress);
        if (row is null)
        {
            row = new SlotStepRowViewModel(progress.Sequence, progress.StepName, progress.Conclusion);
            StepRows.Add(row);
        }

        if (progress.IsRunning && !string.IsNullOrWhiteSpace(statusText))
        {
            row.UpdateRunningStatus(progress.Conclusion, statusText);
        }
        else
        {
            row.UpdateConclusion(progress.Conclusion, progress.Message);
        }

        StatusText = "运行中";
        StatusColor = RunningColor;
        CardBackgroundColor = RunningCardColor;
        LatestCommandText = $"步骤：{progress.StepName} {(string.IsNullOrWhiteSpace(statusText) ? ToDisplayText(progress.Conclusion) : statusText)}";
        if (progress.StepName.Contains("比对", StringComparison.OrdinalIgnoreCase) ||
            progress.StepName.Contains("Compare", StringComparison.OrdinalIgnoreCase))
        {
            CurrentComparisonText = $"{progress.StepName}：{progress.Message}";
        }
    }

    private static string FormatProgressStatus(SlotStepProgressSnapshot progress)
    {
        if (progress.IsRunning && progress.Remaining is { } remaining)
        {
            return $"等待中 {Math.Max(0, remaining.TotalSeconds):0.0}s";
        }

        return "";
    }

    private static string BuildStepDetailText(StepRunTrace step)
    {
        IEnumerable<string> messageDetails = string.IsNullOrWhiteSpace(step.Message)
            ? []
            : [step.Message.Trim()];
        var comparisonDetails = step.Comparisons
            .Select(comparison => $"{comparison.LeftKey} / {comparison.RightKey}: {comparison.Message}");
        var measurementDetails = step.Measurements
            .Select(measurement => $"{measurement.Key} {measurement.NumericValue:0.##}{measurement.Unit}");

        return string.Join("  ", messageDetails.Concat(comparisonDetails).Concat(measurementDetails));
    }

    private static string ToDisplayText(Conclusion conclusion)
    {
        return conclusion switch
        {
            Conclusion.Pass => "通过",
            Conclusion.Fail => "失败",
            Conclusion.Warning => "警告",
            _ => "无结论"
        };
    }

    public static SlotCardViewModel CreateDemo(int slotNumber)
    {
        var slot = new StationSlot(
            Guid.NewGuid(),
            new Domain.ValueObjects.SlotNumber(slotNumber),
            new SlotCommunicationConfig(
                new Domain.ValueObjects.SerialPortName($"COM{slotNumber}"),
                new Domain.ValueObjects.ModbusAddress((byte)slotNumber),
                9600));

        return new SlotCardViewModel(slot);
    }

    partial void OnBarcodeChanged(string value)
    {
        OnPropertyChanged(nameof(BarcodeDisplay));
    }
}
