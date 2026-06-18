using VfdControl.Domain.Enums;

namespace VfdControl.Application.Execution.StepExecutors;

internal sealed record StepTarget(MeasurementSource Source, string PointName)
{
    public static StepTarget Parse(string target)
    {
        var parts = target.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !Enum.TryParse<MeasurementSource>(parts[0], ignoreCase: true, out var source))
        {
            return new StepTarget(MeasurementSource.Vfd, target);
        }

        return new StepTarget(source, parts[1]);
    }
}
