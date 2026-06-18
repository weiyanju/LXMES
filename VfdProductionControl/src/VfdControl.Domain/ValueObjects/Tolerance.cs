using VfdControl.Domain.Enums;

namespace VfdControl.Domain.ValueObjects;

public sealed record Tolerance(ToleranceType Type, double Value)
{
    public static Tolerance Absolute(double value) => new(ToleranceType.Absolute, value);

    public static Tolerance Percent(double value) => new(ToleranceType.Percent, value);

    public bool IsWithin(double primaryValue, double referenceValue)
    {
        var difference = Math.Abs(primaryValue - referenceValue);
        return Type switch
        {
            ToleranceType.Absolute => difference <= Value,
            ToleranceType.Percent => referenceValue == 0
                ? difference == 0
                : difference / Math.Abs(referenceValue) * 100.0 <= Value,
            _ => false
        };
    }
}
