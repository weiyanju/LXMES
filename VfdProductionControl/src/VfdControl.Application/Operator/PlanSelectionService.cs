using VfdControl.Application.Abstractions;
using VfdControl.Domain.Plans;

namespace VfdControl.Application.Operator;

public sealed class PlanSelectionService
{
    private readonly IProcessPlanRepository _processPlanRepository;

    public PlanSelectionService(IProcessPlanRepository processPlanRepository)
    {
        _processPlanRepository = processPlanRepository;
    }

    public Task<IReadOnlyList<ProcessPlanVersion>> GetExecutablePlansAsync(CancellationToken ct)
    {
        return _processPlanRepository.ListExecutableVersionsAsync(ct);
    }

    public async Task<IReadOnlyList<ExecutablePlanOption>> GetExecutablePlanOptionsAsync(CancellationToken ct)
    {
        var plans = await _processPlanRepository.ListAsync(ct);
        return plans
            .SelectMany(plan => plan.Versions
                .Where(version => version.IsExecutable)
                .Select(version => new ExecutablePlanOption(plan.Name, version)))
            .ToList();
    }
}

public sealed record ExecutablePlanOption(string PlanName, ProcessPlanVersion Version);
