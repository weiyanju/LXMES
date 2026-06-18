using VfdControl.Application.Execution;

namespace VfdControl.Application.Abstractions;

public interface IWorkflowEngine
{
    Task<DeviceRunResult> ExecuteAsync(DeviceRunContext context, CancellationToken cancellationToken);
}
