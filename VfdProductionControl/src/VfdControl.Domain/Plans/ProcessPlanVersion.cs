namespace VfdControl.Domain.Plans;

public sealed class ProcessPlanVersion
{
    private readonly List<ProcessStep> _steps = [];

    public ProcessPlanVersion(
        Guid id,
        int versionNumber,
        bool isExecutable,
        DateTimeOffset? createdAt = null)
    {
        Id = id;
        VersionNumber = versionNumber;
        IsExecutable = isExecutable;
        CreatedAt = createdAt ?? DateTimeOffset.Now;
    }

    public Guid Id { get; }

    public int VersionNumber { get; }

    public bool IsExecutable { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public IReadOnlyList<ProcessStep> Steps => _steps;

    public void MarkExecutable() => IsExecutable = true;

    public void AddStep(ProcessStep step) => _steps.Add(step);
}
