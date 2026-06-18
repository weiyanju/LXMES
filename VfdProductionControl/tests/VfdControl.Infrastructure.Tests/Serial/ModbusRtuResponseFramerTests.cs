using FluentAssertions;
using VfdControl.Infrastructure.Serial;

namespace VfdControl.Infrastructure.Tests.Serial;

public class ModbusRtuResponseFramerTests
{
    [Fact]
    public void Write_single_register_response_is_complete_at_eight_bytes()
    {
        var request = ModbusCrc.Append([0x01, 0x06, 0x20, 0x00, 0x00, 0x01]);
        var response = ModbusCrc.Append([0x01, 0x06, 0x20, 0x00, 0x00, 0x01]);

        ModbusRtuResponseFramer.IsComplete(request, response).Should().BeTrue();
        ModbusRtuResponseFramer.IsComplete(request, response[..7]).Should().BeFalse();
    }

    [Fact]
    public void Read_holding_registers_response_uses_byte_count()
    {
        var request = ModbusCrc.Append([0x01, 0x03, 0x40, 0x30, 0x00, 0x01]);
        var response = ModbusCrc.Append([0x01, 0x03, 0x02, 0x00, 0x7B]);

        ModbusRtuResponseFramer.IsComplete(request, response).Should().BeTrue();
        ModbusRtuResponseFramer.IsComplete(request, response[..6]).Should().BeFalse();
    }

    [Fact]
    public void Exception_response_is_complete_at_five_bytes()
    {
        var request = ModbusCrc.Append([0x01, 0x03, 0x40, 0x30, 0x00, 0x01]);
        var response = ModbusCrc.Append([0x01, 0x83, 0x02]);

        ModbusRtuResponseFramer.IsComplete(request, response).Should().BeTrue();
        ModbusRtuResponseFramer.IsComplete(request, response[..4]).Should().BeFalse();
    }
}
