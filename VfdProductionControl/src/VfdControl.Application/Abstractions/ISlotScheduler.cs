using VfdControl.Application.Execution;

namespace VfdControl.Application.Abstractions;

public interface ISlotScheduler
{
    Task<StationSessionResult> RunAsync(StationSessionContext context, CancellationToken cancellationToken);

    Task PauseAsync(Guid sessionId);

    Task ResumeAsync(Guid sessionId);

    Task StopSlotAsync(Guid sessionId, Guid slotRunId);

    Task StopSessionAsync(Guid sessionId);
}
