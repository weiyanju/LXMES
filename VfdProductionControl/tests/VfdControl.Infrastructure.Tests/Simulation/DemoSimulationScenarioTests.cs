using FluentAssertions;
using VfdControl.Application.Execution;
using VfdControl.Domain.Enums;
using VfdControl.Infrastructure.Seed;
using VfdControl.Infrastructure.Simulation;

namespace VfdControl.Infrastructure.Tests.Simulation;

public class DemoSimulationScenarioTests
{
    [Fact]
    public async Task Default_scenario_supports_seeded_station_slots()
    {
        var station = DemoDataSeeder.Create().Stations.Single();
        var scenario = SimulationScenarioLoader.CreateDefault(station.Slots);
        var client = new SimulatedDeviceCommandClient(scenario);

        foreach (var slot in station.Slots)
        {
            var address = new DeviceAddress(slot.Id, MeasurementSource.Vfd, "Control");
            var start = await client.WriteAsync(address, new WriteCommand("Start"), CancellationToken.None);
            var state = await client.ReadStringAsync(new DeviceAddress(slot.Id, MeasurementSource.Vfd, "State"), new ReadStringCommand("State"), CancellationToken.None);
            var vfdVoltage = await client.ReadMeasurementAsync(new DeviceAddress(slot.Id, MeasurementSource.Vfd, "Voltage"), new ReadCommand("Voltage"), CancellationToken.None);
            var instrumentVoltage = await client.ReadMeasurementAsync(new DeviceAddress(slot.Id, MeasurementSource.Instrument, "Voltage"), new ReadCommand("Voltage"), CancellationToken.None);

            start.IsSuccess.Should().BeTrue();
            state.Value.Should().Be("RUNNING");
            vfdVoltage.Value!.NumericValue.Should().Be(220.0);
            instrumentVoltage.Value!.NumericValue.Should().Be(219.5);
        }
    }
}
