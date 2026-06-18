using VfdControl.Application.Execution;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Application.Abstractions;

public interface IDeviceCommandClient
{
    Task<CommandResult<MeasurementValue>> ReadMeasurementAsync(DeviceAddress address, ReadCommand command, CancellationToken ct);

    Task<CommandResult<string>> ReadStringAsync(DeviceAddress address, ReadStringCommand command, CancellationToken ct);

    Task<CommandResult> WriteAsync(DeviceAddress address, WriteCommand command, CancellationToken ct);
}
