using FluentAssertions;
using VfdControl.Application.Execution;
using VfdControl.Domain.Enums;
using VfdControl.Domain.ValueObjects;
using VfdControl.Infrastructure.Simulation;

namespace VfdControl.Infrastructure.Tests.Simulation;

public class SimulatedDeviceCommandClientTests
{
    [Fact]
    public async Task Writing_start_command_marks_slot_as_running()
    {
        var slotId = Guid.NewGuid();
        var scenario = new SimulationScenario();
        scenario.AddSlot(slotId);
        var client = new SimulatedDeviceCommandClient(scenario);

        var result = await client.WriteAsync(Address(slotId, MeasurementSource.Vfd, "Control"), new WriteCommand("Start"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        scenario.GetSlot(slotId).IsRunning.Should().BeTrue();
    }

    [Fact]
    public async Task Writing_stop_command_marks_slot_as_stopped()
    {
        var slotId = Guid.NewGuid();
        var scenario = new SimulationScenario();
        scenario.AddSlot(slotId).Start();
        var client = new SimulatedDeviceCommandClient(scenario);

        var result = await client.WriteAsync(Address(slotId, MeasurementSource.Vfd, "Control"), new WriteCommand("Stop"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        scenario.GetSlot(slotId).IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task Reading_vfd_voltage_returns_configured_value()
    {
        var slotId = Guid.NewGuid();
        var scenario = new SimulationScenario();
        scenario.SetMeasurement(slotId, MeasurementSource.Vfd, "Voltage", new MeasurementValue(220.5, "V", MeasurementSource.Vfd));
        var client = new SimulatedDeviceCommandClient(scenario);

        var result = await client.ReadMeasurementAsync(Address(slotId, MeasurementSource.Vfd, "Voltage"), new ReadCommand("Voltage"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new MeasurementValue(220.5, "V", MeasurementSource.Vfd));
    }

    [Fact]
    public async Task Reading_vfd_state_measurement_returns_3300_status_word()
    {
        var slotId = Guid.NewGuid();
        var scenario = new SimulationScenario();
        scenario.AddSlot(slotId);
        var client = new SimulatedDeviceCommandClient(scenario);

        var stopped = await client.ReadMeasurementAsync(Address(slotId, MeasurementSource.Vfd, "State"), new ReadCommand("State"), CancellationToken.None);
        await client.WriteAsync(Address(slotId, MeasurementSource.Vfd, "Control"), new WriteCommand("Start"), CancellationToken.None);
        var running = await client.ReadMeasurementAsync(Address(slotId, MeasurementSource.Vfd, "State"), new ReadCommand("State"), CancellationToken.None);

        stopped.Value.Should().Be(new MeasurementValue(3, "", MeasurementSource.Vfd));
        running.Value.Should().Be(new MeasurementValue(1, "", MeasurementSource.Vfd));
    }

    [Fact]
    public async Task Reading_instrument_voltage_returns_configured_value()
    {
        var slotId = Guid.NewGuid();
        var scenario = new SimulationScenario();
        scenario.SetMeasurement(slotId, MeasurementSource.Instrument, "Voltage", new MeasurementValue(219.8, "V", MeasurementSource.Instrument));
        var client = new SimulatedDeviceCommandClient(scenario);

        var result = await client.ReadMeasurementAsync(Address(slotId, MeasurementSource.Instrument, "Voltage"), new ReadCommand("Voltage"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new MeasurementValue(219.8, "V", MeasurementSource.Instrument));
    }

    [Fact]
    public async Task Configured_timeout_returns_failed_command_result()
    {
        var slotId = Guid.NewGuid();
        var scenario = new SimulationScenario();
        scenario.SetMeasurement(slotId, MeasurementSource.Vfd, "Voltage", new MeasurementValue(220.0, "V", MeasurementSource.Vfd));
        scenario.FailCommand(slotId, MeasurementSource.Vfd, "Voltage", "Timeout");
        var client = new SimulatedDeviceCommandClient(scenario);

        var result = await client.ReadMeasurementAsync(Address(slotId, MeasurementSource.Vfd, "Voltage"), new ReadCommand("Voltage"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Simulation.Timeout");
    }

    [Fact]
    public async Task Configured_string_returns_model_version_and_serial_text()
    {
        var slotId = Guid.NewGuid();
        var scenario = new SimulationScenario();
        scenario.SetString(slotId, MeasurementSource.Vfd, "Model", "VFD-X1");
        scenario.SetString(slotId, MeasurementSource.Vfd, "Version", "1.0.0");
        scenario.SetString(slotId, MeasurementSource.Vfd, "Serial", "SN202606010001");
        var client = new SimulatedDeviceCommandClient(scenario);

        var model = await client.ReadStringAsync(Address(slotId, MeasurementSource.Vfd, "Model"), new ReadStringCommand("Model"), CancellationToken.None);
        var version = await client.ReadStringAsync(Address(slotId, MeasurementSource.Vfd, "Version"), new ReadStringCommand("Version"), CancellationToken.None);
        var serial = await client.ReadStringAsync(Address(slotId, MeasurementSource.Vfd, "Serial"), new ReadStringCommand("Serial"), CancellationToken.None);

        model.Value.Should().Be("VFD-X1");
        version.Value.Should().Be("1.0.0");
        serial.Value.Should().Be("SN202606010001");
    }

    private static DeviceAddress Address(Guid slotId, MeasurementSource source, string endpoint)
    {
        return new DeviceAddress(slotId, source, endpoint);
    }
}
