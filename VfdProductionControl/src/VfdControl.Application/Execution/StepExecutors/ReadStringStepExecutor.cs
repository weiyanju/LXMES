using VfdControl.Application.Abstractions;
using VfdControl.Domain.Plans;

namespace VfdControl.Application.Execution.StepExecutors;

public sealed class ReadStringStepExecutor
{
    private readonly IDeviceCommandClient _deviceClient;

    public ReadStringStepExecutor(IDeviceCommandClient deviceClient)
    {
        _deviceClient = deviceClient;
    }

    public Task<CommandResult<string>> ExecuteAsync(DeviceRunContext context, ProcessStep step, CancellationToken ct)
    {
        var target = StepTarget.Parse(step.Command.Target);
        var address = new DeviceAddress(context.Slot.Id, target.Source, target.PointName);
        return _deviceClient.ReadStringAsync(address, new ReadStringCommand(target.PointName), ct);
    }
}
