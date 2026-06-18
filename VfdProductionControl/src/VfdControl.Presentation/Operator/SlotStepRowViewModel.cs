using CommunityToolkit.Mvvm.ComponentModel;
using VfdControl.Domain.Enums;

namespace VfdControl.Presentation.Operator;

public sealed partial class SlotStepRowViewModel : ObservableObject
{
    [ObservableProperty]
    private Conclusion _conclusion;

    [ObservableProperty]
    private string _detailText = "";

    private string? _statusTextOverride;

    public SlotStepRowViewModel(int sequence, string stepName, Conclusion conclusion, string detailText = "")
    {
        Sequence = sequence;
        StepName = stepName;
        Conclusion = conclusion;
        DetailText = detailText;
    }

    public int Sequence { get; }

    public string StepName { get; }

    public string DisplayName => $"{Sequence}. {StepName}";

    public string ConclusionText => string.IsNullOrWhiteSpace(_statusTextOverride)
        ? Conclusion switch
        {
            Conclusion.Pass => "通过",
            Conclusion.Fail => "失败",
            Conclusion.Warning => "警告",
            _ => "待执行"
        }
        : _statusTextOverride;

    public string StatusColor => Conclusion switch
    {
        Conclusion.Pass => "#DFF5E9",
        Conclusion.Fail => "#F9DEDE",
        Conclusion.Warning => "#FFF2CC",
        _ => "#E8F2F7"
    };

    public void UpdateConclusion(Conclusion conclusion, string? detailText = null)
    {
        _statusTextOverride = null;
        Conclusion = conclusion;
        DetailText = string.IsNullOrWhiteSpace(detailText) ? "" : detailText.Trim();
        OnPropertyChanged(nameof(ConclusionText));
    }

    public void UpdateRunningStatus(Conclusion conclusion, string statusText)
    {
        _statusTextOverride = statusText;
        Conclusion = conclusion;
        DetailText = "";
        OnPropertyChanged(nameof(ConclusionText));
    }

    partial void OnConclusionChanged(Conclusion value)
    {
        OnPropertyChanged(nameof(ConclusionText));
        OnPropertyChanged(nameof(StatusColor));
    }
}
