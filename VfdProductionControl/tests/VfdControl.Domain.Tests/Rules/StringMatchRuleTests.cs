using FluentAssertions;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Rules;

namespace VfdControl.Domain.Tests.Rules;

public class StringMatchRuleTests
{
    [Fact]
    public void Exact_match_passes()
    {
        var rule = StringMatchRule.Exact("READY");

        rule.Evaluate("READY").Conclusion.Should().Be(Conclusion.Pass);
    }

    [Fact]
    public void Allowed_list_match_passes()
    {
        var rule = StringMatchRule.AllowedValues(["READY", "RUNNING"]);

        rule.Evaluate("RUNNING").Conclusion.Should().Be(Conclusion.Pass);
    }

    [Fact]
    public void Regex_match_passes()
    {
        var rule = StringMatchRule.Regex("^VFD\\d{4}$");

        rule.Evaluate("VFD0001").Conclusion.Should().Be(Conclusion.Pass);
    }

    [Fact]
    public void Mismatch_fails()
    {
        var rule = StringMatchRule.Exact("READY");

        rule.Evaluate("STOPPED").Conclusion.Should().Be(Conclusion.Fail);
    }
}
