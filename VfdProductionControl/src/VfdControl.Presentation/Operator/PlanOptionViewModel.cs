using VfdControl.Domain.Plans;
using VfdControl.Application.Operator;

namespace VfdControl.Presentation.Operator;

public sealed class PlanOptionViewModel
{
    public PlanOptionViewModel(ExecutablePlanOption option)
    {
        PlanVersion = option.Version;
        DisplayName = $"{option.PlanName} - v{option.Version.VersionNumber}";
    }

    public ProcessPlanVersion PlanVersion { get; }

    public string DisplayName { get; }
}
