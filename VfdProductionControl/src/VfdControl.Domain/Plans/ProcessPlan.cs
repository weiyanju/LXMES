namespace VfdControl.Domain.Plans;

public sealed class ProcessPlan
{
    private readonly List<ProcessPlanVersion> _versions = [];

    public ProcessPlan(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; }

    public string Name { get; }

    public IReadOnlyList<ProcessPlanVersion> Versions => _versions;

    public void AddVersion(ProcessPlanVersion version) => _versions.Add(version);
}
