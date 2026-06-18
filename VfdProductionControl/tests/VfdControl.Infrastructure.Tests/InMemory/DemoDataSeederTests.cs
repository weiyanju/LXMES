using FluentAssertions;
using VfdControl.Domain.Enums;
using VfdControl.Infrastructure.Seed;

namespace VfdControl.Infrastructure.Tests.InMemory;

public class DemoDataSeederTests
{
    [Fact]
    public void Demo_data_contains_one_station()
    {
        var demoData = DemoDataSeeder.Create();

        demoData.Stations.Should().ContainSingle();
    }

    [Fact]
    public void Demo_station_contains_at_least_four_slots()
    {
        var demoData = DemoDataSeeder.Create();

        demoData.Stations.Single().Slots.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    [Fact]
    public void Each_slot_has_communication_config()
    {
        var demoData = DemoDataSeeder.Create();

        demoData.Stations.Single().Slots.Should().OnlyContain(slot => slot.CommunicationConfig != null);
    }

    [Fact]
    public void Each_slot_has_vfd_plus_voltage_instrument_point()
    {
        var demoData = DemoDataSeeder.Create();

        demoData.Stations.Single().Slots.Should().OnlyContain(slot =>
            slot.Instruments.Any(instrument =>
                instrument.Points.Any(point => point.Key == "Voltage" && point.DataType == DataType.Number)));
    }

    [Fact]
    public void Demo_data_contains_one_executable_plan_version_with_required_steps()
    {
        var demoData = DemoDataSeeder.Create();

        var executableVersion = demoData.ProcessPlans
            .SelectMany(plan => plan.Versions)
            .First(version => version.IsExecutable);

        executableVersion.Steps.Select(step => step.Command.CommandType)
            .Should()
            .ContainInOrder(
                "Start",
                "ReadMeasurement",
                "Delay",
                "ReadMeasurement",
                "ReadMeasurement",
                "CompareMeasurement",
                "CompareMeasurement",
                "Stop",
                "ReadMeasurement");
    }

    [Fact]
    public void Demo_data_contains_simple_start_delay_stop_vfd_plan()
    {
        var demoData = DemoDataSeeder.Create();

        var plan = demoData.ProcessPlans.Single(plan => plan.Name == "VFD 启停 10 秒测试方案");
        var version = plan.Versions.Single(version => version.IsExecutable);

        version.Steps.Select(step => (step.Name, step.Command.CommandType, step.Command.Target, step.Command.Value))
            .Should()
            .Equal(
                ("启动 VFD", "Start", "Vfd:Control", "1"),
                ("延时 10 秒", "Delay", "Timer", "10000"),
                ("停止 VFD", "Stop", "Vfd:Control", "6"));
    }
}
