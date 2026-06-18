using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;
using VfdControl.Domain.Rules;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Application.Execution.StepExecutors;

public sealed class CompareMeasurementStepExecutor
{
    public RuleEvaluationResult Execute(ProcessStep step, IReadOnlyDictionary<string, MeasurementValue> measurements)
    {
        var parts = step.Command.Target.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !measurements.TryGetValue(parts[0], out var primary) || !measurements.TryGetValue(parts[1], out var reference))
        {
            return new RuleEvaluationResult(Conclusion.Fail, "Comparison inputs are missing.", step.AffectsFinalConclusion);
        }

        var rule = new MeasurementComparisonRule(ParseTolerance(step.Command.Value), step.AffectsFinalConclusion);
        return rule.Evaluate(primary, reference);
    }

    private static Tolerance ParseTolerance(string? value)
    {
        var parts = (value ?? "Absolute:0").Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var amount = parts.Length == 2 && double.TryParse(parts[1], out var parsed) ? parsed : 0.0;

        return parts.Length > 0 && parts[0].Equals("Percent", StringComparison.OrdinalIgnoreCase)
            ? Tolerance.Percent(amount)
            : Tolerance.Absolute(amount);
    }
}
