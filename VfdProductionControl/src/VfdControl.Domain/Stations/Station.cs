namespace VfdControl.Domain.Stations;

public sealed class Station
{
    private readonly List<StationSlot> _slots = [];

    public Station(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; }

    public string Name { get; }

    public IReadOnlyList<StationSlot> Slots => _slots;

    public void AddSlot(StationSlot slot) => _slots.Add(slot);

    public bool RemoveSlot(int slotNumber)
    {
        var slot = _slots.SingleOrDefault(item => item.Number.Value == slotNumber);
        return slot is not null && _slots.Remove(slot);
    }
}
