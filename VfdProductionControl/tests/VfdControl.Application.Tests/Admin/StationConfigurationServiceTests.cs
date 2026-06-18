using FluentAssertions;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Admin;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Stations;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Application.Tests.Admin;

public class StationConfigurationServiceTests
{
    [Fact]
    public async Task Station_can_contain_multiple_slots()
    {
        var repository = new FakeStationRepository();
        var service = new StationConfigurationService(repository);

        var station = (await service.CreateStationAsync("一号测试工位", CancellationToken.None)).Value!;
        await service.AddSlotAsync(station.Id, slotNumber: 1, "COM1", deviceAddress: 1, baudRate: 9600, CancellationToken.None);
        await service.AddSlotAsync(station.Id, slotNumber: 2, "COM2", deviceAddress: 2, baudRate: 9600, CancellationToken.None);

        station.Slots.Should().HaveCount(2);
        station.Slots.Select(slot => slot.Number.Value).Should().Equal(1, 2);
    }

    [Fact]
    public async Task Slot_serial_port_can_change_without_changing_slot_identity()
    {
        var station = CreateStationWithSlot();
        var slotId = station.Slots[0].Id;
        var service = new StationConfigurationService(new FakeStationRepository([station]));

        var result = await service.UpdateSlotSerialPortAsync(station.Id, slotNumber: 1, "COM5", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        station.Slots[0].Id.Should().Be(slotId);
        station.Slots[0].CommunicationConfig.PortName.Value.Should().Be("COM5");
    }

    [Fact]
    public async Task Slot_configuration_can_store_separate_device_addresses()
    {
        var station = CreateStationWithSlot();
        var service = new StationConfigurationService(new FakeStationRepository([station]));

        var result = await service.UpdateSlotConfigurationAsync(
            station.Id,
            slotNumber: 1,
            portName: "COM1",
            vfdAddress: 1,
            voltageMeterAddress: 11,
            currentMeterAddress: 21,
            baudRate: 9600,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        station.Slots[0].CommunicationConfig.VfdAddress.Value.Should().Be(1);
        station.Slots[0].CommunicationConfig.VoltageMeterAddress.Value.Should().Be(11);
        station.Slots[0].CommunicationConfig.CurrentMeterAddress.Value.Should().Be(21);
    }

    [Fact]
    public async Task Same_serial_port_cannot_reuse_modbus_addresses()
    {
        var station = new Station(Guid.NewGuid(), "一号测试工位");
        station.AddSlot(CreateSlot(1, "COM1", 1, 11, 21));
        station.AddSlot(CreateSlot(2, "COM1", 2, 12, 22));
        var service = new StationConfigurationService(new FakeStationRepository([station]));

        var result = await service.UpdateSlotConfigurationAsync(
            station.Id,
            slotNumber: 2,
            portName: "COM1",
            vfdAddress: 11,
            voltageMeterAddress: 12,
            currentMeterAddress: 22,
            baudRate: 9600,
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("StationSlot.AddressConflict");
    }

    [Fact]
    public async Task Slot_can_be_added_and_deleted_from_station_configuration()
    {
        var station = CreateStationWithSlot();
        var service = new StationConfigurationService(new FakeStationRepository([station]));

        var added = await service.AddNextSlotAsync(station.Id, "COM2", 2, 12, 22, 9600, CancellationToken.None);
        var deleted = await service.DeleteSlotAsync(station.Id, slotNumber: 1, CancellationToken.None);

        added.IsSuccess.Should().BeTrue();
        deleted.IsSuccess.Should().BeTrue();
        station.Slots.Select(slot => slot.Number.Value).Should().Equal(2);
    }

    [Fact]
    public async Task Slot_display_name_can_change_without_changing_slot_number()
    {
        var station = CreateStationWithSlot();
        var service = new StationConfigurationService(new FakeStationRepository([station]));

        var result = await service.UpdateSlotDisplayNameAsync(
            station.Id,
            slotNumber: 1,
            displayName: "\u8001\u5316\u4F4D A",
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        station.Slots[0].Number.Value.Should().Be(1);
        station.Slots[0].DisplayName.Should().Be("\u8001\u5316\u4F4D A");
    }

    [Fact]
    public async Task Slot_can_contain_multiple_instruments()
    {
        var station = CreateStationWithSlot();
        var service = new StationConfigurationService(new FakeStationRepository([station]));

        await service.AddInstrumentAsync(station.Id, slotNumber: 1, "电压表", CancellationToken.None);
        await service.AddInstrumentAsync(station.Id, slotNumber: 1, "电流表", CancellationToken.None);

        station.Slots[0].Instruments.Should().HaveCount(2);
    }

    [Fact]
    public async Task Instrument_can_contain_points()
    {
        var station = CreateStationWithSlot();
        var service = new StationConfigurationService(new FakeStationRepository([station]));
        var instrument = (await service.AddInstrumentAsync(station.Id, 1, "电压表", CancellationToken.None)).Value!;

        var result = await service.AddInstrumentPointAsync(
            station.Id,
            slotNumber: 1,
            instrument.Id,
            key: "Voltage",
            name: "电压",
            DataType.Number,
            unit: "V",
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        instrument.Points.Should().ContainSingle(point => point.Key == "Voltage" && point.Unit == "V");
    }

    [Theory]
    [InlineData("EMP0001", true)]
    [InlineData("emp12345678", true)]
    [InlineData("bad", false)]
    public void Barcode_rules_validate_default_employee_format(string value, bool expected)
    {
        var service = new BarcodeRuleService();

        service.ValidateEmployeeCode(value).IsSuccess.Should().Be(expected);
    }

    [Theory]
    [InlineData("VFD202606010001", true)]
    [InlineData("vfdABC12345", true)]
    [InlineData("ABC202606010001", false)]
    public void Barcode_rules_validate_default_vfd_format(string value, bool expected)
    {
        var service = new BarcodeRuleService();

        service.ValidateVfdBarcode(value).IsSuccess.Should().Be(expected);
    }

    private static Station CreateStationWithSlot()
    {
        var station = new Station(Guid.NewGuid(), "一号测试工位");
        station.AddSlot(new StationSlot(
            Guid.NewGuid(),
            new SlotNumber(1),
            new SlotCommunicationConfig(new SerialPortName("COM1"), new ModbusAddress(1), 9600)));

        return station;
    }

    private static StationSlot CreateSlot(
        int number,
        string portName,
        byte vfdAddress,
        byte voltageMeterAddress,
        byte currentMeterAddress)
    {
        return new StationSlot(
            Guid.NewGuid(),
            new SlotNumber(number),
            new SlotCommunicationConfig(
                new SerialPortName(portName),
                new ModbusAddress(vfdAddress),
                new ModbusAddress(voltageMeterAddress),
                new ModbusAddress(currentMeterAddress),
                9600));
    }

    private sealed class FakeStationRepository : IStationRepository
    {
        public FakeStationRepository(IEnumerable<Station>? stations = null)
        {
            Stations = (stations ?? []).ToList();
        }

        public List<Station> Stations { get; }

        public Task<IReadOnlyList<Station>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Station>>(Stations);

        public Task<Station?> GetAsync(Guid stationId, CancellationToken ct)
        {
            return Task.FromResult(Stations.SingleOrDefault(station => station.Id == stationId));
        }

        public Task SaveAsync(Station station, CancellationToken ct)
        {
            if (Stations.All(existing => existing.Id != station.Id))
            {
                Stations.Add(station);
            }

            return Task.CompletedTask;
        }
    }
}
