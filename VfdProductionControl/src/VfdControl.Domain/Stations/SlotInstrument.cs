namespace VfdControl.Domain.Stations;

public sealed class SlotInstrument
{
    private readonly List<InstrumentPoint> _points = [];

    public SlotInstrument(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; }

    public string Name { get; }

    public IReadOnlyList<InstrumentPoint> Points => _points;

    public void AddPoint(InstrumentPoint point) => _points.Add(point);
}
