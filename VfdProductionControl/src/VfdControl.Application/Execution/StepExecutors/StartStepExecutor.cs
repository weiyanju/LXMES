using VfdControl.Application.Abstractions;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;

namespace VfdControl.Application.Execution.StepExecutors;

public sealed class StartStepExecutor
{
    private readonly IDeviceCommandClient _deviceClient;

    public StartStepExecutor(IDeviceCommandClient deviceClient)
    {
        _deviceClient = deviceClient;
    }

    public Task<CommandResult> ExecuteAsync(DeviceRunContext context, ProcessStep step, CancellationToken ct)
    {
        var address = new DeviceAddress(context.Slot.Id, MeasurementSource.Vfd, step.Command.Target);
        return _deviceClient.WriteAsync(address, new WriteCommand("Start", step.Command.Value), ct);
    }
}
