namespace VfdControl.Infrastructure.Simulation;

public sealed class SimulatedSlotState
{
    public SimulatedSlotState(Guid slotId)
    {
        SlotId = slotId;
    }

    public Guid SlotId { get; }

    public bool IsRunning { get; private set; }

    public void Start() => IsRunning = true;

    public void Stop() => IsRunning = false;
}
