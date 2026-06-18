namespace VfdControl.Domain.Enums;

public enum SessionStatus
{
    NotStarted,
    Running,
    Paused,
    Completed,
    Stopped,
    Faulted
}

public enum SlotRunStatus
{
    Empty,
    WaitingBarcode,
    Queued,
    Running,
    Passed,
    Failed,
    Warning,
    PendingAction,
    Stopped,
    Removed
}

public enum StepRunStatus
{
    Pending,
    Running,
    Passed,
    Warning,
    Failed,
    Skipped,
    WaitingOperator,
    Retried
}
