using FluentAssertions;
using VfdControl.Domain.Stations;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Domain.Tests.Stations;

public class StationSlotTests
{
    [Theory]
    [InlineData(1, "1 号槽位")]
    [InlineData(6, "6 号槽位")]
    [InlineData(27, "27 号槽位")]
    public void Default_display_name_keeps_slot_number_inside_name(int slotNumber, string expectedDisplayName)
    {
        var slot = new StationSlot(
            Guid.NewGuid(),
            new SlotNumber(slotNumber),
            CreateCommunicationConfig(slotNumber));

        slot.DisplayName.Should().Be(expectedDisplayName);
    }

    [Fact]
    public void Blank_display_name_resets_to_numbered_default_name()
    {
        var slot = new StationSlot(
            Guid.NewGuid(),
            new SlotNumber(2),
            CreateCommunicationConfig(2),
            "老化位 B");

        slot.UpdateDisplayName(" ");

        slot.DisplayName.Should().Be("2 号槽位");
    }

    private static SlotCommunicationConfig CreateCommunicationConfig(int slotNumber)
    {
        return new SlotCommunicationConfig(
            null,
            new ModbusAddress((byte)slotNumber),
            new ModbusAddress((byte)(slotNumber + 10)),
            new ModbusAddress((byte)(slotNumber + 20)),
            9600);
    }
}
