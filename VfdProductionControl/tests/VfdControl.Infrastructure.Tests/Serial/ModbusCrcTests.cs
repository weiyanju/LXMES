using FluentAssertions;
using VfdControl.Infrastructure.Serial;

namespace VfdControl.Infrastructure.Tests.Serial;

public class ModbusCrcTests
{
    [Fact]
    public void Compute_returns_known_modbus_crc()
    {
        var frame = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };

        var crc = ModbusCrc.Compute(frame);

        crc.Should().Be(0xCDC5);
    }

    [Fact]
    public void Append_adds_low_byte_then_high_byte()
    {
        var frame = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };

        var withCrc = ModbusCrc.Append(frame);

        withCrc.Should().Equal(0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xC5, 0xCD);
    }

    [Fact]
    public void Is_valid_rejects_corrupted_frame()
    {
        var validFrame = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xC5, 0xCD };
        var corruptedFrame = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xC5, 0xCC };

        ModbusCrc.IsValid(validFrame).Should().BeTrue();
        ModbusCrc.IsValid(corruptedFrame).Should().BeFalse();
    }
}
