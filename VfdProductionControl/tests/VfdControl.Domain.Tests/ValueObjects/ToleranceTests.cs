using FluentAssertions;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Domain.Tests.ValueObjects;

public class ToleranceTests
{
    [Fact]
    public void AbsoluteTolerance_accepts_difference_within_limit()
    {
        var tolerance = Tolerance.Absolute(2.0);

        tolerance.IsWithin(220.0, 218.2).Should().BeTrue();
    }

    [Fact]
    public void AbsoluteTolerance_rejects_difference_outside_limit()
    {
        var tolerance = Tolerance.Absolute(1.0);

        tolerance.IsWithin(220.0, 218.2).Should().BeFalse();
    }

    [Fact]
    public void PercentTolerance_uses_reference_value_as_basis()
    {
        var tolerance = Tolerance.Percent(1.0);

        tolerance.IsWithin(primaryValue: 220.0, referenceValue: 218.0).Should().BeTrue();
    }

    [Fact]
    public void PercentTolerance_rejects_large_percent_difference()
    {
        var tolerance = Tolerance.Percent(0.5);

        tolerance.IsWithin(primaryValue: 220.0, referenceValue: 218.0).Should().BeFalse();
    }
}
