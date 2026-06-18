using VfdControl.Application.Abstractions;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Application.Execution.StepExecutors;

public sealed class ReadMeasurementStepExecutor
{
    private readonly IDeviceCommandClient _deviceClient;

    public ReadMeasurementStepExecutor(IDeviceCommandClient deviceClient)
    {
        _deviceClient = deviceClient;
    }

    public Task<CommandResult<MeasurementValue>> ExecuteAsync(DeviceRunContext context, ProcessStep step, CancellationToken ct)
    {
        var target = StepTarget.Parse(step.Command.Target);
        var address = new DeviceAddress(context.Slot.Id, target.Source, target.PointName);
        return _deviceClient.ReadMeasurementAsync(address, new ReadCommand(target.PointName), ct);
    }
}
