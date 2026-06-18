using VfdControl.Application.Abstractions;
using VfdControl.Application.Common;
using VfdControl.Application.Execution;
using VfdControl.Domain.Plans;
using VfdControl.Domain.Stations;

namespace VfdControl.Application.Operator;

public sealed class ProductionRunService
{
    private readonly ISlotScheduler _slotScheduler;

    public ProductionRunService(ISlotScheduler slotScheduler)
    {
        _slotScheduler = slotScheduler;
    }

    public async Task<AppResult<StationSessionResult>> StartAsync(
        OperatorSession operatorSession,
        Station station,
        ProcessPlanVersion planVersion,
        IReadOnlyList<SlotBarcodeBinding> bindings,
        CancellationToken ct,
        Action<SlotStepProgressSnapshot>? progressHandler = null)
    {
        if (!planVersion.IsExecutable)
        {
            return AppResult<StationSessionResult>.Failure("Plan version is not executable.", "ProductionRun.PlanNotExecutable");
        }

        var context = new StationSessionContext(
            Guid.NewGuid(),
            station,
            operatorSession.EmployeeCode,
            planVersion,
            bindings,
            progressHandler);

        var result = await _slotScheduler.RunAsync(context, ct);
        return AppResult<StationSessionResult>.Success(result);
    }
}
