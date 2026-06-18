using VfdControl.Domain.Enums;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Infrastructure.Simulation;

public sealed class SimulationScenario
{
    private readonly Dictionary<Guid, SimulatedSlotState> _slots = [];
    private readonly Dictionary<(Guid SlotId, MeasurementSource Source, string PointName), MeasurementValue> _measurements = [];
    private readonly Dictionary<(Guid SlotId, MeasurementSource Source, string PointName), string> _strings = [];
    private readonly Dictionary<(Guid SlotId, MeasurementSource Source, string CommandName), string> _failures = [];

    public SimulatedSlotState AddSlot(Guid slotId)
    {
        if (!_slots.TryGetValue(slotId, out var state))
        {
            state = new SimulatedSlotState(slotId);
            _slots[slotId] = state;
        }

        return state;
    }

    public SimulatedSlotState GetSlot(Guid slotId)
    {
        return AddSlot(slotId);
    }

    public void SetMeasurement(Guid slotId, MeasurementSource source, string pointName, MeasurementValue value)
    {
        AddSlot(slotId);
        _measurements[(slotId, source, pointName)] = value;
    }

    public bool TryGetMeasurement(Guid slotId, MeasurementSource source, string pointName, out MeasurementValue value)
    {
        return _measurements.TryGetValue((slotId, source, pointName), out value!);
    }

    public void SetString(Guid slotId, MeasurementSource source, string pointName, string value)
    {
        AddSlot(slotId);
        _strings[(slotId, source, pointName)] = value;
    }

    public bool TryGetString(Guid slotId, MeasurementSource source, string pointName, out string value)
    {
        return _strings.TryGetValue((slotId, source, pointName), out value!);
    }

    public void FailCommand(Guid slotId, MeasurementSource source, string commandName, string failureCode)
    {
        AddSlot(slotId);
        _failures[(slotId, source, commandName)] = failureCode;
    }

    public bool TryGetFailure(Guid slotId, MeasurementSource source, string commandName, out string failureCode)
    {
        return _failures.TryGetValue((slotId, source, commandName), out failureCode!);
    }
}
