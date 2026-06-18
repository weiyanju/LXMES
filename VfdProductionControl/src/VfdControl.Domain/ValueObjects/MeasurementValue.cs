using VfdControl.Domain.Enums;

namespace VfdControl.Domain.ValueObjects;

public sealed record MeasurementValue(double NumericValue, string Unit, MeasurementSource Source);
