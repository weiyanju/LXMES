namespace VfdControl.Application.Execution;

public sealed class SlotExecutionStateStore
{
    private readonly object _sync = new();
    private readonly HashSet<Guid> _pausedSessions = [];
    private readonly HashSet<Guid> _stoppedSessions = [];
    private readonly HashSet<(Guid SessionId, Guid SlotId)> _stoppedSlots = [];

    public void PauseSession(Guid sessionId)
    {
        lock (_sync)
        {
            _pausedSessions.Add(sessionId);
        }
    }

    public void ResumeSession(Guid sessionId)
    {
        lock (_sync)
        {
            _pausedSessions.Remove(sessionId);
        }
    }

    public bool IsPaused(Guid sessionId)
    {
        lock (_sync)
        {
            return _pausedSessions.Contains(sessionId);
        }
    }

    public void StopSession(Guid sessionId, IEnumerable<Guid> slotIds)
    {
        lock (_sync)
        {
            _stoppedSessions.Add(sessionId);
            foreach (var slotId in slotIds)
            {
                _stoppedSlots.Add((sessionId, slotId));
            }
        }
    }

    public void StopSlot(Guid sessionId, Guid slotId)
    {
        lock (_sync)
        {
            _stoppedSlots.Add((sessionId, slotId));
        }
    }

    public bool IsSessionStopRequested(Guid sessionId)
    {
        lock (_sync)
        {
            return _stoppedSessions.Contains(sessionId);
        }
    }

    public bool IsSlotStopRequested(Guid sessionId, Guid slotId)
    {
        lock (_sync)
        {
            return _stoppedSlots.Contains((sessionId, slotId));
        }
    }
}
