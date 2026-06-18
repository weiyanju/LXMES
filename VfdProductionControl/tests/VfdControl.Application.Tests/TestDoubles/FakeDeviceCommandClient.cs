using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;
using VfdControl.Domain.Enums;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Application.Tests.TestDoubles;

public sealed class FakeDeviceCommandClient : IDeviceCommandClient
{
    private readonly Dictionary<(Guid SlotId, MeasurementSource Source, string PointName), MeasurementValue> _measurements = [];
    private readonly Dictionary<(Guid SlotId, MeasurementSource Source, string PointName), string> _strings = [];
    private readonly HashSet<string> _failingCommands = [];

    public List<WriteLogEntry> WriteLog { get; } = [];

    public void SetMeasurement(Guid slotId, MeasurementSource source, string pointName, MeasurementValue value)
    {
        _measurements[(slotId, source, pointName)] = value;
    }

    public void SetString(Guid slotId, MeasurementSource source, string pointName, string value)
    {
        _strings[(slotId, source, pointName)] = value;
    }

    public void FailCommand(string commandName)
    {
        _failingCommands.Add(commandName);
    }

    public Task<CommandResult<MeasurementValue>> ReadMeasurementAsync(DeviceAddress address, ReadCommand command, CancellationToken ct)
    {
        if (_failingCommands.Contains(command.PointName))
        {
            return Task.FromResult(CommandResult<MeasurementValue>.Failure("Configured measurement failure.", "Fake.MeasurementFailure"));
        }

        return _measurements.TryGetValue((address.SlotId, address.Source, command.PointName), out var value)
            ? Task.FromResult(CommandResult<MeasurementValue>.Success(value))
            : Task.FromResult(CommandResult<MeasurementValue>.Failure("Measurement response is not configured.", "Fake.MeasurementMissing"));
    }

    public Task<CommandResult<string>> ReadStringAsync(DeviceAddress address, ReadStringCommand command, CancellationToken ct)
    {
        if (_failingCommands.Contains(command.PointName))
        {
            return Task.FromResult(CommandResult<string>.Failure("Configured string failure.", "Fake.StringFailure"));
        }

        return _strings.TryGetValue((address.SlotId, address.Source, command.PointName), out var value)
            ? Task.FromResult(CommandResult<string>.Success(value))
            : Task.FromResult(CommandResult<string>.Failure("String response is not configured.", "Fake.StringMissing"));
    }

    public Task<CommandResult> WriteAsync(DeviceAddress address, WriteCommand command, CancellationToken ct)
    {
        WriteLog.Add(new WriteLogEntry(address.SlotId, address.Source, command.CommandName, command.Value));
        var request = $$"""{"source":"{{address.Source}}","endpoint":"{{address.EndpointName}}","command":"{{command.CommandName}}","value":"{{command.Value}}"}""";

        return _failingCommands.Contains(command.CommandName)
            ? Task.FromResult(CommandResult.Failure(
                "Configured write failure.",
                "Fake.WriteFailure",
                request,
                """{"result":"Failure"}"""))
            : Task.FromResult(CommandResult.Success(
                requestJson: request,
                responseJson: """{"result":"Success"}"""));
    }
}

public sealed record WriteLogEntry(
    Guid SlotId,
    MeasurementSource Source,
    string CommandName,
    string? Value);
