using FluentAssertions;
using VfdControl.Application.Execution;
using VfdControl.Application.Tests.TestDoubles;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;
using VfdControl.Domain.Stations;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Application.Tests.Execution;

public class WorkflowEngineTests
{
    [Fact]
    public async Task Software_start_writes_start_command_before_reads()
    {
        var deviceClient = new FakeDeviceCommandClient();
        var slot = CreateSlot();
        deviceClient.SetMeasurement(slot.Id, MeasurementSource.Vfd, "Voltage", new MeasurementValue(220, "V", MeasurementSource.Vfd));

        var engine = new WorkflowEngine(deviceClient, new InMemoryTraceRepository());
        var context = CreateContext(slot, [
            Step(1, "Start", "Start", "Vfd:Control"),
            Step(2, "Read VFD voltage", "ReadMeasurement", "Vfd:Voltage")
        ]);

        await engine.ExecuteAsync(context, CancellationToken.None);

        deviceClient.WriteLog.Should().ContainSingle();
        deviceClient.WriteLog[0].CommandName.Should().Be("Start");
    }

    [Fact]
    public async Task Delay_step_is_executed_in_order()
    {
        var repository = new InMemoryTraceRepository();
        var engine = new WorkflowEngine(new FakeDeviceCommandClient(), repository);
        var context = CreateContext(CreateSlot(), [
            Step(1, "Delay", "Delay", "Timer", "1"),
            Step(2, "Stop", "Stop", "Vfd:Control")
        ]);

        await engine.ExecuteAsync(context, CancellationToken.None);

        repository.Steps.Select(step => step.StepName).Should().Equal("Delay", "Stop");
    }

