using System.Globalization;
using VfdControl.Domain.Enums;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Domain.Rules;

public sealed record MeasurementComparisonRule(
    Tolerance Tolerance,
    bool AffectsFinalConclusion = true)
{
    public RuleEvaluationResult Evaluate(MeasurementValue primaryValue, MeasurementValue referenceValue)
    {
        var isWithin = Tolerance.IsWithin(primaryValue.NumericValue, referenceValue.NumericValue);
        var conclusion = isWithin ? Conclusion.Pass : Conclusion.Fail;
        var difference = Math.Abs(primaryValue.NumericValue - referenceValue.NumericValue);
        double? percentDifference = referenceValue.NumericValue == 0
            ? null
            : difference / Math.Abs(referenceValue.NumericValue) * 100.0;
        var message =
            $"{FormatNumber(primaryValue.NumericValue)} {primaryValue.Unit} / {FormatNumber(referenceValue.NumericValue)} {referenceValue.Unit}, " +
            $"\u5DEE\u503C {FormatNumber(difference)} {primaryValue.Unit}, " +
            $"\u8BEF\u5DEE {FormatPercent(percentDifference)}, " +
            $"\u5BB9\u5DEE {FormatTolerance()}, " +
            (isWithin ? "\u901A\u8FC7" : "\u4E0D\u901A\u8FC7");

        return new RuleEvaluationResult(conclusion, message, AffectsFinalConclusion);
    }

    private string FormatTolerance()
    {
        return Tolerance.Type switch
        {
            ToleranceType.Percent => $"\u00B1{FormatNumber(Tolerance.Value)}%",
            _ => $"\u00B1{FormatNumber(Tolerance.Value)}"
        };
    }

    private static string FormatPercent(double? value)
    {
        return value is null
            ? "\u65E0\u6CD5\u8BA1\u7B97"
            : $"{FormatNumber(value.Value)}%";
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
