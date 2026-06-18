using VfdControl.Domain.Enums;

namespace VfdControl.Domain.Rules;

public sealed record RuleEvaluationResult(
    Conclusion Conclusion,
    string Message,
    bool AffectsFinalConclusion);
