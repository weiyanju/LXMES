using VfdControl.Domain.Plans;

namespace VfdControl.Application.Execution.StepExecutors;

public sealed class DelayStepExecutor
{
    public async Task ExecuteAsync(ProcessStep step, CancellationToken ct)
    {
        var delayMilliseconds = int.TryParse(step.Command.Value, out var parsed) ? parsed : 0;
        if (delayMilliseconds > 0)
        {
            await Task.Delay(delayMilliseconds, ct);
        }
    }
}
