using FluentAssertions;
using VfdControl.Infrastructure.Serial;

namespace VfdControl.Infrastructure.Tests.Serial;

public class SerialBarcodeFrameBufferTests
{
    [Fact]
    public void Append_returns_complete_scans_split_by_line_endings()
    {
        var buffer = new SerialBarcodeFrameBuffer();

        var scans = buffer.Append("EMP0001\r\nVFD202606010001\n");

        scans.Should().Equal("EMP0001", "VFD202606010001");
    }

    [Fact]
    public void Append_keeps_partial_scan_until_terminator_arrives()
    {
        var buffer = new SerialBarcodeFrameBuffer();

        buffer.Append("EMP").Should().BeEmpty();
        var scans = buffer.Append("0001\r");

        scans.Should().Equal("EMP0001");
    }

    [Fact]
    public void Append_ignores_empty_scans()
    {
        var buffer = new SerialBarcodeFrameBuffer();

        var scans = buffer.Append("\r\nEMP0001\n\n");

        scans.Should().Equal("EMP0001");
    }
}
