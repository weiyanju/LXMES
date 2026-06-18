using System.Globalization;
using Dapper;
using VfdControl.Application.Abstractions;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;

namespace VfdControl.Infrastructure.Sql;

public sealed class SqlProcessPlanRepository : IProcessPlanRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public SqlProcessPlanRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ProcessPlan>> ListAsync(CancellationToken ct)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var planRows = (await connection.QueryAsync<ProcessPlanRow>(
            "SELECT Id, Name FROM dbo.ProcessPlans WHERE IsActive = 1 ORDER BY Name;")).ToList();
        if (planRows.Count == 0)
        {
            return [];
        }

        var versionRows = (await connection.QueryAsync<ProcessPlanVersionRow>(
            """
            SELECT Id, ProcessPlanId, VersionNumber, IsExecutable, CreatedAt
            FROM dbo.ProcessPlanVersions
            WHERE ProcessPlanId IN @planIds
            ORDER BY VersionNumber;
            """,
            new { planIds = planRows.Select(plan => plan.Id).ToArray() })).ToList();
        var stepRows = await LoadStepRowsAsync(connection, versionRows.Select(version => version.Id).ToArray());

        return planRows.Select(plan => BuildPlan(plan, versionRows, stepRows)).ToList();
    }

    public async Task<IReadOnlyList<ProcessPlanVersion>> ListExecutableVersionsAsync(CancellationToken ct)
    {
        var plans = await ListAsync(ct);
        return plans.SelectMany(plan => plan.Versions).Where(version => version.IsExecutable).ToList();
    }

    public async Task<ProcessPlan?> GetAsync(Guid planId, CancellationToken ct)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var plan = await connection.QuerySingleOrDefaultAsync<ProcessPlanRow>(
            "SELECT Id, Name FROM dbo.ProcessPlans WHERE Id = @planId AND IsActive = 1;",
            new { planId });
        if (plan is null)
        {
            return null;
        }

        var versions = (await connection.QueryAsync<ProcessPlanVersionRow>(
            """
            SELECT Id, ProcessPlanId, VersionNumber, IsExecutable, CreatedAt
            FROM dbo.ProcessPlanVersions
            WHERE ProcessPlanId = @planId
            ORDER BY VersionNumber;
            """,
            new { planId })).ToList();
        var steps = await LoadStepRowsAsync(connection, versions.Select(version => version.Id).ToArray());

        return BuildPlan(plan, versions, steps);
    }

    public async Task SaveAsync(ProcessPlan plan, CancellationToken ct)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        await connection.ExecuteAsync(
            """
            MERGE dbo.ProcessPlans AS target
            USING (SELECT @Id AS Id) AS source
            ON target.Id = source.Id
            WHEN MATCHED THEN
                UPDATE SET Name = @Name, IsActive = 1, UpdatedAt = SYSDATETIMEOFFSET()
            WHEN NOT MATCHED THEN
                INSERT (Id, Name, IsActive, PlanJson) VALUES (@Id, @Name, 1, @PlanJson);
            """,
            new { plan.Id, plan.Name, PlanJson = "{}" },
            transaction);

        await connection.ExecuteAsync(
            """
            DELETE stepRows
            FROM dbo.ProcessSteps stepRows
            INNER JOIN dbo.ProcessPlanVersions versions ON versions.Id = stepRows.PlanVersionId
            WHERE versions.ProcessPlanId = @planId;
            DELETE FROM dbo.ProcessPlanVersions WHERE ProcessPlanId = @planId;
            """,
            new { planId = plan.Id },
            transaction);

        foreach (var version in plan.Versions)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO dbo.ProcessPlanVersions
                    (Id, ProcessPlanId, VersionNumber, IsExecutable, StepsJson, CreatedAt, PublishedAt)
                VALUES
                    (@Id, @ProcessPlanId, @VersionNumber, @IsExecutable, @StepsJson, @CreatedAt, @PublishedAt);
                """,
                new
                {
                    version.Id,
                    ProcessPlanId = plan.Id,
                    version.VersionNumber,
                    version.IsExecutable,
                    StepsJson = "{}",
                    version.CreatedAt,
                    PublishedAt = version.IsExecutable ? version.CreatedAt : (DateTimeOffset?)null
                },
                transaction);

            foreach (var step in version.Steps)
            {
                var compare = ParseCompareTarget(step.Command.Target);
                var tolerance = ParseTolerance(step.Command.Value);
                await connection.ExecuteAsync(
                    """
                    INSERT INTO dbo.ProcessSteps
                        (Id, PlanVersionId, Sequence, Name, StepType, TargetPointKey, CommandValue,
                         CompareLeftPointKey, CompareRightPointKey, ToleranceType, ToleranceValue,
                         RuleType, LowerLimit, UpperLimit, ExpectedValue, FailureAction, MaxRetries, AffectsFinalConclusion)
                    VALUES
                        (@Id, @PlanVersionId, @Sequence, @Name, @StepType, @TargetPointKey, @CommandValue,
                         @CompareLeftPointKey, @CompareRightPointKey, @ToleranceType, @ToleranceValue,
                         @RuleType, @LowerLimit, @UpperLimit, @ExpectedValue, @FailureAction, @MaxRetries, @AffectsFinalConclusion);
                    """,
                    new
                    {
                        step.Id,
                        PlanVersionId = version.Id,
                        step.Sequence,
                        step.Name,
                        StepType = step.Command.CommandType,
                        TargetPointKey = step.Command.CommandType == "CompareMeasurement" ? null : step.Command.Target,
                        CommandValue = step.Command.CommandType == "CompareMeasurement" ? null : step.Command.Value,
                        CompareLeftPointKey = step.Command.CommandType == "CompareMeasurement" ? compare.Left : null,
                        CompareRightPointKey = step.Command.CommandType == "CompareMeasurement" ? compare.Right : null,
                        ToleranceType = step.Command.CommandType == "CompareMeasurement" ? tolerance.Type : null,
                        ToleranceValue = step.Command.CommandType == "CompareMeasurement" ? tolerance.Value : null,
                        RuleType = step.Rule?.RuleType,
                        LowerLimit = step.Rule?.LowerLimit,
                        UpperLimit = step.Rule?.UpperLimit,
                        ExpectedValue = step.Rule?.ExpectedValue,
                        FailureAction = step.FailurePolicy.Action.ToString(),
                        step.FailurePolicy.MaxRetries,
                        step.AffectsFinalConclusion
                    },
                    transaction);
            }
        }

        await transaction.CommitAsync(ct);
    }

    private static async Task<IReadOnlyList<ProcessStepRow>> LoadStepRowsAsync(
        System.Data.IDbConnection connection,
        Guid[] versionIds)
    {
        if (versionIds.Length == 0)
        {
            return [];
        }

        return (await connection.QueryAsync<ProcessStepRow>(
            """
            SELECT Id, PlanVersionId, Sequence, Name, StepType, TargetPointKey, CommandValue,
                   CompareLeftPointKey, CompareRightPointKey, ToleranceType, ToleranceValue,
                   RuleType, LowerLimit, UpperLimit, ExpectedValue, FailureAction, MaxRetries, AffectsFinalConclusion
            FROM dbo.ProcessSteps
            WHERE PlanVersionId IN @versionIds
            ORDER BY Sequence;
            """,
            new { versionIds })).ToList();
    }

    private static ProcessPlan BuildPlan(
        ProcessPlanRow planRow,
        IReadOnlyList<ProcessPlanVersionRow> versionRows,
        IReadOnlyList<ProcessStepRow> stepRows)
    {
        var plan = new ProcessPlan(planRow.Id, planRow.Name);
        foreach (var versionRow in versionRows.Where(version => version.ProcessPlanId == plan.Id).OrderBy(version => version.VersionNumber))
        {
            var version = new ProcessPlanVersion(
                versionRow.Id,
                versionRow.VersionNumber,
                versionRow.IsExecutable,
                versionRow.CreatedAt);
            foreach (var stepRow in stepRows.Where(step => step.PlanVersionId == version.Id).OrderBy(step => step.Sequence))
            {
                version.AddStep(BuildStep(stepRow));
            }

            plan.AddVersion(version);
        }

        return plan;
    }

    private static ProcessStep BuildStep(ProcessStepRow row)
    {
        var command = new StepCommand(
            row.StepType,
            row.StepType == "CompareMeasurement"
                ? $"{row.CompareLeftPointKey}|{row.CompareRightPointKey}"
                : row.TargetPointKey ?? "",
            row.StepType == "CompareMeasurement"
                ? FormatTolerance(row.ToleranceType, row.ToleranceValue)
                : row.CommandValue);
        return new ProcessStep(
            row.Id,
            row.Sequence,
            row.Name,
            command,
            new StepFailurePolicy(Enum.Parse<FailureAction>(row.FailureAction), row.MaxRetries),
            row.AffectsFinalConclusion,
            BuildRule(row));
    }

    private static StepRule? BuildRule(ProcessStepRow row)
    {
        return row.RuleType switch
        {
            StepRule.NumericRangeRuleType => StepRule.NumericRange((double?)row.LowerLimit, (double?)row.UpperLimit),
            StepRule.StringEqualsRuleType when row.ExpectedValue is not null => StepRule.StringEquals(row.ExpectedValue),
            _ => null
        };
    }

    private static (string? Left, string? Right) ParseCompareTarget(string target)
    {
        var parts = target.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return (parts.ElementAtOrDefault(0), parts.ElementAtOrDefault(1));
    }

    private static (string? Type, decimal? Value) ParseTolerance(string? value)
    {
        var parts = (value ?? "").Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || !decimal.TryParse(parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var tolerance))
        {
            return (null, null);
        }

        return (parts[0], tolerance);
    }

    private static string? FormatTolerance(string? toleranceType, decimal? toleranceValue)
    {
        return toleranceType is null || toleranceValue is null
            ? null
            : $"{toleranceType}:{toleranceValue.Value.ToString("0.######", CultureInfo.InvariantCulture)}";
    }

    private sealed record ProcessPlanRow(Guid Id, string Name);

    private sealed record ProcessPlanVersionRow(
        Guid Id,
        Guid ProcessPlanId,
        int VersionNumber,
        bool IsExecutable,
        DateTimeOffset CreatedAt);

    private sealed record ProcessStepRow(
        Guid Id,
        Guid PlanVersionId,
        int Sequence,
        string Name,
        string StepType,
        string? TargetPointKey,
        string? CommandValue,
        string? CompareLeftPointKey,
        string? CompareRightPointKey,
        string? ToleranceType,
        decimal? ToleranceValue,
        string? RuleType,
        decimal? LowerLimit,
        decimal? UpperLimit,
        string? ExpectedValue,
        string FailureAction,
        int MaxRetries,
        bool AffectsFinalConclusion);
}
