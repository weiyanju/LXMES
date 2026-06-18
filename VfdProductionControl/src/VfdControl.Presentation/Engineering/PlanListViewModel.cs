using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VfdControl.Application.Engineering;
using VfdControl.Domain.Plans;

namespace VfdControl.Presentation.Engineering;

public sealed partial class PlanListViewModel : ObservableObject
{
    private readonly ProcessPlanService _processPlanService;

    [ObservableProperty]
    private EngineeringPlanItemViewModel? _selectedPlan;

    [ObservableProperty]
    private string _newPlanName = "";

    [ObservableProperty]
    private string _statusMessage = "工程维护就绪。";

    public PlanListViewModel(ProcessPlanService processPlanService)
    {
        _processPlanService = processPlanService;
    }

    public ObservableCollection<EngineeringPlanItemViewModel> Plans { get; } = [];

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        Plans.Clear();
        var plans = await _processPlanService.ListPlansAsync(cancellationToken);
        foreach (var plan in plans)
        {
            Plans.Add(new EngineeringPlanItemViewModel(plan));
        }

        SelectedPlan ??= Plans.FirstOrDefault();
    }

    [RelayCommand]
    private async Task CreatePlanAsync(CancellationToken cancellationToken)
    {
        var result = await _processPlanService.CreatePlanAsync(NewPlanName, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            StatusMessage = result.Message;
            return;
        }

        NewPlanName = "";
        Plans.Add(new EngineeringPlanItemViewModel(result.Value));
        SelectedPlan = Plans.Last();
        StatusMessage = "测试方案已创建。";
    }
}

public sealed class EngineeringPlanItemViewModel
{
    public EngineeringPlanItemViewModel(ProcessPlan plan)
    {
        Plan = plan;
        Name = plan.Name;
        VersionCount = plan.Versions.Count;
        var latestVersion = plan.Versions
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefault();
        LastUpdatedDisplay = latestVersion is null
            ? "未发布"
            : $"最后更新：{latestVersion.CreatedAt.LocalDateTime:yyyy-MM-dd HH:mm}";
    }

    public ProcessPlan Plan { get; }

    public string Name { get; }

    public int VersionCount { get; }

    public string LastUpdatedDisplay { get; }
}
