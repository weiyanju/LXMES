using FluentAssertions;
using VfdControl.Infrastructure.Serial;

namespace VfdControl.Infrastructure.Tests.Serial;

public class ModbusRegisterAddressTests
{
    [Theory]
    [InlineData("0x2000", 0x2000)]
    [InlineData("2000", 0x2000)]
    [InlineData("8192", 0x8192)]
    public void Parses_hex_prefixed_and_four_digit_helper_addresses(string value, ushort expected)
    {
        ModbusRegisterAddress.TryParse(value, out var parsed).Should().BeTrue();

        parsed.Should().Be(expected);
    }

    [Theory]
    [InlineData("0x2000", "0x2000")]
    [InlineData("2000", "0x2000")]
    [InlineData("40020", "40020")]
    public void Normalizes_display_without_rewriting_five_digit_legacy_addresses(string value, string expected)
    {
        ModbusRegisterAddress.NormalizeForDisplay(value).Should().Be(expected);
    }
}
