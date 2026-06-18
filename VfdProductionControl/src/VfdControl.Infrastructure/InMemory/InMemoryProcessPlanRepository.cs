using VfdControl.Application.Abstractions;
using VfdControl.Domain.Plans;

namespace VfdControl.Infrastructure.InMemory;

public sealed class InMemoryProcessPlanRepository : IProcessPlanRepository
{
    private readonly Dictionary<Guid, ProcessPlan> _plans;

    public InMemoryProcessPlanRepository(IEnumerable<ProcessPlan>? plans = null)
    {
        _plans = (plans ?? []).ToDictionary(plan => plan.Id);
    }

    public Task<IReadOnlyList<ProcessPlan>> ListAsync(CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<ProcessPlan>>(_plans.Values.ToList());
    }

    public Task<IReadOnlyList<ProcessPlanVersion>> ListExecutableVersionsAsync(CancellationToken ct)
    {
        var versions = _plans.Values
            .SelectMany(plan => plan.Versions)
            .Where(version => version.IsExecutable)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProcessPlanVersion>>(versions);
    }

    public Task<ProcessPlan?> GetAsync(Guid planId, CancellationToken ct)
    {
        _plans.TryGetValue(planId, out var plan);
        return Task.FromResult(plan);
    }

    public Task SaveAsync(ProcessPlan plan, CancellationToken ct)
    {
        _plans[plan.Id] = plan;
        return Task.CompletedTask;
    }
}
