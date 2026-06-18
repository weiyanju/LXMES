using FluentAssertions;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Rules;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Domain.Tests.Rules;

public class MeasurementComparisonRuleTests
{
    [Fact]
    public void Vfd_and_instrument_voltage_within_absolute_tolerance_returns_pass()
    {
        var rule = new MeasurementComparisonRule(Tolerance.Absolute(2.0));

        rule.Evaluate(
            new MeasurementValue(220.0, "V", MeasurementSource.Vfd),
            new MeasurementValue(219.0, "V", MeasurementSource.Instrument)).Conclusion.Should().Be(Conclusion.Pass);
    }

    [Fact]
    public void Vfd_and_instrument_voltage_outside_absolute_tolerance_returns_fail()
    {
        var rule = new MeasurementComparisonRule(Tolerance.Absolute(2.0));

        rule.Evaluate(
            new MeasurementValue(225.0, "V", MeasurementSource.Vfd),
            new MeasurementValue(219.0, "V", MeasurementSource.Instrument)).Conclusion.Should().Be(Conclusion.Fail);
    }

    [Fact]
    public void Vfd_and_instrument_voltage_within_percent_tolerance_returns_pass()
    {
        var rule = new MeasurementComparisonRule(Tolerance.Percent(1.0));

        rule.Evaluate(
            new MeasurementValue(220.0, "V", MeasurementSource.Vfd),
            new MeasurementValue(219.0, "V", MeasurementSource.Instrument)).Conclusion.Should().Be(Conclusion.Pass);
    }

    [Fact]
    public void Comparison_message_includes_difference_percent_and_tolerance()
    {
        var rule = new MeasurementComparisonRule(Tolerance.Percent(1.0));

        var result = rule.Evaluate(
            new MeasurementValue(220.0, "V", MeasurementSource.Vfd),
            new MeasurementValue(219.0, "V", MeasurementSource.Instrument));

        result.Message.Should().Contain("\u5DEE\u503C 1 V");
        result.Message.Should().Contain("\u8BEF\u5DEE 0.46%");
        result.Message.Should().Contain("\u5BB9\u5DEE \u00B11%");
    }
}
