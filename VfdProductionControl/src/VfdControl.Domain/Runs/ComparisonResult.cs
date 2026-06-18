using VfdControl.Domain.Enums;

namespace VfdControl.Domain.Runs;

public sealed record ComparisonResult(
    Guid Id,
    Guid StepRunId,
    string LeftKey,
    string RightKey,
    Conclusion Conclusion,
    string Message);
