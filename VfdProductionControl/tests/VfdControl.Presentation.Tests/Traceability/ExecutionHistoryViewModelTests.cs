using FluentAssertions;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;
using VfdControl.Application.Traceability;
using VfdControl.Domain.Enums;
using VfdControl.Domain.ValueObjects;
using VfdControl.Presentation.Traceability;

namespace VfdControl.Presentation.Tests.Traceability;

public class ExecutionHistoryViewModelTests
{
    [Fact]
    public async Task Search_filters_runs_and_opening_result_loads_trace_details()
    {
        var repository = new FakeTraceRepository();
        var sessionId = Guid.NewGuid();
        var deviceRunId = Guid.NewGuid();
        var stepRunId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        await repository.SaveSessionStartedAsync(new StationSessionSnapshot(sessionId, Guid.NewGuid(), "EMP0001", DateTimeOffset.UtcNow), CancellationToken.None);
        await repository.SaveDeviceRunAsync(new DeviceRunSnapshot(deviceRunId, sessionId, slotId, "VFD202606020001", Conclusion.Pass), CancellationToken.None);
        await repository.SaveDeviceRunAsync(new DeviceRunSnapshot(Guid.NewGuid(), sessionId, Guid.NewGuid(), "VFD202606020002", Conclusion.Fail), CancellationToken.None);
        await repository.SaveStepRunAsync(new StepRunSnapshot(stepRunId, deviceRunId, 1, "读取 VFD 电压", Conclusion.Pass), CancellationToken.None);
        await repository.SaveMeasurementResultAsync(new MeasurementTrace(stepRunId, "Vfd:Voltage", 220.5, "V", MeasurementSource.Vfd), CancellationToken.None);
        await repository.SaveCommandTraceAsync(
            new CommandTraceSnapshot(Guid.NewGuid(), stepRunId, slotId, "Vfd:Voltage", """{"request":"read"}""", """{"value":220.5}""", true, DateTimeOffset.UtcNow),
            CancellationToken.None);
        var detailViewModel = new DeviceRunTraceViewModel();
        var viewModel = new ExecutionHistoryViewModel(new TraceabilityQueryService(repository), detailViewModel);

        viewModel.BarcodeFilter = "vfd202606020001";
        await viewModel.SearchAsync();
        await viewModel.OpenDeviceRunAsync(viewModel.DeviceRuns.Single());

        viewModel.DeviceRuns.Should().ContainSingle();
        viewModel.DeviceRuns[0].Barcode.Should().Be("VFD202606020001");
        detailViewModel.Title.Should().Be("VFD202606020001");
        detailViewModel.Steps.Should().ContainSingle();
        detailViewModel.Steps[0].Measurements.Should().ContainSingle(measurement => measurement.DisplayValue == "220.5 V");
        detailViewModel.Steps[0].CommandTraces.Should().ContainSingle(command => command.ResponseJson.Contains("220.5"));
    }

    private sealed class FakeTraceRepository : ITraceRepository
    {
        private readonly List<StationSessionSnapshot> _sessions = [];
        private readonly List<DeviceRunSnapshot> _runs = [];
        private readonly List<StepRunSnapshot> _steps = [];
        private readonly List<MeasurementTrace> _measurements = [];
        private readonly List<ComparisonTrace> _comparisons = [];
        private readonly List<CommandTraceSnapshot> _commands = [];

        public Task SaveSessionStartedAsync(StationSessionSnapshot session, CancellationToken ct)
        {
            _sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task SaveDeviceRunAsync(DeviceRunSnapshot run, CancellationToken ct)
        {
            _runs.Add(run);
            return Task.CompletedTask;
        }

        public Task SaveStepRunAsync(StepRunSnapshot step, CancellationToken ct)
        {
            _steps.Add(step);
            return Task.CompletedTask;
        }

        public Task SaveMeasurementResultAsync(MeasurementTrace measurement, CancellationToken ct)
        {
            _measurements.Add(measurement);
            return Task.CompletedTask;
        }

        public Task SaveComparisonResultAsync(ComparisonTrace comparison, CancellationToken ct)
        {
            _comparisons.Add(comparison);
            return Task.CompletedTask;
        }

        public Task SaveCommandTraceAsync(CommandTraceSnapshot trace, CancellationToken ct)
        {
            _commands.Add(trace);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<StationSessionSummary>> QuerySessionsAsync(TraceabilitySessionQuery query, CancellationToken ct)
        {
            var summaries = _sessions
                .Select(session => new StationSessionSummary(session.SessionId, session.StationId, session.OperatorCode, session.StartedAt, Conclusion.Pass, 1))
                .ToList();
            return Task.FromResult<IReadOnlyList<StationSessionSummary>>(summaries);
        }

        public Task<IReadOnlyList<DeviceRunSummary>> QueryDeviceRunsAsync(DeviceRunQuery query, CancellationToken ct)
        {
            var runs = _runs
                .Where(run => query.Barcode is null || string.Equals(run.Barcode, query.Barcode, StringComparison.OrdinalIgnoreCase))
                .Where(run => query.Conclusion is null || run.Conclusion == query.Conclusion)
                .Select(run => new DeviceRunSummary(run.DeviceRunId, run.SessionId, run.Barcode, run.Conclusion, DateTimeOffset.UtcNow))
                .ToList();
            return Task.FromResult<IReadOnlyList<DeviceRunSummary>>(runs);
        }

        public Task<DeviceRunTrace?> GetDeviceRunTraceAsync(Guid deviceRunId, CancellationToken ct)
        {
            var run = _runs.SingleOrDefault(run => run.DeviceRunId == deviceRunId);
            if (run is null)
            {
                return Task.FromResult<DeviceRunTrace?>(null);
            }

            var steps = _steps
                .Where(step => step.DeviceRunId == deviceRunId)
                .Select(step => new StepRunTrace(
                    step.StepRunId,
                    step.DeviceRunId,
                    step.Sequence,
                    step.StepName,
                    step.Conclusion,
                    _measurements.Where(measurement => measurement.StepRunId == step.StepRunId).ToList(),
                    _comparisons.Where(comparison => comparison.StepRunId == step.StepRunId).ToList(),
                    _commands.Where(command => command.StepRunId == step.StepRunId).ToList(),
                    step.Message))
                .ToList();

            return Task.FromResult<DeviceRunTrace?>(new DeviceRunTrace(
                run.DeviceRunId,
                run.SessionId,
                run.SlotId,
                run.Barcode,
                run.Conclusion,
                DateTimeOffset.UtcNow,
                steps));
        }
    }
}
