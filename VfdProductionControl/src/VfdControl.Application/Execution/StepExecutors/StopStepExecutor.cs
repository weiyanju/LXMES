using VfdControl.Application.Abstractions;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;

namespace VfdControl.Application.Execution.StepExecutors;

public sealed class StopStepExecutor
{
    private readonly IDeviceCommandClient _deviceClient;

    public StopStepExecutor(IDeviceCommandClient deviceClient)
    {
        _deviceClient = deviceClient;
    }

    public Task<CommandResult> ExecuteAsync(DeviceRunContext context, ProcessStep step, CancellationToken ct)
    {
        var address = new DeviceAddress(context.Slot.Id, MeasurementSource.Vfd, step.Command.Target);
        return _deviceClient.WriteAsync(address, new WriteCommand("Stop", step.Command.Value), ct);
    }

    public Task<CommandResult> ExecuteSafetyStopAsync(DeviceRunContext context, CancellationToken ct)
    {
        var address = new DeviceAddress(context.Slot.Id, MeasurementSource.Vfd, "Vfd:Control");
        return _deviceClient.WriteAsync(address, new WriteCommand("Stop", "5"), ct);
    }
}
