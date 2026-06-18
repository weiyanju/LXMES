using VfdControl.Domain.Enums;

namespace VfdControl.Domain.Plans;

public sealed record StepFailurePolicy(
    FailureAction Action,
    int MaxRetries = 0);
