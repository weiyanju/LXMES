using VfdControl.Application.Abstractions;
using VfdControl.Application.Common;
using VfdControl.Domain.Plans;

namespace VfdControl.Application.Engineering;

public sealed class ProcessPlanService
{
    private readonly IProcessPlanRepository _processPlanRepository;
    private readonly WorkflowDefinitionService _workflowDefinitionService;

    public ProcessPlanService(IProcessPlanRepository processPlanRepository)
        : this(processPlanRepository, new WorkflowDefinitionService())
    {
    }

    public ProcessPlanService(
        IProcessPlanRepository processPlanRepository,
        WorkflowDefinitionService workflowDefinitionService)
    {
        _processPlanRepository = processPlanRepository;
        _workflowDefinitionService = workflowDefinitionService;
    }

    public Task<IReadOnlyList<ProcessPlan>> ListPlansAsync(CancellationToken ct)
    {
        return _processPlanRepository.ListAsync(ct);
    }

    public async Task<AppResult<ProcessPlan>> CreatePlanAsync(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return AppResult<ProcessPlan>.Failure("方案名称不能为空。", "ProcessPlan.NameRequired");
        }

        var plan = new ProcessPlan(Guid.NewGuid(), name.Trim());
        await _processPlanRepository.SaveAsync(plan, ct);
        return AppResult<ProcessPlan>.Success(plan);
    }

    public async Task<AppResult<ProcessPlanVersion>> SaveNewVersionAsync(
        Guid planId,
        IReadOnlyList<ProcessStep> steps,
        bool isExecutable,
        CancellationToken ct)
    {
        var plan = await _processPlanRepository.GetAsync(planId, ct);
        if (plan is null)
        {
            return AppResult<ProcessPlanVersion>.Failure("测试方案不存在。", "ProcessPlan.NotFound");
        }

        var nextVersionNumber = plan.Versions.Count == 0
            ? 1
            : plan.Versions.Max(version => version.VersionNumber) + 1;

        var version = new ProcessPlanVersion(Guid.NewGuid(), nextVersionNumber, isExecutable);
        foreach (var step in steps.OrderBy(step => step.Sequence))
        {
            version.AddStep(_workflowDefinitionService.CloneStep(step, step.Sequence));
        }

        plan.AddVersion(version);
        await _processPlanRepository.SaveAsync(plan, ct);
        return AppResult<ProcessPlanVersion>.Success(version);
    }

    public async Task<AppResult<ProcessPlanVersion>> MarkVersionExecutableAsync(
        Guid planId,
        Guid versionId,
        CancellationToken ct)
    {
        var plan = await _processPlanRepository.GetAsync(planId, ct);
        var version = plan?.Versions.SingleOrDefault(item => item.Id == versionId);
        if (plan is null || version is null)
        {
            return AppResult<ProcessPlanVersion>.Failure("方案版本不存在。", "ProcessPlanVersion.NotFound");
        }

        version.MarkExecutable();
        await _processPlanRepository.SaveAsync(plan, ct);
        return AppResult<ProcessPlanVersion>.Success(version);
    }

    public async Task<AppResult<ProcessPlanVersion>> CloneVersionAsync(
        Guid planId,
        Guid sourceVersionId,
        CancellationToken ct)
    {
        var plan = await _processPlanRepository.GetAsync(planId, ct);
        var sourceVersion = plan?.Versions.SingleOrDefault(version => version.Id == sourceVersionId);
        if (plan is null || sourceVersion is null)
        {
            return AppResult<ProcessPlanVersion>.Failure("源方案版本不存在。", "ProcessPlanVersion.NotFound");
        }

        return await SaveNewVersionAsync(
            planId,
            sourceVersion.Steps,
            isExecutable: false,
            ct);
    }
}
