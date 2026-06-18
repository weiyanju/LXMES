using VfdControl.Domain.Enums;

namespace VfdControl.Domain.Runs;

public sealed class DeviceRun
{
    private readonly List<StepRun> _stepRuns = [];
    private readonly List<(Conclusion Conclusion, bool AffectsFinalConclusion)> _stepConclusions = [];

    public Guid Id { get; } = Guid.NewGuid();

    public Conclusion FinalConclusion { get; private set; } = Conclusion.None;

    public IReadOnlyList<StepRun> StepRuns => _stepRuns;

    public static DeviceRun CreateForTest() => new();

    public void AddStepRun(StepRun stepRun) => _stepRuns.Add(stepRun);

    public void AddStepConclusion(Conclusion conclusion, bool affectsFinalConclusion)
    {
        _stepConclusions.Add((conclusion, affectsFinalConclusion));
    }

    public Conclusion CalculateFinalConclusion()
    {
        if (_stepConclusions.Any(x => x.AffectsFinalConclusion && x.Conclusion == Conclusion.Fail))
        {
            return FinalConclusion = Conclusion.Fail;
        }

        if (_stepConclusions.Any(x => x.Conclusion == Conclusion.Warning))
        {
            return FinalConclusion = Conclusion.Warning;
        }

        if (_stepConclusions.Count > 0 && _stepConclusions.All(x => x.Conclusion is Conclusion.Pass or Conclusion.Warning))
        {
            return FinalConclusion = Conclusion.Pass;
        }

        return FinalConclusion = Conclusion.None;
    }
}
