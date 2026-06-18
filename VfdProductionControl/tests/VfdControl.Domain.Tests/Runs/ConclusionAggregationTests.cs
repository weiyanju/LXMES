using FluentAssertions;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Runs;

namespace VfdControl.Domain.Tests.Runs;

public class ConclusionAggregationTests
{
    [Fact]
    public void DeviceRun_passes_when_all_affecting_steps_pass()
    {
        var run = DeviceRun.CreateForTest();
        run.AddStepConclusion(Conclusion.Pass, affectsFinalConclusion: true);
        run.AddStepConclusion(Conclusion.Pass, affectsFinalConclusion: true);

        run.CalculateFinalConclusion().Should().Be(Conclusion.Pass);
    }

    [Fact]
    public void DeviceRun_fails_when_any_affecting_step_fails()
    {
        var run = DeviceRun.CreateForTest();
        run.AddStepConclusion(Conclusion.Pass, affectsFinalConclusion: true);
        run.AddStepConclusion(Conclusion.Fail, affectsFinalConclusion: true);

        run.CalculateFinalConclusion().Should().Be(Conclusion.Fail);
    }

    [Fact]
    public void DeviceRun_warns_when_only_non_affecting_warning_exists()
    {
        var run = DeviceRun.CreateForTest();
        run.AddStepConclusion(Conclusion.Pass, affectsFinalConclusion: true);
        run.AddStepConclusion(Conclusion.Warning, affectsFinalConclusion: false);

        run.CalculateFinalConclusion().Should().Be(Conclusion.Warning);
    }
}
