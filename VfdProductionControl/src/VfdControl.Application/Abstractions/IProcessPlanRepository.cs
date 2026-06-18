using VfdControl.Domain.Plans;

namespace VfdControl.Application.Abstractions;

public interface IProcessPlanRepository
{
    Task<IReadOnlyList<ProcessPlan>> ListAsync(CancellationToken ct);

    Task<IReadOnlyList<ProcessPlanVersion>> ListExecutableVersionsAsync(CancellationToken ct);

    Task<ProcessPlan?> GetAsync(Guid planId, CancellationToken ct);

    Task SaveAsync(ProcessPlan plan, CancellationToken ct);
}
