using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Infrastructure.Simulation;

public sealed class SimulatedDeviceCommandClient : IDeviceCommandClient
{
    private readonly SimulationScenario _scenario;

    public SimulatedDeviceCommandClient(SimulationScenario scenario)
    {
        _scenario = scenario;
    }

    public Task<CommandResult<MeasurementValue>> ReadMeasurementAsync(DeviceAddress address, ReadCommand command, CancellationToken ct)
    {
        if (_scenario.TryGetFailure(address.SlotId, address.Source, command.PointName, out var failureCode))
        {
            return Task.FromResult(CommandResult<MeasurementValue>.Failure("Simulated command failure.", $"Simulation.{failureCode}"));
        }

        if (address.Source == Domain.Enums.MeasurementSource.Vfd
            && command.PointName.Equals("State", StringComparison.OrdinalIgnoreCase))
        {
            var slot = _scenario.GetSlot(address.SlotId);
            var statusWord = slot.IsRunning ? 1 : 3;
            return Task.FromResult(CommandResult<MeasurementValue>.Success(new MeasurementValue(statusWord, string.Empty, address.Source)));
        }

        return _scenario.TryGetMeasurement(address.SlotId, address.Source, command.PointName, out var value)
            ? Task.FromResult(CommandResult<MeasurementValue>.Success(value))
            : Task.FromResult(CommandResult<MeasurementValue>.Failure("Simulated measurement is not configured.", "Simulation.MeasurementMissing"));
    }

    public Task<CommandResult<string>> ReadStringAsync(DeviceAddress address, ReadStringCommand command, CancellationToken ct)
    {
        if (_scenario.TryGetFailure(address.SlotId, address.Source, command.PointName, out var failureCode))
        {
            return Task.FromResult(CommandResult<string>.Failure("Simulated command failure.", $"Simulation.{failureCode}"));
        }

        if (address.Source == Domain.Enums.MeasurementSource.Vfd
            && command.PointName.Equals("State", StringComparison.OrdinalIgnoreCase))
        {
            var slot = _scenario.GetSlot(address.SlotId);
            return Task.FromResult(CommandResult<string>.Success(slot.IsRunning ? "RUNNING" : "STOPPED"));
        }

        return _scenario.TryGetString(address.SlotId, address.Source, command.PointName, out var value)
            ? Task.FromResult(CommandResult<string>.Success(value))
            : Task.FromResult(CommandResult<string>.Failure("Simulated string is not configured.", "Simulation.StringMissing"));
    }

    public Task<CommandResult> WriteAsync(DeviceAddress address, WriteCommand command, CancellationToken ct)
    {
        if (_scenario.TryGetFailure(address.SlotId, address.Source, command.CommandName, out var failureCode))
        {
            return Task.FromResult(CommandResult.Failure("Simulated command failure.", $"Simulation.{failureCode}"));
        }

        var slot = _scenario.GetSlot(address.SlotId);
        if (command.CommandName.Equals("Start", StringComparison.OrdinalIgnoreCase))
        {
            slot.Start();
        }
        else if (command.CommandName.Equals("Stop", StringComparison.OrdinalIgnoreCase))
        {
            slot.Stop();
        }

        return Task.FromResult(CommandResult.Success());
    }
}
