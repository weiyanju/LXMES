namespace VfdControl.Domain.Plans;

public sealed record StepCommand(
    string CommandType,
    string Target,
    string? Value = null);
