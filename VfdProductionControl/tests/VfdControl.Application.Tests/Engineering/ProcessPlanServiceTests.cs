using FluentAssertions;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Engineering;
using VfdControl.Application.Operator;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;

namespace VfdControl.Application.Tests.Engineering;

public class ProcessPlanServiceTests
{
    [Fact]
    public async Task Creating_plan_saves_plan()
    {
        var repository = new FakeProcessPlanRepository();
        var service = new ProcessPlanService(repository);

        var result = await service.CreatePlanAsync("VFD 基础测试", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repository.Plans.Should().ContainSingle(plan => plan.Name == "VFD 基础测试");
    }

    [Fact]
    public async Task Saving_plan_creates_new_version()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD 基础测试");
        var repository = new FakeProcessPlanRepository([plan]);
        var service = new ProcessPlanService(repository);
        var steps = new WorkflowDefinitionService().CreateDefaultDemoSteps();

        var result = await service.SaveNewVersionAsync(plan.Id, steps, isExecutable: false, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        plan.Versions.Should().ContainSingle();
        plan.Versions[0].VersionNumber.Should().Be(1);
        plan.Versions[0].Steps.Should().HaveCount(steps.Count);
    }

    [Fact]
    public async Task Executable_version_appears_in_operator_plan_list()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD 基础测试");
        var repository = new FakeProcessPlanRepository([plan]);
        var service = new ProcessPlanService(repository);
        var operatorService = new PlanSelectionService(repository);
        var version = (await service.SaveNewVersionAsync(
            plan.Id,
            new WorkflowDefinitionService().CreateDefaultDemoSteps(),
            isExecutable: false,
            CancellationToken.None)).Value!;

        await service.MarkVersionExecutableAsync(plan.Id, version.Id, CancellationToken.None);

        var options = await operatorService.GetExecutablePlanOptionsAsync(CancellationToken.None);
        options.Should().ContainSingle();
        options[0].PlanName.Should().Be("VFD 基础测试");
        options[0].Version.Should().Be(version);
    }

    [Fact]
    public async Task Editing_version_does_not_mutate_historical_version()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD 基础测试");
        var repository = new FakeProcessPlanRepository([plan]);
        var service = new ProcessPlanService(repository);
        var workflow = new WorkflowDefinitionService();
        var historical = (await service.SaveNewVersionAsync(
            plan.Id,
            [workflow.CreateStartStep(1)],
            isExecutable: true,
            CancellationToken.None)).Value!;

        var clone = (await service.CloneVersionAsync(plan.Id, historical.Id, CancellationToken.None)).Value!;
        clone.AddStep(workflow.CreateDelayStep(2, milliseconds: 5000));

        historical.Steps.Should().ContainSingle();
        clone.Steps.Should().HaveCount(2);
        clone.VersionNumber.Should().Be(2);
    }

    [Fact]
    public void Workflow_definition_service_creates_named_mvp_steps()
    {
        var service = new WorkflowDefinitionService();

        var steps = new[]
        {
            service.CreateStartStep(1),
            service.CreateDelayStep(2, 5000),
            service.CreateReadVfdMeasurementStep(3),
            service.CreateReadInstrumentMeasurementStep(4),
            service.CreateCompareMeasurementStep(5, FailureAction.ContinueAsWarning),
            service.CreateStopStep(6)
        };

        steps.Select(step => step.Command.CommandType)
            .Should()
            .Equal("Start", "Delay", "ReadMeasurement", "ReadMeasurement", "CompareMeasurement", "Stop");
        steps[4].FailurePolicy.Action.Should().Be(FailureAction.ContinueAsWarning);
    }

    private sealed class FakeProcessPlanRepository : IProcessPlanRepository
    {
        public FakeProcessPlanRepository(IEnumerable<ProcessPlan>? plans = null)
        {
            Plans = (plans ?? []).ToList();
        }

        public List<ProcessPlan> Plans { get; }

        public Task<IReadOnlyList<ProcessPlan>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<ProcessPlan>>(Plans);

        public Task<IReadOnlyList<ProcessPlanVersion>> ListExecutableVersionsAsync(CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<ProcessPlanVersion>>(
                Plans.SelectMany(plan => plan.Versions).Where(version => version.IsExecutable).ToList());
        }

        public Task<ProcessPlan?> GetAsync(Guid planId, CancellationToken ct)
        {
            return Task.FromResult(Plans.SingleOrDefault(plan => plan.Id == planId));
        }

        public Task SaveAsync(ProcessPlan plan, CancellationToken ct)
        {
            if (Plans.All(existing => existing.Id != plan.Id))
            {
                Plans.Add(plan);
            }

            return Task.CompletedTask;
        }
    }
}
