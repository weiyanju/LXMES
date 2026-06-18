using System.Collections.ObjectModel;
using VfdControl.Application.Execution;
using VfdControl.Application.Traceability;
using VfdControl.Domain.Enums;

namespace VfdControl.Presentation.Traceability;

public sealed class DeviceRunTraceViewModel
{
    public string Title { get; private set; } = "未选择记录";

    public string Summary { get; private set; } = "请选择一条设备运行记录查看详情。";

    public ObservableCollection<StepRunTraceRowViewModel> Steps { get; } = [];

    public void Load(DeviceRunTrace trace)
    {
        Title = trace.Barcode;
        Summary = $"{ConclusionDisplay(trace.Conclusion)} · {trace.StartedAt:yyyy-MM-dd HH:mm:ss}";
        Steps.Clear();
        foreach (var step in trace.Steps)
        {
            Steps.Add(new StepRunTraceRowViewModel(step));
        }
    }

    private static string ConclusionDisplay(Conclusion conclusion)
    {
        return conclusion switch
        {
            VfdControl.Domain.Enums.Conclusion.Pass => "通过",
            VfdControl.Domain.Enums.Conclusion.Fail => "失败",
            VfdControl.Domain.Enums.Conclusion.Warning => "警告",
            _ => conclusion.ToString()
        };
    }
}

public sealed class StepRunTraceRowViewModel
{
    public StepRunTraceRowViewModel(StepRunTrace trace)
    {
        Sequence = trace.Sequence;
        StepName = trace.StepName;
        Conclusion = ConclusionDisplay(trace.Conclusion);
        Measurements = new ObservableCollection<MeasurementTraceRowViewModel>(
            trace.Measurements.Select(measurement => new MeasurementTraceRowViewModel(measurement)));
        Comparisons = new ObservableCollection<ComparisonTraceRowViewModel>(
            trace.Comparisons.Select(comparison => new ComparisonTraceRowViewModel(comparison)));
        CommandTraces = new ObservableCollection<CommandTraceRowViewModel>(
            trace.CommandTraces.Select(command => new CommandTraceRowViewModel(command)));
    }

    public int Sequence { get; }

    public string StepName { get; }

    public string Conclusion { get; }

    public ObservableCollection<MeasurementTraceRowViewModel> Measurements { get; }

    public ObservableCollection<ComparisonTraceRowViewModel> Comparisons { get; }

    public ObservableCollection<CommandTraceRowViewModel> CommandTraces { get; }

    private static string ConclusionDisplay(Conclusion conclusion)
    {
        return conclusion switch
        {
            VfdControl.Domain.Enums.Conclusion.Pass => "通过",
            VfdControl.Domain.Enums.Conclusion.Fail => "失败",
            VfdControl.Domain.Enums.Conclusion.Warning => "警告",
            _ => conclusion.ToString()
        };
    }
}

public sealed class MeasurementTraceRowViewModel
{
    public MeasurementTraceRowViewModel(MeasurementTrace measurement)
    {
        Key = measurement.Key;
        Source = measurement.Source.ToString();
        DisplayValue = $"{measurement.NumericValue:0.###} {measurement.Unit}";
    }

    public string Key { get; }

    public string Source { get; }

    public string DisplayValue { get; }
}

public sealed class ComparisonTraceRowViewModel
{
    public ComparisonTraceRowViewModel(ComparisonTrace comparison)
    {
        Pair = $"{comparison.LeftKey} / {comparison.RightKey}";
        Conclusion = comparison.Conclusion switch
        {
            Domain.Enums.Conclusion.Pass => "通过",
            Domain.Enums.Conclusion.Fail => "失败",
            Domain.Enums.Conclusion.Warning => "警告",
            _ => comparison.Conclusion.ToString()
        };
        Message = comparison.Message;
    }

    public string Pair { get; }

    public string Conclusion { get; }

    public string Message { get; }
}

public sealed class CommandTraceRowViewModel
{
    public CommandTraceRowViewModel(CommandTraceSnapshot trace)
    {
        CommandName = trace.CommandName;
        RequestJson = trace.RequestJson;
        ResponseJson = trace.ResponseJson;
        Status = trace.IsSuccess ? "成功" : "失败";
        CreatedAt = trace.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public string CommandName { get; }

    public string RequestJson { get; }

    public string ResponseJson { get; }

    public string Status { get; }

    public string CreatedAt { get; }
}
