using FluentAssertions;
using VfdControl.Application.Execution;
using VfdControl.Application.Tests.TestDoubles;
using VfdControl.Application.Traceability;
using VfdControl.Domain.Enums;

namespace VfdControl.Application.Tests.Traceability;

public class TraceabilityQueryServiceTests
{
    [Fact]
    public async Task Query_sessions_by_date_range_returns_matching_sessions()
    {
        var repository = new InMemoryTraceRepository();
        var service = new TraceabilityQueryService(repository);
        var matchingSession = new StationSessionSnapshot(Guid.NewGuid(), Guid.NewGuid(), "EMP001", new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero));
        var oldSession = new StationSessionSnapshot(Guid.NewGuid(), Guid.NewGuid(), "EMP002", new DateTimeOffset(2026, 5, 30, 9, 0, 0, TimeSpan.Zero));
        await repository.SaveSessionStartedAsync(matchingSession, CancellationToken.None);
        await repository.SaveSessionStartedAsync(oldSession, CancellationToken.None);

        var results = await service.QuerySessionsAsync(
            new TraceabilitySessionQuery(
                From: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                To: new DateTimeOffset(2026, 6, 3, 0, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        results.Should().ContainSingle()
            .Which.SessionId.Should().Be(matchingSession.SessionId);
    }

    [Fact]
    public async Task Query_device_runs_by_barcode_returns_matching_run()
    {
        var repository = new InMemoryTraceRepository();
        var service = new TraceabilityQueryService(repository);
        var sessionId = Guid.NewGuid();
        var run = new DeviceRunSnapshot(Guid.NewGuid(), sessionId, Guid.NewGuid(), "VFD202606020001", Conclusion.Pass);
        await repository.SaveDeviceRunAsync(run, CancellationToken.None);

        var results = await service.QueryDeviceRunsAsync(
            new DeviceRunQuery(Barcode: "vfd202606020001"),
            CancellationToken.None);

        results.Should().ContainSingle()
            .Which.DeviceRunId.Should().Be(run.DeviceRunId);
    }

    [Fact]
    public async Task Device_run_details_include_steps_measurements_comparisons_and_command_traces()
    {
        var repository = new InMemoryTraceRepository();
        var service = new TraceabilityQueryService(repository);
        var deviceRunId = Guid.NewGuid();
        var stepRunId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        await repository.SaveDeviceRunAsync(
            new DeviceRunSnapshot(deviceRunId, Guid.NewGuid(), slotId, "VFD202606020002", Conclusion.Pass),
            CancellationToken.None);
        await repository.SaveStepRunAsync(
            new StepRunSnapshot(stepRunId, deviceRunId, 1, "读取 VFD 电压", Conclusion.Pass),
            CancellationToken.None);
        await repository.SaveMeasurementResultAsync(
            new MeasurementTrace(stepRunId, "Vfd:Voltage", 220.5, "V", MeasurementSource.Vfd),
            CancellationToken.None);
        await repository.SaveComparisonResultAsync(
            new ComparisonTrace(stepRunId, "Vfd:Voltage", "Instrument:Voltage", Conclusion.Pass, "差值在允许范围内。"),
            CancellationToken.None);
        await repository.SaveCommandTraceAsync(
            new CommandTraceSnapshot(Guid.NewGuid(), stepRunId, slotId, "Vfd:Voltage", """{"request":"read"}""", """{"value":220.5}""", true, DateTimeOffset.UtcNow),
            CancellationToken.None);

        var trace = await service.GetDeviceRunTraceAsync(deviceRunId, CancellationToken.None);

        trace.Should().NotBeNull();
        trace!.Steps.Should().ContainSingle();
        var step = trace.Steps[0];
        step.Measurements.Should().ContainSingle(measurement => measurement.Key == "Vfd:Voltage" && measurement.NumericValue == 220.5);
        step.Comparisons.Should().ContainSingle(comparison => comparison.LeftKey == "Vfd:Voltage" && comparison.RightKey == "Instrument:Voltage");
        step.CommandTraces.Should().ContainSingle(command => command.RequestJson.Contains("request") && command.ResponseJson.Contains("220.5"));
    }
}
