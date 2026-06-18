using Dapper;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;
using VfdControl.Application.Traceability;
using VfdControl.Domain.Enums;

namespace VfdControl.Infrastructure.Sql;

public sealed class SqlTraceRepository : ITraceRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public SqlTraceRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveSessionStartedAsync(StationSessionSnapshot session, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.StationSessions (SessionId, StationId, OperatorCode, StartedAt, SessionJson)
            VALUES (@SessionId, @StationId, @OperatorCode, @StartedAt, @SessionJson);
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            session.SessionId,
            session.StationId,
            session.OperatorCode,
            session.StartedAt,
            SessionJson = "{}"
        });
    }

    public async Task SaveDeviceRunAsync(DeviceRunSnapshot run, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.DeviceRuns (DeviceRunId, SessionId, SlotId, Barcode, Conclusion, RunJson)
            VALUES (@DeviceRunId, @SessionId, @SlotId, @Barcode, @Conclusion, @RunJson);
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            run.DeviceRunId,
            run.SessionId,
            run.SlotId,
            run.Barcode,
            Conclusion = run.Conclusion.ToString(),
            RunJson = "{}"
        });
    }

    public async Task SaveStepRunAsync(StepRunSnapshot step, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.StepRuns (StepRunId, DeviceRunId, Sequence, StepName, Conclusion, Message, StepJson)
            VALUES (@StepRunId, @DeviceRunId, @Sequence, @StepName, @Conclusion, @Message, @StepJson);
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            step.StepRunId,
            step.DeviceRunId,
            step.Sequence,
            step.StepName,
            Conclusion = step.Conclusion.ToString(),
            step.Message,
            StepJson = "{}"
        });
    }

    public async Task SaveMeasurementResultAsync(MeasurementTrace measurement, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.MeasurementResults (StepRunId, Source, PointKey, NumericValue, Unit, MeasurementJson)
            VALUES (@StepRunId, @Source, @PointKey, @NumericValue, @Unit, @MeasurementJson);
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            measurement.StepRunId,
            Source = measurement.Source.ToString(),
            PointKey = measurement.Key,
            measurement.NumericValue,
            measurement.Unit,
            MeasurementJson = "{}"
        });
    }

    public async Task SaveComparisonResultAsync(ComparisonTrace comparison, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.ComparisonResults
                (StepRunId, LeftKey, RightKey, Conclusion, Message, ComparisonJson)
            VALUES
                (@StepRunId, @LeftKey, @RightKey, @Conclusion, @Message, @ComparisonJson);
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            comparison.StepRunId,
            comparison.LeftKey,
            comparison.RightKey,
            Conclusion = comparison.Conclusion.ToString(),
            comparison.Message,
            ComparisonJson = "{}"
        });
    }

    public async Task SaveCommandTraceAsync(CommandTraceSnapshot trace, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.CommandTraces
                (TraceId, StepRunId, SlotId, CommandName, RequestJson, ResponseJson, IsSuccess, CreatedAt)
            VALUES
                (@TraceId, @StepRunId, @SlotId, @CommandName, @RequestJson, @ResponseJson, @IsSuccess, @CreatedAt);
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, trace);
    }

    public async Task<IReadOnlyList<StationSessionSummary>> QuerySessionsAsync(TraceabilitySessionQuery query, CancellationToken ct)
    {
        const string sql = """
            SELECT
                session.SessionId,
                session.StationId,
                session.OperatorCode,
                session.StartedAt,
                CASE
                    WHEN EXISTS (SELECT 1 FROM dbo.DeviceRuns failRun WHERE failRun.SessionId = session.SessionId AND failRun.Conclusion = 'Fail') THEN 'Fail'
                    WHEN EXISTS (SELECT 1 FROM dbo.DeviceRuns warnRun WHERE warnRun.SessionId = session.SessionId AND warnRun.Conclusion = 'Warning') THEN 'Warning'
                    WHEN EXISTS (SELECT 1 FROM dbo.DeviceRuns passRun WHERE passRun.SessionId = session.SessionId) THEN 'Pass'
                    ELSE NULL
                END AS Conclusion,
                (SELECT COUNT(1) FROM dbo.DeviceRuns countRun WHERE countRun.SessionId = session.SessionId) AS DeviceRunCount
            FROM dbo.StationSessions session
            WHERE (@From IS NULL OR session.StartedAt >= @From)
              AND (@To IS NULL OR session.StartedAt <= @To)
              AND (@Barcode IS NULL OR EXISTS (
                  SELECT 1 FROM dbo.DeviceRuns barcodeRun
                  WHERE barcodeRun.SessionId = session.SessionId AND barcodeRun.Barcode = @Barcode))
              AND (@Conclusion IS NULL OR EXISTS (
                  SELECT 1 FROM dbo.DeviceRuns conclusionRun
                  WHERE conclusionRun.SessionId = session.SessionId AND conclusionRun.Conclusion = @Conclusion))
            ORDER BY session.StartedAt DESC;
            """;

        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<StationSessionSummaryRow>(sql, new
        {
            query.From,
            query.To,
            query.Barcode,
            Conclusion = query.Conclusion?.ToString()
        });

        return rows
            .Select(row => new StationSessionSummary(
                row.SessionId,
                row.StationId,
                row.OperatorCode,
                row.StartedAt,
                row.Conclusion is null ? null : Enum.Parse<Conclusion>(row.Conclusion),
                row.DeviceRunCount))
            .ToList();
    }

    public async Task<IReadOnlyList<DeviceRunSummary>> QueryDeviceRunsAsync(DeviceRunQuery query, CancellationToken ct)
    {
        const string sql = """
            SELECT DeviceRunId, SessionId, Barcode, Conclusion, StartedAt
            FROM dbo.DeviceRuns
            WHERE (@Barcode IS NULL OR Barcode = @Barcode)
              AND (@Conclusion IS NULL OR Conclusion = @Conclusion)
              AND (@From IS NULL OR StartedAt >= @From)
              AND (@To IS NULL OR StartedAt <= @To)
            ORDER BY StartedAt DESC;
            """;

        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<DeviceRunSummaryRow>(sql, new
        {
            query.Barcode,
            Conclusion = query.Conclusion?.ToString(),
            query.From,
            query.To
        });

        return rows
            .Select(row => new DeviceRunSummary(
                row.DeviceRunId,
                row.SessionId,
                row.Barcode,
                Enum.Parse<Conclusion>(row.Conclusion),
                row.StartedAt))
            .ToList();
    }

    public async Task<DeviceRunTrace?> GetDeviceRunTraceAsync(Guid deviceRunId, CancellationToken ct)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var run = await connection.QuerySingleOrDefaultAsync<DeviceRunTraceRow>(
            """
            SELECT DeviceRunId, SessionId, SlotId, Barcode, Conclusion, StartedAt
            FROM dbo.DeviceRuns
            WHERE DeviceRunId = @deviceRunId;
            """,
            new { deviceRunId });

        if (run is null)
        {
            return null;
        }

        var stepRows = (await connection.QueryAsync<StepRunTraceRow>(
            """
            SELECT StepRunId, DeviceRunId, Sequence, StepName, Conclusion, Message
            FROM dbo.StepRuns
            WHERE DeviceRunId = @deviceRunId
            ORDER BY Sequence;
            """,
            new { deviceRunId })).ToList();

        var stepIds = stepRows.Select(step => step.StepRunId).ToArray();
        var measurements = (await connection.QueryAsync<MeasurementTraceRow>(
            """
            SELECT StepRunId, PointKey AS [Key], NumericValue, Unit, Source
            FROM dbo.MeasurementResults
            WHERE StepRunId IN @stepIds;
            """,
            new { stepIds })).ToList();
        var comparisons = (await connection.QueryAsync<ComparisonTraceRow>(
            """
            SELECT StepRunId, LeftKey, RightKey, Conclusion, Message
            FROM dbo.ComparisonResults
            WHERE StepRunId IN @stepIds;
            """,
            new { stepIds })).ToList();
        var commandTraces = (await connection.QueryAsync<CommandTraceSnapshot>(
            """
            SELECT TraceId, StepRunId, SlotId, CommandName, RequestJson, ResponseJson, IsSuccess, CreatedAt
            FROM dbo.CommandTraces
            WHERE StepRunId IN @stepIds
            ORDER BY CreatedAt;
            """,
            new { stepIds })).ToList();

        var steps = stepRows
            .Select(step => new StepRunTrace(
                step.StepRunId,
                step.DeviceRunId,
                step.Sequence,
                step.StepName,
                Enum.Parse<Conclusion>(step.Conclusion),
                measurements
                    .Where(measurement => measurement.StepRunId == step.StepRunId)
                    .Select(measurement => new MeasurementTrace(
                        measurement.StepRunId,
                        measurement.Key,
                        measurement.NumericValue,
                        measurement.Unit,
                        Enum.Parse<MeasurementSource>(measurement.Source)))
                    .ToList(),
                comparisons
                    .Where(comparison => comparison.StepRunId == step.StepRunId)
                    .Select(comparison => new ComparisonTrace(
                        comparison.StepRunId,
                        comparison.LeftKey,
                        comparison.RightKey,
                        Enum.Parse<Conclusion>(comparison.Conclusion),
                        comparison.Message))
                    .ToList(),
                commandTraces.Where(trace => trace.StepRunId == step.StepRunId).ToList(),
                step.Message ?? ""))
            .ToList();

        return new DeviceRunTrace(
            run.DeviceRunId,
            run.SessionId,
            run.SlotId,
            run.Barcode,
            Enum.Parse<Conclusion>(run.Conclusion),
            run.StartedAt,
            steps);
    }

    private sealed record StationSessionSummaryRow(
        Guid SessionId,
        Guid StationId,
        string OperatorCode,
        DateTimeOffset StartedAt,
        string? Conclusion,
        int DeviceRunCount);

    private sealed record DeviceRunSummaryRow(
        Guid DeviceRunId,
        Guid SessionId,
        string Barcode,
        string Conclusion,
        DateTimeOffset StartedAt);

    private sealed record DeviceRunTraceRow(
        Guid DeviceRunId,
        Guid SessionId,
        Guid SlotId,
        string Barcode,
        string Conclusion,
        DateTimeOffset StartedAt);

    private sealed record StepRunTraceRow(
        Guid StepRunId,
        Guid DeviceRunId,
        int Sequence,
        string StepName,
        string Conclusion,
        string? Message);

    private sealed record MeasurementTraceRow(
        Guid StepRunId,
        string Key,
        double NumericValue,
        string Unit,
        string Source);

    private sealed record ComparisonTraceRow(
        Guid StepRunId,
        string LeftKey,
        string RightKey,
        string Conclusion,
        string Message);
}
