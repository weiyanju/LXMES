using VfdControl.Domain.ValueObjects;

namespace VfdControl.Domain.Stations;

public sealed class StationSlot
{
    private readonly List<SlotInstrument> _instruments = [];

    public StationSlot(Guid id, SlotNumber number, SlotCommunicationConfig communicationConfig, string? displayName = null)
    {
        Id = id;
        Number = number;
        CommunicationConfig = communicationConfig;
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? $"{number.Value} \u53F7\u69FD\u4F4D"
            : displayName.Trim();
    }

    public Guid Id { get; }

    public SlotNumber Number { get; }

    public string DisplayName { get; private set; }

    public SlotCommunicationConfig CommunicationConfig { get; private set; }

    public IReadOnlyList<SlotInstrument> Instruments => _instruments;

    public void UpdateCommunicationConfig(SlotCommunicationConfig communicationConfig)
    {
        CommunicationConfig = communicationConfig;
    }

    public void UpdateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            DisplayName = $"{Number.Value} \u53F7\u69FD\u4F4D";
            return;
        }

        DisplayName = displayName.Trim();
    }

    public void AddInstrument(SlotInstrument instrument) => _instruments.Add(instrument);
}
