using FluentAssertions;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Rules;

namespace VfdControl.Domain.Tests.Rules;

public class NumericRuleTests
{
    [Fact]
    public void Value_inside_lower_and_upper_range_returns_pass()
    {
        var rule = new NumericRangeRule(210.0, 230.0);

        rule.Evaluate(220.0).Conclusion.Should().Be(Conclusion.Pass);
    }

    [Fact]
    public void Value_below_lower_range_returns_fail()
    {
        var rule = new NumericRangeRule(210.0, 230.0);

        rule.Evaluate(209.9).Conclusion.Should().Be(Conclusion.Fail);
    }

    [Fact]
    public void Value_above_upper_range_returns_fail()
    {
        var rule = new NumericRangeRule(210.0, 230.0);

        rule.Evaluate(230.1).Conclusion.Should().Be(Conclusion.Fail);
    }
}
