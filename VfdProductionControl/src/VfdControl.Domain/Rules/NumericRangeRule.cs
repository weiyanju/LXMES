using VfdControl.Domain.Enums;

namespace VfdControl.Domain.Rules;

public sealed record NumericRangeRule(
    double? LowerLimit,
    double? UpperLimit,
    bool AffectsFinalConclusion = true)
{
    public RuleEvaluationResult Evaluate(double value)
    {
        if (LowerLimit.HasValue && value < LowerLimit.Value)
        {
            return new RuleEvaluationResult(Conclusion.Fail, $"Value {value} is below lower limit {LowerLimit.Value}.", AffectsFinalConclusion);
        }

        if (UpperLimit.HasValue && value > UpperLimit.Value)
        {
            return new RuleEvaluationResult(Conclusion.Fail, $"Value {value} is above upper limit {UpperLimit.Value}.", AffectsFinalConclusion);
        }

        return new RuleEvaluationResult(Conclusion.Pass, $"Value {value} is within range.", AffectsFinalConclusion);
    }
}
