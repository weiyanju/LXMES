using FluentAssertions;

namespace VfdControl.Infrastructure.Tests.Sql;

public class SchemaScriptTests
{
    [Theory]
    [InlineData("Stations")]
    [InlineData("StationSlots")]
    [InlineData("DeviceModels")]
    [InlineData("LogicalPoints")]
    [InlineData("LogicalPointWriteOptions")]
    [InlineData("ProcessPlans")]
    [InlineData("ProcessPlanVersions")]
    [InlineData("ProcessSteps")]
    [InlineData("StationSessions")]
    [InlineData("DeviceRuns")]
    [InlineData("StepRuns")]
    [InlineData("MeasurementResults")]
    [InlineData("ComparisonResults")]
    [InlineData("CommandTraces")]
    public void Schema_contains_required_table(string tableName)
    {
        var schema = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "Sql",
            "schema.sql"));

        schema.Should().Contain($"CREATE TABLE dbo.{tableName}");
    }

    [Fact]
    public void Station_slots_schema_stores_each_device_address()
    {
        var schema = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "Sql",
            "schema.sql"));

        schema.Should().Contain("VfdAddress TINYINT");
        schema.Should().Contain("VoltageMeterAddress TINYINT");
        schema.Should().Contain("CurrentMeterAddress TINYINT");
    }

    [Fact]
    public void Station_slots_schema_stores_editable_display_name()
    {
        var schema = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "Sql",
            "schema.sql"));

        schema.Should().Contain("DisplayName NVARCHAR(120) NOT NULL");
    }

    [Fact]
    public void Process_steps_schema_models_compare_measurement_as_step_type()
    {
        var schema = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "Sql",
            "schema.sql"));

        schema.Should().Contain("StepType NVARCHAR(50) NOT NULL");
        schema.Should().Contain("CompareLeftPointKey NVARCHAR(100) NULL");
        schema.Should().Contain("CompareRightPointKey NVARCHAR(100) NULL");
        schema.Should().Contain("ToleranceType NVARCHAR(50) NULL");
        schema.Should().Contain("ToleranceValue DECIMAL(18,6) NULL");
        schema.Should().NotContain("StepsJson NVARCHAR(MAX) NOT NULL");
    }

    [Fact]
    public void Schema_is_idempotent_for_database_initialization()
    {
        var schema = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "Sql",
            "schema.sql"));

        schema.Should().Contain("IF OBJECT_ID('dbo.ProcessSteps', 'U') IS NULL");
        schema.Should().Contain("IF OBJECT_ID('dbo.Stations', 'U') IS NULL");
    }
}
