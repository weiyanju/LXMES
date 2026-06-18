using Dapper;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using VfdControl.Application.Engineering;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;
using VfdControl.Domain.Stations;
using VfdControl.Domain.ValueObjects;
using VfdControl.Infrastructure.Sql;

namespace VfdControl.Infrastructure.Tests.Sql;

public class SqlRepositoryIntegrationTests
{
    [Fact]
    public async Task Sql_repositories_round_trip_station_and_process_plan()
    {
        var connectionString = Environment.GetEnvironmentVariable("VFD_SQL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var stationId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var factory = new SqlConnectionFactory(connectionString);
        var initializer = new DatabaseInitializer(factory);
        var stationRepository = new SqlStationRepository(factory);
        var planRepository = new SqlProcessPlanRepository(factory);

        await initializer.InitializeAsync(CancellationToken.None);

        try
        {
            var station = new Station(stationId, "Codex SQL Test Station");
            station.AddSlot(new StationSlot(
                slotId,
                new SlotNumber(1),
                new SlotCommunicationConfig(
                    new SerialPortName("COM99"),
                    new ModbusAddress(1),
                    new ModbusAddress(11),
                    new ModbusAddress(21),
                    9600),
                "1 号测试槽位"));

            await stationRepository.SaveAsync(station, CancellationToken.None);

            var loadedStation = await stationRepository.GetAsync(stationId, CancellationToken.None);
            loadedStation.Should().NotBeNull();
            loadedStation!.Name.Should().Be(station.Name);
            loadedStation.Slots.Should().ContainSingle();
            loadedStation.Slots.Single().CommunicationConfig.PortName!.Value.Should().Be("COM99");
            loadedStation.Slots.Single().CommunicationConfig.VfdAddress.Value.Should().Be(1);
            loadedStation.Slots.Single().CommunicationConfig.VoltageMeterAddress.Value.Should().Be(11);
            loadedStation.Slots.Single().CommunicationConfig.CurrentMeterAddress.Value.Should().Be(21);

            var plan = new ProcessPlan(planId, "Codex SQL Test Plan");
            var version = new ProcessPlanVersion(versionId, 1, isExecutable: true);
            version.AddStep(new WorkflowDefinitionService().CreateStartStep(1));
            version.AddStep(new WorkflowDefinitionService().CreateDelayStep(2, 10000));
            version.AddStep(new ProcessStep(
                Guid.NewGuid(),
                3,
                "读取 VFD 电压",
                new StepCommand("ReadMeasurement", "Vfd:Voltage"),
                new StepFailurePolicy(FailureAction.ContinueAndMarkFail),
                affectsFinalConclusion: true,
                StepRule.NumericRange(210, 230)));
            version.AddStep(new WorkflowDefinitionService().CreateCompareMeasurementStep(4, FailureAction.ContinueAndMarkFail));
            plan.AddVersion(version);

            await planRepository.SaveAsync(plan, CancellationToken.None);

            var loadedPlan = await planRepository.GetAsync(planId, CancellationToken.None);
            loadedPlan.Should().NotBeNull();
            loadedPlan!.Versions.Should().ContainSingle();
            var loadedVersion = loadedPlan.Versions.Single();
            loadedVersion.IsExecutable.Should().BeTrue();
            loadedVersion.Steps.Select(step => step.Command.CommandType)
                .Should()
                .Equal("Start", "Delay", "ReadMeasurement", "CompareMeasurement");
            loadedVersion.Steps[2].Rule.Should().Be(StepRule.NumericRange(210, 230));
            loadedVersion.Steps[3].Command.Target.Should().Be("Vfd:Voltage|Instrument:Voltage");
            loadedVersion.Steps[3].Command.Value.Should().Be("Absolute:2");
        }
        finally
        {
            await CleanupAsync(connectionString, planId, stationId);
        }
    }

    private static async Task CleanupAsync(string connectionString, Guid planId, Guid stationId)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.ExecuteAsync(
            """
            DELETE stepRows
            FROM dbo.ProcessSteps stepRows
            INNER JOIN dbo.ProcessPlanVersions versions ON versions.Id = stepRows.PlanVersionId
            WHERE versions.ProcessPlanId = @planId;
            DELETE FROM dbo.ProcessPlanVersions WHERE ProcessPlanId = @planId;
            DELETE FROM dbo.ProcessPlans WHERE Id = @planId;
            DELETE FROM dbo.StationSlots WHERE StationId = @stationId;
            DELETE FROM dbo.Stations WHERE Id = @stationId;
            """,
            new { planId, stationId });
    }
}
