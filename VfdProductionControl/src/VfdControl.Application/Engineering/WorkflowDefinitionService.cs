using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;

namespace VfdControl.Application.Engineering;

public sealed class WorkflowDefinitionService
{
    public IReadOnlyList<ProcessStep> CreateDefaultDemoSteps()
    {
        return
        [
            CreateStartStep(1),
            CreateDelayStep(2, 5000),
            CreateReadVfdMeasurementStep(3),
            CreateReadInstrumentMeasurementStep(4),
            CreateCompareMeasurementStep(5, FailureAction.ContinueAndMarkFail),
            CreateStopStep(6)
        ];
    }

    public ProcessStep CreateStartStep(int sequence)
    {
        return CreateStep(sequence, "启动 VFD", "Start", "Vfd:Control", "1");
    }

    public ProcessStep CreateDelayStep(int sequence, int milliseconds)
    {
        return CreateStep(sequence, "稳定等待", "Delay", "Timer", milliseconds.ToString());
    }

    public ProcessStep CreateReadVfdMeasurementStep(int sequence)
    {
        return CreateStep(sequence, "读取 VFD 电压", "ReadMeasurement", "Vfd:Voltage");
    }

    public ProcessStep CreateReadInstrumentMeasurementStep(int sequence)
    {
        return CreateStep(sequence, "读取仪表电压", "ReadMeasurement", "Instrument:Voltage");
    }

    public ProcessStep CreateCompareMeasurementStep(int sequence, FailureAction failureAction)
    {
        return CreateStep(
            sequence,
            "\u7535\u538B\u8BFB\u6570\u6BD4\u5BF9",
            "CompareMeasurement",
            "Vfd:Voltage|Instrument:Voltage",
            "Absolute:2",
            failureAction);
    }

    public ProcessStep CreateStopStep(int sequence)
    {
        return CreateStep(sequence, "停止 VFD", "Stop", "Vfd:Control", "6");
    }

    public ProcessStep CloneStep(ProcessStep source, int sequence)
    {
        return CreateStep(
            sequence,
            source.Name,
            source.Command.CommandType,
            source.Command.Target,
            source.Command.Value,
            source.FailurePolicy.Action,
            source.AffectsFinalConclusion,
            source.FailurePolicy.MaxRetries,
            source.Rule);
    }

    private static ProcessStep CreateStep(
        int sequence,
        string name,
        string commandType,
        string target,
        string? value = null,
        FailureAction failureAction = FailureAction.ContinueAndMarkFail,
        bool affectsFinalConclusion = true,
        int maxRetries = 0,
        StepRule? rule = null)
    {
        return new ProcessStep(
            Guid.NewGuid(),
            sequence,
            name,
            new StepCommand(commandType, target, value),
            new StepFailurePolicy(failureAction, maxRetries),
            affectsFinalConclusion,
            rule);
    }
}
