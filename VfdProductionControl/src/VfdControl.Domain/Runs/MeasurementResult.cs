using VfdControl.Domain.ValueObjects;

namespace VfdControl.Domain.Runs;

public sealed record MeasurementResult(
    Guid Id,
    Guid StepRunId,
    string Key,
    MeasurementValue Value);
