using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VfdControl.Application.Execution;
using VfdControl.Application.Traceability;
using VfdControl.Domain.Enums;

namespace VfdControl.Presentation.Traceability;

public sealed partial class ExecutionHistoryViewModel : ObservableObject
{
    private readonly TraceabilityQueryService _queryService;
    private readonly DeviceRunTraceViewModel _detailViewModel;

    [ObservableProperty]
    private DateTime? fromDate = DateTime.Today.AddDays(-1);

    [ObservableProperty]
    private DateTime? toDate = DateTime.Today.AddDays(1);

    [ObservableProperty]
    private string barcodeFilter = string.Empty;

    [ObservableProperty]
    private ConclusionFilterOption selectedConclusion;

    [ObservableProperty]
    private string statusMessage = "输入筛选条件后查询历史记录。";

    public ExecutionHistoryViewModel(TraceabilityQueryService queryService, DeviceRunTraceViewModel detailViewModel)
    {
        _queryService = queryService;
        _detailViewModel = detailViewModel;
        ConclusionOptions =
        [
            new ConclusionFilterOption(null, "全部结论"),
            new ConclusionFilterOption(Conclusion.Pass, "通过"),
            new ConclusionFilterOption(Conclusion.Fail, "失败"),
            new ConclusionFilterOption(Conclusion.Warning, "警告")
        ];
        selectedConclusion = ConclusionOptions[0];
    }

    public ObservableCollection<StationSessionRowViewModel> Sessions { get; } = [];

    public ObservableCollection<DeviceRunHistoryRowViewModel> DeviceRuns { get; } = [];

    public IReadOnlyList<ConclusionFilterOption> ConclusionOptions { get; }

    [RelayCommand]
    public Task SearchAsync()
    {
        return LoadAsync();
    }

    public async Task LoadAsync()
    {
        Sessions.Clear();
        DeviceRuns.Clear();

        var sessionQuery = new TraceabilitySessionQuery(
            DateToOffset(FromDate),
            DateToOffset(ToDate),
            Normalize(BarcodeFilter),
            SelectedConclusion.Value);
        var runQuery = new DeviceRunQuery(
            DateToOffset(FromDate),
            DateToOffset(ToDate),
            Normalize(BarcodeFilter),
            SelectedConclusion.Value);

        var sessions = await _queryService.QuerySessionsAsync(sessionQuery, CancellationToken.None);
        foreach (var session in sessions)
        {
            Sessions.Add(new StationSessionRowViewModel(session));
        }

        var runs = await _queryService.QueryDeviceRunsAsync(runQuery, CancellationToken.None);
        foreach (var run in runs)
        {
            DeviceRuns.Add(new DeviceRunHistoryRowViewModel(run));
        }

        StatusMessage = $"已加载 {Sessions.Count} 个会话，{DeviceRuns.Count} 条设备记录。";
    }

    [RelayCommand]
    public async Task OpenDeviceRunAsync(DeviceRunHistoryRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        var trace = await _queryService.GetDeviceRunTraceAsync(row.DeviceRunId, CancellationToken.None);
        if (trace is null)
        {
            StatusMessage = "未找到该设备运行详情。";
            return;
        }

        _detailViewModel.Load(trace);
        StatusMessage = $"已打开 {trace.Barcode} 的追溯详情。";
    }

    private static DateTimeOffset? DateToOffset(DateTime? date)
    {
        return date is null ? null : new DateTimeOffset(DateTime.SpecifyKind(date.Value, DateTimeKind.Local));
    }

    private static string? Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }
}

public sealed record ConclusionFilterOption(Conclusion? Value, string DisplayName);

public sealed class StationSessionRowViewModel
{
    public StationSessionRowViewModel(StationSessionSummary session)
    {
        SessionId = session.SessionId;
        OperatorCode = session.OperatorCode;
        StartedAt = session.StartedAt.ToString("yyyy-MM-dd HH:mm:ss");
        Conclusion = session.Conclusion is null ? "无记录" : ConclusionDisplay(session.Conclusion.Value);
        DeviceRunCount = session.DeviceRunCount;
    }

    public Guid SessionId { get; }

    public string OperatorCode { get; }

    public string StartedAt { get; }

    public string Conclusion { get; }

    public int DeviceRunCount { get; }

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

public sealed class DeviceRunHistoryRowViewModel
{
    public DeviceRunHistoryRowViewModel(DeviceRunSummary run)
    {
        DeviceRunId = run.DeviceRunId;
        Barcode = run.Barcode;
        StartedAt = run.StartedAt == DateTimeOffset.MinValue ? "-" : run.StartedAt.ToString("yyyy-MM-dd HH:mm:ss");
        Conclusion = run.Conclusion switch
        {
            Domain.Enums.Conclusion.Pass => "通过",
            Domain.Enums.Conclusion.Fail => "失败",
            Domain.Enums.Conclusion.Warning => "警告",
            _ => run.Conclusion.ToString()
        };
    }

    public Guid DeviceRunId { get; }

    public string Barcode { get; }

    public string StartedAt { get; }

    public string Conclusion { get; }
}
