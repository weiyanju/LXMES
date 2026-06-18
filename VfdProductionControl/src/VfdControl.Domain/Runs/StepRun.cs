using VfdControl.Domain.Enums;

namespace VfdControl.Domain.Runs;

public sealed class StepRun
{
    public StepRun(Guid id, Guid processStepId, int sequence)
    {
        Id = id;
        ProcessStepId = processStepId;
        Sequence = sequence;
    }

    public Guid Id { get; }

    public Guid ProcessStepId { get; }

    public int Sequence { get; }

    public StepRunStatus Status { get; private set; } = StepRunStatus.Pending;

    public Conclusion Conclusion { get; private set; } = Conclusion.None;

    public void Complete(Conclusion conclusion)
    {
        Conclusion = conclusion;
        Status = conclusion switch
        {
            Conclusion.Pass => StepRunStatus.Passed,
            Conclusion.Warning => StepRunStatus.Warning,
            Conclusion.Fail => StepRunStatus.Failed,
            _ => StepRunStatus.Skipped
        };
    }
}
