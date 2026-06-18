using VfdControl.Domain.Enums;

namespace VfdControl.Domain.Runs;

public sealed class StationSession
{
    private readonly List<DeviceRun> _deviceRuns = [];

    public StationSession(Guid id, Guid stationId, DateTimeOffset startedAt)
    {
        Id = id;
        StationId = stationId;
        StartedAt = startedAt;
    }

    public Guid Id { get; }

    public Guid StationId { get; }

    public DateTimeOffset StartedAt { get; }

    public SessionStatus Status { get; private set; } = SessionStatus.NotStarted;

    public IReadOnlyList<DeviceRun> DeviceRuns => _deviceRuns;

    public void Start() => Status = SessionStatus.Running;

    public void AddDeviceRun(DeviceRun deviceRun) => _deviceRuns.Add(deviceRun);
}
