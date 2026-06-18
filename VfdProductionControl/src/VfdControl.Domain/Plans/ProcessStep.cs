namespace VfdControl.Domain.Plans;

public sealed class ProcessStep
{
    public ProcessStep(
        Guid id,
        int sequence,
        string name,
        StepCommand command,
        StepFailurePolicy failurePolicy,
        bool affectsFinalConclusion,
        StepRule? rule = null)
    {
        Id = id;
        Sequence = sequence;
        Name = name;
        Command = command;
        FailurePolicy = failurePolicy;
        AffectsFinalConclusion = affectsFinalConclusion;
        Rule = rule;
    }

    public Guid Id { get; }

    public int Sequence { get; }

    public string Name { get; }

    public StepCommand Command { get; }

    public StepFailurePolicy FailurePolicy { get; }

    public bool AffectsFinalConclusion { get; }

    public StepRule? Rule { get; }
}
