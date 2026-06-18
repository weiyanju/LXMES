using VfdControl.Application.Execution;
using VfdControl.Domain.Enums;

namespace VfdControl.Application.Traceability;

public sealed record TraceabilitySessionQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Barcode = null,
    Conclusion? Conclusion = null);

public sealed record StationSessionSummary(
    Guid SessionId,
    Guid StationId,
    string OperatorCode,
    DateTimeOffset StartedAt,
    Conclusion? Conclusion,
    int DeviceRunCount);

public sealed record DeviceRunTrace(
    Guid DeviceRunId,
    Guid SessionId,
    Guid SlotId,
    string Barcode,
    Conclusion Conclusion,
    DateTimeOffset StartedAt,
    IReadOnlyList<StepRunTrace> Steps);

public sealed record StepRunTrace(
    Guid StepRunId,
    Guid DeviceRunId,
    int Sequence,
    string StepName,
    Conclusion Conclusion,
    IReadOnlyList<MeasurementTrace> Measurements,
    IReadOnlyList<ComparisonTrace> Comparisons,
    IReadOnlyList<CommandTraceSnapshot> CommandTraces,
    string Message = "");

public sealed record MeasurementTrace(
    Guid StepRunId,
    string Key,
    double NumericValue,
    string Unit,
    MeasurementSource Source);

public sealed record ComparisonTrace(
    Guid StepRunId,
    string LeftKey,
    string RightKey,
    Conclusion Conclusion,
    string Message);
