using FluentAssertions;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Domain.Tests.ValueObjects;

public class BarcodeTests
{
    [Fact]
    public void EmployeeCode_accepts_default_test_format()
    {
        EmployeeCode.TryCreate("EMP0001").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void VfdBarcode_accepts_default_test_format()
    {
        Barcode.TryCreateVfd("VFD202605280001").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void VfdBarcode_rejects_empty_text()
    {
        Barcode.TryCreateVfd("").IsSuccess.Should().BeFalse();
    }
}
