using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;
using VfdControl.Domain.Stations;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Infrastructure.Seed;

public static class DemoDataSeeder
{
    public static DemoData Create()
    {
        var station = CreateStation();
        var plan = CreateDemoPlan();
        var simplePlan = CreateSimpleStartStopPlan();

        return new DemoData([station], [plan, simplePlan]);
    }

    private static Station CreateStation()
    {
        var station = new Station(
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            "演示工位");

        for (var slotNumber = 1; slotNumber <= 4; slotNumber++)
        {
            var slot = new StationSlot(
                Guid.Parse($"20000000-0000-0000-0000-00000000000{slotNumber}"),
                new SlotNumber(slotNumber),
                new SlotCommunicationConfig(
                    new SerialPortName($"COM{slotNumber}"),
                    new ModbusAddress((byte)slotNumber),
                    9600));

            var instrument = new SlotInstrument(
                Guid.Parse($"30000000-0000-0000-0000-00000000000{slotNumber}"),
                $"{slotNumber} 号槽位电压表");
            instrument.AddPoint(new InstrumentPoint(
                Guid.Parse($"40000000-0000-0000-0000-00000000000{slotNumber}"),
                "Voltage",
                "Voltage",
                DataType.Number,
                "V"));

            slot.AddInstrument(instrument);
            station.AddSlot(slot);
        }

        return station;
    }

    private static ProcessPlan CreateDemoPlan()
    {
        var plan = new ProcessPlan(
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            "VFD 生产测试演示方案");

        var version = new ProcessPlanVersion(
            Guid.Parse("60000000-0000-0000-0000-000000000001"),
            versionNumber: 1,
            isExecutable: true);

        version.AddStep(Step(1, "Start VFD", "Start", "Vfd:Control", "1"));
        version.AddStep(Step(2, "Confirm running", "ReadMeasurement", "Vfd:State", failureAction: FailureAction.StopSlotImmediately, rule: StepRule.NumericRange(1, 2)));
        version.AddStep(Step(3, "Delay 5 seconds", "Delay", "Timer", "5000"));
        version.AddStep(Step(4, "Read VFD voltage", "ReadMeasurement", "Vfd:Voltage"));
        version.AddStep(Step(5, "Read instrument voltage", "ReadMeasurement", "Instrument:Voltage"));
        version.AddStep(Step(6, "Compare absolute tolerance", "CompareMeasurement", "Vfd:Voltage|Instrument:Voltage", "Absolute:2"));
        version.AddStep(Step(7, "Compare percent tolerance", "CompareMeasurement", "Vfd:Voltage|Instrument:Voltage", "Percent:1"));
        version.AddStep(Step(8, "Stop VFD", "Stop", "Vfd:Control", "6"));
        version.AddStep(Step(9, "Confirm stopped", "ReadMeasurement", "Vfd:State", failureAction: FailureAction.ContinueAndMarkFail, affectsFinalConclusion: false, rule: StepRule.NumericRange(3, 3)));

        plan.AddVersion(version);
        return plan;
    }

    private static ProcessPlan CreateSimpleStartStopPlan()
    {
        var plan = new ProcessPlan(
            Guid.Parse("50000000-0000-0000-0000-000000000002"),
            "VFD \u542F\u505C 10 \u79D2\u6D4B\u8BD5\u65B9\u6848");

        var version = new ProcessPlanVersion(
            Guid.Parse("60000000-0000-0000-0000-000000000002"),
            versionNumber: 1,
            isExecutable: true);

        version.AddStep(new ProcessStep(
            Guid.Parse("71000000-0000-0000-0000-000000000001"),
            1,
            "\u542F\u52A8 VFD",
            new StepCommand("Start", "Vfd:Control", "1"),
            new StepFailurePolicy(FailureAction.StopSlotImmediately),
            affectsFinalConclusion: true));
        version.AddStep(new ProcessStep(
            Guid.Parse("71000000-0000-0000-0000-000000000002"),
            2,
            "\u5EF6\u65F6 10 \u79D2",
            new StepCommand("Delay", "Timer", "10000"),
            new StepFailurePolicy(FailureAction.ContinueAndMarkFail),
            affectsFinalConclusion: false));
        version.AddStep(new ProcessStep(
            Guid.Parse("71000000-0000-0000-0000-000000000003"),
            3,
            "\u505C\u6B62 VFD",
            new StepCommand("Stop", "Vfd:Control", "6"),
            new StepFailurePolicy(FailureAction.ContinueAndMarkFail),
            affectsFinalConclusion: true));

        plan.AddVersion(version);
        return plan;
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
            Guid.Parse($"70000000-0000-0000-0000-0000000000{sequence:00}"),
            sequence,
            name,
            new StepCommand(commandType, target, value),
            new StepFailurePolicy(failureAction),
            affectsFinalConclusion,
            rule);
    }
}

public sealed record DemoData(
    IReadOnlyList<Station> Stations,
    IReadOnlyList<ProcessPlan> ProcessPlans);
