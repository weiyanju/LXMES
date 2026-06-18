using FluentAssertions;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Engineering;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;
using VfdControl.Presentation.Admin;
using VfdControl.Presentation.Engineering;

namespace VfdControl.Presentation.Tests.Engineering;

public class WorkflowEditorViewModelTests
{
    [Fact]
    public async Task Plan_list_loads_existing_plans()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD 生产测试演示方案");
        var viewModel = CreatePlanListViewModel([plan]);

        await viewModel.LoadAsync();

        viewModel.Plans.Should().ContainSingle();
        viewModel.Plans[0].Name.Should().Be("VFD 生产测试演示方案");
    }

    [Fact]
    public async Task Plan_list_shows_last_updated_instead_of_latest_version_number()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD 生产测试演示方案");
        plan.AddVersion(new ProcessPlanVersion(
            Guid.NewGuid(),
            1,
            isExecutable: true,
            new DateTimeOffset(2026, 6, 12, 14, 30, 0, TimeSpan.FromHours(8))));
        var viewModel = CreatePlanListViewModel([plan]);

        await viewModel.LoadAsync();

        viewModel.Plans[0].LastUpdatedDisplay.Should().Be("最后更新：2026-06-12 14:30");
    }

    [Fact]
    public async Task Editor_adds_mvp_steps_in_order()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD 生产测试演示方案");
        var viewModel = CreateEditorViewModel([plan]);
        await viewModel.LoadPlanAsync(plan);

        viewModel.AddStartStepCommand.Execute(null);
        viewModel.AddDelayStepCommand.Execute(null);
        viewModel.AddReadVfdMeasurementStepCommand.Execute(null);
        viewModel.AddReadInstrumentMeasurementStepCommand.Execute(null);
        viewModel.AddCompareMeasurementStepCommand.Execute(null);
        viewModel.AddStopStepCommand.Execute(null);

        viewModel.Steps.Select(step => step.CommandType)
            .Should()
            .Equal("Start", "Delay", "ReadMeasurement", "ReadMeasurement", "CompareMeasurement", "Stop");
        viewModel.Steps.Select(step => step.StepTypeDisplay)
            .Should()
            .Equal(
                "\u5199\u5165\u70B9\u4F4D",
                "\u5EF6\u8FDF",
                "\u8BFB\u53D6\u70B9\u4F4D",
                "\u8BFB\u53D6\u70B9\u4F4D",
                "\u70B9\u4F4D\u8BFB\u6570\u6BD4\u5BF9",
                "\u5199\u5165\u70B9\u4F4D");
        viewModel.Steps.Select(step => step.Sequence).Should().Equal(1, 2, 3, 4, 5, 6);
    }

    [Fact]
    public async Task Editor_configures_compare_failure_action()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD 生产测试演示方案");
        var viewModel = CreateEditorViewModel([plan]);
        await viewModel.LoadPlanAsync(plan);
        viewModel.SelectedFailureAction = FailureAction.ContinueAsWarning;

        viewModel.AddCompareMeasurementStepCommand.Execute(null);

        viewModel.Steps.Single().FailureAction.Should().Be(FailureAction.ContinueAsWarning);
        viewModel.Steps.Single().FailureActionDisplay.Should().Be("继续并标记警告");
    }

    [Fact]
    public async Task Adding_step_selects_it_for_property_editing()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD production test plan");
        var viewModel = CreateEditorViewModel([plan]);
        await viewModel.LoadPlanAsync(plan);

        viewModel.AddDelayStepCommand.Execute(null);

        viewModel.SelectedStep.Should().BeSameAs(viewModel.Steps.Single());
    }

    [Fact]
    public async Task Adding_blank_step_creates_point_driven_default_step()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD production test plan");
        var viewModel = CreateEditorViewModel([plan]);
        await viewModel.LoadPlanAsync(plan);

        viewModel.AddBlankStepCommand.Execute(null);

        viewModel.Steps.Should().ContainSingle();
        viewModel.SelectedStep.Should().BeSameAs(viewModel.Steps.Single());
        viewModel.SelectedStep!.Name.Should().Be("\u65B0\u589E\u6B65\u9AA4");
        viewModel.SelectedStep.CommandType.Should().Be("ReadMeasurement");
        viewModel.SelectedStep.Target.Should().Be("Vfd:Voltage");
        viewModel.SelectedStep.Value.Should().Be("");
    }

    [Fact]
    public async Task Edited_step_properties_are_saved_to_new_version()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD production test plan");
        var repository = new FakeProcessPlanRepository([plan]);
        var viewModel = CreateEditorViewModel(repository);
        await viewModel.LoadPlanAsync(plan);
        viewModel.AddDelayStepCommand.Execute(null);

        viewModel.SelectedStep!.Name = "Wait for voltage stabilization";
        viewModel.SelectedStep.Value = "8000";
        viewModel.SelectedStep.FailureAction = FailureAction.StopSlotImmediately;
        viewModel.SelectedStep.MaxRetries = 1;
        viewModel.SelectedStep.AffectsFinalConclusion = false;

        await viewModel.SaveVersionCommand.ExecuteAsync(null);

        var savedStep = plan.Versions.Single().Steps.Single();
        savedStep.Name.Should().Be("Wait for voltage stabilization");
        savedStep.Command.Value.Should().Be("8000");
        savedStep.FailurePolicy.Action.Should().Be(FailureAction.StopSlotImmediately);
        savedStep.FailurePolicy.MaxRetries.Should().Be(1);
        savedStep.AffectsFinalConclusion.Should().BeFalse();
    }

    [Fact]
    public async Task Read_measurement_step_saves_range_rule_instead_of_generic_parameter()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD production test plan");
        var repository = new FakeProcessPlanRepository([plan]);
        var viewModel = CreateEditorViewModel(repository);
        await viewModel.LoadPlanAsync(plan);
        viewModel.AddReadVfdMeasurementStepCommand.Execute(null);

        viewModel.SelectedStep!.LowerLimit = "210";
        viewModel.SelectedStep.UpperLimit = "230";

        await viewModel.SaveVersionCommand.ExecuteAsync(null);

        var savedStep = plan.Versions.Single().Steps.Single();
        savedStep.Command.Value.Should().BeNull();
        savedStep.Rule.Should().BeEquivalentTo(StepRule.NumericRange(210, 230));
        viewModel.SelectedStep.ParameterSummary.Should().Be("Vfd:Voltage / 210 ~ 230");
    }

    [Fact]
    public async Task Read_string_step_saves_expected_value_rule()
    {
        var catalog = new DeviceModelCatalog();
        var vfdModel = catalog.DeviceModels.Single(model => model.Name == "\u6807\u51C6 VFD");
        vfdModel.Points.Add(new LogicalPointRowViewModel(
            "Vfd:ModelText",
            "VFD \u578B\u53F7\u6587\u672C",
            "VFD",
            "\u8BFB\u53D6",
            "03",
            "40100",
            "String",
            "",
            "\u81EA\u5B9A\u4E49\u5B57\u7B26\u4E32\u70B9\u4F4D",
            isCustom: true));
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD production test plan");
        var repository = new FakeProcessPlanRepository([plan]);
        var viewModel = CreateEditorViewModel(repository, catalog);
        await viewModel.LoadPlanAsync(plan);
        viewModel.AddBlankStepCommand.Execute(null);

        viewModel.SelectedStep!.Target = "Vfd:ModelText";
        viewModel.SelectedStep.ExpectedText = "VFD-X1";

        await viewModel.SaveVersionCommand.ExecuteAsync(null);

        var savedStep = plan.Versions.Single().Steps.Single();
        savedStep.Command.Value.Should().BeNull();
        savedStep.Rule.Should().BeEquivalentTo(StepRule.StringEquals("VFD-X1"));
        viewModel.SelectedStep.ParameterSummary.Should().Be("Vfd:ModelText = VFD-X1");
    }

    [Fact]
    public async Task Point_metadata_drives_read_and_write_editor_fields()
    {
        var catalog = new DeviceModelCatalog();
        var vfdModel = catalog.DeviceModels.Single(model => model.Name == "\u6807\u51C6 VFD");
        vfdModel.Points.Add(new LogicalPointRowViewModel(
            "Vfd:Speed",
            "VFD \u8F6C\u901F",
            "VFD",
            "\u8BFB\u53D6",
            "03",
            "40060",
            "Int16",
            "rpm",
            "\u6574\u578B\u8F6C\u901F",
            isCustom: true));

        var plan = new ProcessPlan(Guid.NewGuid(), "VFD production test plan");
        var viewModel = CreateEditorViewModel(new FakeProcessPlanRepository([plan]), catalog);
        await viewModel.LoadPlanAsync(plan);
        viewModel.AddBlankStepCommand.Execute(null);

        viewModel.SelectedStep!.Target = "Vfd:Control";
        viewModel.SelectedStep.CommandType.Should().Be("Start");
        viewModel.SelectedStep.GenericValueVisibility.Should().Be("Collapsed");
        viewModel.SelectedStep.WriteValueVisibility.Should().Be("Visible");
        viewModel.SelectedStep.NumericRangeVisibility.Should().Be("Collapsed");
        viewModel.SelectedStep.StringExpectedVisibility.Should().Be("Collapsed");
        viewModel.SelectedStepWriteValueOptions.Should().Contain(option =>
            option.Value == "5" && option.DisplayName == "\u81EA\u7531\u505C\u673A");

        viewModel.SelectedStep.Target = "Vfd:Speed";
        viewModel.SelectedStep.CommandType.Should().Be("ReadMeasurement");
        viewModel.SelectedStep.GenericValueVisibility.Should().Be("Collapsed");
        viewModel.SelectedStep.WriteValueVisibility.Should().Be("Collapsed");
        viewModel.SelectedStep.NumericRangeVisibility.Should().Be("Visible");
        viewModel.SelectedStep.StringExpectedVisibility.Should().Be("Collapsed");

        viewModel.SelectedStep.Target = "Vfd:State";
        viewModel.SelectedStep.CommandType.Should().Be("ReadMeasurement");
        viewModel.SelectedStep.GenericValueVisibility.Should().Be("Collapsed");
        viewModel.SelectedStep.WriteValueVisibility.Should().Be("Collapsed");
        viewModel.SelectedStep.NumericRangeVisibility.Should().Be("Visible");
        viewModel.SelectedStep.StringExpectedVisibility.Should().Be("Collapsed");
    }

    [Fact]
    public async Task Vfd_control_write_value_drives_command_summary_and_saved_value()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD production test plan");
        var repository = new FakeProcessPlanRepository([plan]);
        var viewModel = CreateEditorViewModel(repository);
        await viewModel.LoadPlanAsync(plan);
        viewModel.AddStopStepCommand.Execute(null);

        viewModel.SelectedStep!.Value = "5";

        viewModel.SelectedStep.CommandType.Should().Be("Stop");
        viewModel.SelectedStep.ConditionSummary.Should().Be("\u5199\u5165\u81EA\u7531\u505C\u673A");
        viewModel.SelectedStep.ParameterSummary.Should().Be("Vfd:Control / \u81EA\u7531\u505C\u673A (5)");

        await viewModel.SaveVersionCommand.ExecuteAsync(null);

        var savedStep = plan.Versions.Single().Steps.Single();
        savedStep.Command.CommandType.Should().Be("Stop");
        savedStep.Command.Value.Should().Be("5");
    }

    [Fact]
    public async Task Stop_step_defaults_to_deceleration_stop_for_normal_production_flow()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD production test plan");
        var repository = new FakeProcessPlanRepository([plan]);
        var viewModel = CreateEditorViewModel(repository);
        await viewModel.LoadPlanAsync(plan);

        viewModel.AddStopStepCommand.Execute(null);

        viewModel.SelectedStep!.Value.Should().Be("6");
        viewModel.SelectedStep.ConditionSummary.Should().Be("\u5199\u5165\u51CF\u901F\u505C\u673A");

        await viewModel.SaveVersionCommand.ExecuteAsync(null);

        var savedStep = plan.Versions.Single().Steps.Single();
        savedStep.Command.CommandType.Should().Be("Stop");
        savedStep.Command.Value.Should().Be("6");
    }

    [Fact]
    public async Task Unknown_vfd_control_write_value_remains_editable_and_visible()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD production test plan");
        var viewModel = CreateEditorViewModel([plan]);
        await viewModel.LoadPlanAsync(plan);
        viewModel.AddStartStepCommand.Execute(null);

        viewModel.SelectedStep!.Value = "99";

        viewModel.SelectedStep.ConditionSummary.Should().Be("\u5199\u5165\u503C 99");
        viewModel.SelectedStep.ParameterSummary.Should().Be("Vfd:Control / 99");
    }

    [Fact]
    public async Task Compare_measurement_step_edits_two_points_and_tolerance_separately()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD production test plan");
        var repository = new FakeProcessPlanRepository([plan]);
        var viewModel = CreateEditorViewModel(repository);
        await viewModel.LoadPlanAsync(plan);
        viewModel.AddCompareMeasurementStepCommand.Execute(null);

        var step = viewModel.SelectedStep!;
        step.SinglePointVisibility.Should().Be("Collapsed");
        step.CompareSettingsVisibility.Should().Be("Visible");
        step.CompareLeftTarget.Should().Be("Vfd:Voltage");
        step.CompareRightTarget.Should().Be("Instrument:Voltage");
        step.ToleranceType.Should().Be("Absolute");
        step.ToleranceValue.Should().Be("2");
        step.Name.Should().Be("\u7535\u538B\u8BFB\u6570\u6BD4\u5BF9");
        step.StepTypeDisplay.Should().Be("\u70B9\u4F4D\u8BFB\u6570\u6BD4\u5BF9");

        step.CompareLeftTarget = "Instrument:Voltage";
        step.CompareRightTarget = "Vfd:Voltage";
        step.ToleranceType = "Percent";
        step.ToleranceValue = "1.5";

        await viewModel.SaveVersionCommand.ExecuteAsync(null);

        var savedStep = plan.Versions.Single().Steps.Single();
        savedStep.Command.Target.Should().Be("Instrument:Voltage|Vfd:Voltage");
        savedStep.Command.Value.Should().Be("Percent:1.5");
        step.ParameterSummary.Should().Be("Instrument:Voltage vs Vfd:Voltage / Percent:1.5");
    }

    [Fact]
    public async Task Step_grid_separates_target_and_condition_summaries()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD production test plan");
        var viewModel = CreateEditorViewModel([plan]);
        await viewModel.LoadPlanAsync(plan);

        viewModel.AddStartStepCommand.Execute(null);
        viewModel.AddDelayStepCommand.Execute(null);
        viewModel.AddReadVfdMeasurementStepCommand.Execute(null);
        viewModel.SelectedStep!.LowerLimit = "210";
        viewModel.SelectedStep.UpperLimit = "230";
        viewModel.AddBlankStepCommand.Execute(null);
        viewModel.SelectedStep!.Target = "Vfd:State";
        viewModel.SelectedStep.LowerLimit = "1";
        viewModel.SelectedStep.UpperLimit = "2";
        viewModel.AddCompareMeasurementStepCommand.Execute(null);
        viewModel.SelectedStep!.ToleranceType = "Percent";
        viewModel.SelectedStep.ToleranceValue = "1.5";

        viewModel.Steps.Select(step => step.TargetSummary)
            .Should()
            .Equal("Vfd:Control", "Timer", "Vfd:Voltage", "Vfd:State", "Vfd:Voltage vs Instrument:Voltage");
        viewModel.Steps.Select(step => step.ConditionSummary)
            .Should()
            .Equal("写入正转运行", "5000 ms", "210 ~ 230", "1 ~ 2", "百分比 ±1.5%");
    }

    [Fact]
    public async Task Step_row_commands_copy_remove_and_reorder_steps()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD production test plan");
        var viewModel = CreateEditorViewModel([plan]);
        await viewModel.LoadPlanAsync(plan);
        viewModel.AddStartStepCommand.Execute(null);
        viewModel.AddStopStepCommand.Execute(null);

        viewModel.SelectedStep = viewModel.Steps[0];
        viewModel.CopySelectedStepCommand.Execute(null);
        viewModel.MoveSelectedStepDownCommand.Execute(null);
        viewModel.RemoveSelectedStepCommand.Execute(null);

        viewModel.Steps.Select(step => step.Sequence).Should().Equal(1, 2);
        viewModel.Steps.Select(step => step.CommandType).Should().Equal("Start", "Stop");
        viewModel.SelectedStep.Should().BeSameAs(viewModel.Steps[1]);
    }

    [Fact]
    public async Task Saving_editor_steps_creates_new_version()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD 生产测试演示方案");
        var repository = new FakeProcessPlanRepository([plan]);
        var viewModel = CreateEditorViewModel(repository);
        await viewModel.LoadPlanAsync(plan);
        viewModel.AddStartStepCommand.Execute(null);

        await viewModel.SaveVersionCommand.ExecuteAsync(null);

        plan.Versions.Should().ContainSingle();
        viewModel.StatusMessage.Should().Be("已保存为 v1。");
    }

    [Fact]
    public void Workflow_editor_no_longer_exposes_common_step_template_management()
    {
        var viewModelType = typeof(WorkflowEditorViewModel);

        viewModelType.GetProperty("CommandTemplates").Should().BeNull();
        viewModelType.GetProperty("SelectedCommandTemplate").Should().BeNull();
        viewModelType.GetProperty("IsTemplateManagerVisible").Should().BeNull();
        viewModelType.GetProperty("AddSelectedCommandTemplateCommand").Should().BeNull();
        viewModelType.GetProperty("AddCustomCommandTemplateCommand").Should().BeNull();
        viewModelType.GetProperty("RemoveSelectedCommandTemplateCommand").Should().BeNull();
    }

    [Fact]
    public void Editor_point_options_refresh_when_admin_edits_logical_point_properties()
    {
        var catalog = new DeviceModelCatalog();
        var viewModel = CreateEditorViewModel(new FakeProcessPlanRepository(), catalog);
        var point = catalog.DeviceModels
            .Single(model => model.Name == "\u6807\u51C6 VFD")
            .Points
            .Single(point => point.LogicalKey == "Vfd:Voltage");

        point.LogicalKey = "Vfd:test";
        point.DisplayName = "\u6D4B\u8BD51";

        viewModel.LogicalPointOptions.Should().Contain(option =>
            option.LogicalKey == "Vfd:test"
            && option.DisplayNameWithKey == "\u6D4B\u8BD51 (Vfd:test)");
    }

    [Fact]
    public async Task Selecting_custom_decimal_logical_point_maps_step_to_read_measurement()
    {
        var catalog = new DeviceModelCatalog();
        var vfdModel = catalog.DeviceModels.Single(model => model.Name == "\u6807\u51C6 VFD");
        vfdModel.Points.Add(new LogicalPointRowViewModel(
            "Vfd:test",
            "\u6D4B\u8BD51",
            "VFD",
            "\u8BFB\u53D6",
            "03",
            "40050",
            "Decimal",
            "",
            "\u81EA\u5B9A\u4E49\u70B9\u4F4D",
            isCustom: true));

        var plan = new ProcessPlan(Guid.NewGuid(), "VFD production test plan");
        var viewModel = CreateEditorViewModel(new FakeProcessPlanRepository([plan]), catalog);
        await viewModel.LoadPlanAsync(plan);
        viewModel.AddBlankStepCommand.Execute(null);

        viewModel.SelectedLogicalPointOptions.Should().Contain(point =>
            point.LogicalKey == "Vfd:test" && point.DisplayNameWithKey == "\u6D4B\u8BD51 (Vfd:test)");

        viewModel.SelectedStep!.Target = "Vfd:test";

        viewModel.SelectedStep.CommandType.Should().Be("ReadMeasurement");
        viewModel.SelectedStep.Target.Should().Be("Vfd:test");
    }

    [Fact]
    public async Task Adding_draft_logical_point_step_uses_custom_admin_point()
    {
        var catalog = new DeviceModelCatalog();
        var vfdModel = catalog.DeviceModels.Single(model => model.Name == "\u6807\u51C6 VFD");
        vfdModel.Points.Add(new LogicalPointRowViewModel(
            "Vfd:test",
            "\u6D4B\u8BD51",
            "VFD",
            "\u8BFB\u53D6",
            "03",
            "40050",
            "Decimal",
            "",
            "\u81EA\u5B9A\u4E49\u70B9\u4F4D",
            isCustom: true));

        var plan = new ProcessPlan(Guid.NewGuid(), "VFD production test plan");
        var viewModel = CreateEditorViewModel(new FakeProcessPlanRepository([plan]), catalog);
        await viewModel.LoadPlanAsync(plan);
        viewModel.SelectedDraftLogicalPointKey = "Vfd:test";
        viewModel.DraftStepName = "\u8BFB\u53D6\u6D4B\u8BD51";
        viewModel.DraftStepValue = "";

        viewModel.AddDraftLogicalPointStepCommand.Execute(null);

        viewModel.Steps.Should().ContainSingle();
        viewModel.SelectedStep!.Name.Should().Be("\u8BFB\u53D6\u6D4B\u8BD51");
        viewModel.SelectedStep.CommandType.Should().Be("ReadMeasurement");
        viewModel.SelectedStep.Target.Should().Be("Vfd:test");
        viewModel.SelectedStep.Value.Should().Be("");
    }

    [Fact]
    public void Draft_write_point_creates_unconfigured_write_step_for_property_panel_editing()
    {
        var viewModel = CreateEditorViewModel(new FakeProcessPlanRepository());

        viewModel.SelectedDraftLogicalPointKey = "Vfd:Control";

        viewModel.DraftStepName.Should().Be("\u5199\u5165 VFD \u542F\u505C\u63A7\u5236");
        viewModel.DraftStepValue.Should().BeEmpty();

        viewModel.AddDraftLogicalPointStepCommand.Execute(null);

        viewModel.Steps.Should().ContainSingle();
        viewModel.SelectedStep!.CommandType.Should().Be("Start");
        viewModel.SelectedStep.Target.Should().Be("Vfd:Control");
        viewModel.SelectedStep.Value.Should().BeEmpty();
        viewModel.SelectedStepWriteValueOptions.Should().Contain(option =>
            option.Value == "1" && option.DisplayName == "\u6B63\u8F6C\u8FD0\u884C");
    }

    [Fact]
    public async Task Validation_and_save_reject_write_steps_without_write_command()
    {
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD production test plan");
        var repository = new FakeProcessPlanRepository([plan]);
        var viewModel = CreateEditorViewModel(repository);
        await viewModel.LoadPlanAsync(plan);

        viewModel.SelectedDraftLogicalPointKey = "Vfd:Control";
        viewModel.AddDraftLogicalPointStepCommand.Execute(null);

        viewModel.ValidateWorkflowCommand.Execute(null);

        viewModel.StatusMessage.Should().Contain("\u7F3A\u5C11\u5199\u5165\u547D\u4EE4");

        await viewModel.SaveVersionCommand.ExecuteAsync(null);

        plan.Versions.Should().BeEmpty();
        viewModel.StatusMessage.Should().Contain("\u7F3A\u5C11\u5199\u5165\u547D\u4EE4");
    }

    private static PlanListViewModel CreatePlanListViewModel(IReadOnlyList<ProcessPlan> plans)
    {
        return new PlanListViewModel(new ProcessPlanService(new FakeProcessPlanRepository(plans)));
    }

    private static WorkflowEditorViewModel CreateEditorViewModel(IReadOnlyList<ProcessPlan> plans)
    {
        return CreateEditorViewModel(new FakeProcessPlanRepository(plans));
    }

    private static WorkflowEditorViewModel CreateEditorViewModel(FakeProcessPlanRepository repository)
    {
        return CreateEditorViewModel(repository, new DeviceModelCatalog());
    }

    private static WorkflowEditorViewModel CreateEditorViewModel(
        FakeProcessPlanRepository repository,
        DeviceModelCatalog catalog)
    {
        var workflowDefinitionService = new WorkflowDefinitionService();
        return new WorkflowEditorViewModel(
            new ProcessPlanService(repository, workflowDefinitionService),
            workflowDefinitionService,
            catalog);
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
