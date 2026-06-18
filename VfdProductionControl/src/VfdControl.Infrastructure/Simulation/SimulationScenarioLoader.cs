using VfdControl.Domain.Enums;
using VfdControl.Domain.Stations;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Infrastructure.Simulation;

public static class SimulationScenarioLoader
{
    public static SimulationScenario CreateDefault(int slotCount)
    {
        var scenario = new SimulationScenario();

        for (var index = 1; index <= slotCount; index++)
        {
            ConfigureSlot(scenario, Guid.NewGuid(), index);
        }

        return scenario;
    }

    public static SimulationScenario CreateDefault(IEnumerable<StationSlot> slots)
    {
        var scenario = new SimulationScenario();

        foreach (var slot in slots.OrderBy(slot => slot.Number.Value))
        {
            ConfigureSlot(scenario, slot.Id, slot.Number.Value);
        }

        return scenario;
    }

    private static void ConfigureSlot(SimulationScenario scenario, Guid slotId, int slotNumber)
    {
        scenario.AddSlot(slotId);
        scenario.SetMeasurement(slotId, MeasurementSource.Vfd, "Voltage", new MeasurementValue(220.0, "V", MeasurementSource.Vfd));
        scenario.SetMeasurement(slotId, MeasurementSource.Instrument, "Voltage", new MeasurementValue(219.5, "V", MeasurementSource.Instrument));
        scenario.SetString(slotId, MeasurementSource.Vfd, "Model", "VFD-X1");
        scenario.SetString(slotId, MeasurementSource.Vfd, "Version", "1.0.0");
        scenario.SetString(slotId, MeasurementSource.Vfd, "Serial", $"SIM-{slotNumber:0000}");
    }
}