    [Fact]
    public async Task Delay_step_waits_for_configured_milliseconds_before_next_step()
    {
        var deviceClient = new FakeDeviceCommandClient();
        var engine = new WorkflowEngine(deviceClient, new InMemoryTraceRepository());
        var context = CreateContext(CreateSlot(), [
            Step(1, "Delay", "Delay", "Timer", "150"),
            Step(2, "Stop", "Stop", "Vfd:Control")
        ]);
        var startedAt = DateTimeOffset.UtcNow;

        await engine.ExecuteAsync(context, CancellationToken.None);

        (DateTimeOffset.UtcNow - startedAt).Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(130));
        deviceClient.WriteLog.Should().ContainSingle(entry => entry.CommandName == "Stop");
    }

    [Fact]
    public async Task Delay_step_reports_countdown_progress_before_completion()
    {
        var engine = new WorkflowEngine(new FakeDeviceCommandClient(), new InMemoryTraceRepository());
        var progress = new List<SlotStepProgressSnapshot>();
        var context = CreateContext(CreateSlot(), [
            Step(1, "Delay", "Delay", "Timer", "120")
        ], progress.Add);

        await engine.ExecuteAsync(context, CancellationToken.None);

        progress.Any(snapshot =>
            snapshot.Sequence == 1
            && snapshot.IsRunning
            && snapshot.Remaining is not null
            && snapshot.Remaining.Value > TimeSpan.Zero)
            .Should()
            .BeTrue();
        progress.Last(snapshot => snapshot.Sequence == 1).Conclusion.Should().Be(Conclusion.Pass);
    }

    [Fact]
    public async Task Vfd_and_instrument_voltage_are_read_and_compared()
    {
        var deviceClient = new FakeDeviceCommandClient();
        var slot = CreateSlot();
        deviceClient.SetMeasurement(slot.Id, MeasurementSource.Vfd, "Voltage", new MeasurementValue(220, "V", MeasurementSource.Vfd));
        deviceClient.SetMeasurement(slot.Id, MeasurementSource.Instrument, "Voltage", new MeasurementValue(219, "V", MeasurementSource.Instrument));

        var engine = new WorkflowEngine(deviceClient, new InMemoryTraceRepository());
        var context = CreateContext(slot, [
            Step(1, "Read VFD voltage", "ReadMeasurement", "Vfd:Voltage"),
            Step(2, "Read instrument voltage", "ReadMeasurement", "Instrument:Voltage"),
            Step(3, "Compare voltage", "CompareMeasurement", "Vfd:Voltage|Instrument:Voltage", "Absolute:2")
        ]);

        var result = await engine.ExecuteAsync(context, CancellationToken.None);

        result.Conclusion.Should().Be(Conclusion.Pass);
    }

    [Fact]
    public async Task Read_measurement_step_fails_when_value_is_outside_configured_range()
    {
        var deviceClient = new FakeDeviceCommandClient();
        var slot = CreateSlot();
        deviceClient.SetMeasurement(slot.Id, MeasurementSource.Vfd, "Voltage", new MeasurementValue(240, "V", MeasurementSource.Vfd));

        var engine = new WorkflowEngine(deviceClient, new InMemoryTraceRepository());
        var context = CreateContext(slot, [
            Step(1, "Read VFD voltage", "ReadMeasurement", "Vfd:Voltage", rule: StepRule.NumericRange(210, 230))
        ]);

        var result = await engine.ExecuteAsync(context, CancellationToken.None);

        result.Conclusion.Should().Be(Conclusion.Fail);
        result.Steps.Single().Conclusion.Should().Be(Conclusion.Fail);
    }

    [Fact]
    public async Task Failed_comparison_with_continue_and_mark_fail_continues_later_steps_but_final_result_is_fail()
    {
        var deviceClient = new FakeDeviceCommandClient();
        var slot = CreateSlot();
        deviceClient.SetMeasurement(slot.Id, MeasurementSource.Vfd, "Voltage", new MeasurementValue(225, "V", MeasurementSource.Vfd));
        deviceClient.SetMeasurement(slot.Id, MeasurementSource.Instrument, "Voltage", new MeasurementValue(219, "V", MeasurementSource.Instrument));

        var engine = new WorkflowEngine(deviceClient, new InMemoryTraceRepository());
        var context = CreateContext(slot, [
            Step(1, "Read VFD voltage", "ReadMeasurement", "Vfd:Voltage"),
            Step(2, "Read instrument voltage", "ReadMeasurement", "Instrument:Voltage"),
            Step(3, "Compare voltage", "CompareMeasurement", "Vfd:Voltage|Instrument:Voltage", "Absolute:2", FailureAction.ContinueAndMarkFail),
            Step(4, "Stop", "Stop", "Vfd:Control")
        ]);

        var result = await engine.ExecuteAsync(context, CancellationToken.None);

        result.Conclusion.Should().Be(Conclusion.Fail);
        deviceClient.WriteLog.Should().Contain(entry => entry.CommandName == "Stop");
    }

    [Fact]
    public async Task Failed_string_comparison_with_stop_slot_immediately_writes_stop_command_and_ends_slot()
    {
        var deviceClient = new FakeDeviceCommandClient();
        var slot = CreateSlot();
        deviceClient.SetString(slot.Id, MeasurementSource.Vfd, "State", "STOPPED");
        deviceClient.SetMeasurement(slot.Id, MeasurementSource.Vfd, "Voltage", new MeasurementValue(220, "V", MeasurementSource.Vfd));

        var repository = new InMemoryTraceRepository();
        var engine = new WorkflowEngine(deviceClient, repository);
        var context = CreateContext(slot, [
            Step(1, "Read state", "ReadString", "Vfd:State", "READY", FailureAction.StopSlotImmediately),
            Step(2, "Read after stop", "ReadMeasurement", "Vfd:Voltage")
        ]);

        var result = await engine.ExecuteAsync(context, CancellationToken.None);

        result.Conclusion.Should().Be(Conclusion.Fail);
        deviceClient.WriteLog.Should().Contain(entry => entry.CommandName == "Stop" && entry.Value == "5");
        repository.Steps.Select(step => step.StepName).Should().NotContain("Read after stop");
    }

    [Fact]
    public async Task Normal_completion_writes_stop_command()
    {
        var deviceClient = new FakeDeviceCommandClient();
        var engine = new WorkflowEngine(deviceClient, new InMemoryTraceRepository());
        var context = CreateContext(CreateSlot(), [
            Step(1, "Start", "Start", "Vfd:Control"),
            Step(2, "Stop", "Stop", "Vfd:Control")
        ]);

        var result = await engine.ExecuteAsync(context, CancellationToken.None);

        result.Conclusion.Should().Be(Conclusion.Pass);
        deviceClient.WriteLog.Should().Contain(entry => entry.CommandName == "Stop");
    }

    [Fact]
    public async Task Command_trace_records_request_and_response_from_device_result()
    {
        var deviceClient = new FakeDeviceCommandClient();
        var repository = new InMemoryTraceRepository();
        var engine = new WorkflowEngine(deviceClient, repository);
        var context = CreateContext(CreateSlot(), [
            Step(1, "Start", "Start", "Vfd:Control", "1")
        ]);

        await engine.ExecuteAsync(context, CancellationToken.None);

        repository.CommandTraces.Should().ContainSingle();
        repository.CommandTraces[0].RequestJson.Should().Contain("Start");
        repository.CommandTraces[0].RequestJson.Should().NotBe("{}");
        repository.CommandTraces[0].ResponseJson.Should().Contain("Success");
    }

    private static DeviceRunContext CreateContext(
        StationSlot slot,
        IReadOnlyList<ProcessStep> steps,
        Action<SlotStepProgressSnapshot>? progressHandler = null)
    {
        var version = new ProcessPlanVersion(Guid.NewGuid(), versionNumber: 1, isExecutable: true);
        foreach (var step in steps)
        {
            version.AddStep(step);
        }

        return new DeviceRunContext(
            SessionId: Guid.NewGuid(),
            DeviceRunId: Guid.NewGuid(),
            Slot: slot,
            Barcode: Barcode.TryCreateVfd("VFD202606010001").Value!,
            PlanVersion: version,
            ProgressHandler: progressHandler);
    }

    private static StationSlot CreateSlot()
    {
        return new StationSlot(
            Guid.NewGuid(),
            new SlotNumber(1),
            new SlotCommunicationConfig(new SerialPortName("COM1"), new ModbusAddress(1), 9600));
    }

    private static ProcessStep Step(
        int sequence,
        string name,
        string commandType,
        string target,
        string? value = null,
        FailureAction failureAction = FailureAction.ContinueAndMarkFail,
        bool affectsFinalConclusion = true,
        StepRule? rule = null)
    {
        return new ProcessStep(
            Guid.NewGuid(),
            sequence,
            name,
            new StepCommand(commandType, target, value),
            new StepFailurePolicy(failureAction),
            affectsFinalConclusion,
            rule);
    }
}
