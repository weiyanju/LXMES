namespace VfdControl.Domain.Plans;

public sealed record StepRule(
    string RuleType,
    double? LowerLimit = null,
    double? UpperLimit = null,
    string? ExpectedValue = null)
{
    public const string NumericRangeRuleType = "NumericRange";
    public const string StringEqualsRuleType = "StringEquals";

    public static StepRule NumericRange(double? lowerLimit, double? upperLimit)
    {
        return new StepRule(NumericRangeRuleType, lowerLimit, upperLimit);
    }

    public static StepRule StringEquals(string expectedValue)
    {
        return new StepRule(StringEqualsRuleType, ExpectedValue: expectedValue);
    }
}
