using FluentAssertions;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Admin;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Stations;
using VfdControl.Domain.ValueObjects;
using VfdControl.Presentation.Admin;

namespace VfdControl.Presentation.Tests.Admin;

public class AdminConfigurationViewModelTests
{
    [Fact]
    public async Task Station_config_loads_station_slots_and_instruments()
    {
        var station = CreateStation();
        var viewModel = CreateStationConfigViewModel([station], ["COM1"]);

        await viewModel.LoadAsync();

        viewModel.StationName.Should().Be("\u4E00\u53F7\u6D4B\u8BD5\u5DE5\u4F4D");
        viewModel.Slots.Should().ContainSingle();
        viewModel.Slots[0].PortName.Should().Be("COM1");
        viewModel.Slots[0].Instruments.Should().ContainSingle();
        viewModel.Slots[0].Instruments[0].Points.Should().ContainSingle(point => point.Key == "Voltage");
    }

    [Fact]
    public async Task Station_config_assigns_scanned_ports_by_slot_order()
    {
        var station = CreateStation(slotCount: 3);
        var viewModel = CreateStationConfigViewModel([station], ["COM7", "COM8"]);

        await viewModel.LoadAsync();

        viewModel.PortOptions.Should().Equal("\u65E0", "COM7", "COM8");
        viewModel.Slots.Select(slot => slot.PortName).Should().Equal("COM7", "COM8", "\u65E0");
        viewModel.Slots[0].StatusText.Should().Be("\u5728\u7EBF");
        viewModel.Slots[2].StatusText.Should().Be("\u672A\u68C0\u6D4B");
        viewModel.StatusMessage.Should().Be("\u5DF2\u68C0\u6D4B\u5230 2 \u4E2A\u4E32\u53E3 / \u5DF2\u914D\u7F6E 3 \u4E2A\u69FD\u4F4D / \u672A\u4FDD\u5B58 0 \u9879");
    }

    [Fact]
    public async Task Saving_slot_row_updates_serial_port_and_device_addresses()
    {
        var station = CreateStation();
        var viewModel = CreateStationConfigViewModel([station], ["COM1", "COM5"]);
        await viewModel.LoadAsync();
        viewModel.SelectedSlot = viewModel.Slots[0];
        viewModel.SelectedSlot.DisplayName = "\u8001\u5316\u4F4D A";
        viewModel.SelectedSlot.PortName = "COM5";
        viewModel.SelectedSlot.VfdAddress = 2;
        viewModel.SelectedSlot.VoltageMeterAddress = 12;
        viewModel.SelectedSlot.CurrentMeterAddress = 22;

        await viewModel.SaveSlotCommand.ExecuteAsync(viewModel.SelectedSlot);

        viewModel.Slots[0].PortName.Should().Be("COM5");
        viewModel.Slots[0].DisplayName.Should().Be("\u8001\u5316\u4F4D A");
        viewModel.Slots[0].VfdAddress.Should().Be(2);
        viewModel.Slots[0].VoltageMeterAddress.Should().Be(12);
        viewModel.Slots[0].CurrentMeterAddress.Should().Be(22);
        viewModel.StatusMessage.Should().Be("\u69FD\u4F4D\u914D\u7F6E\u5DF2\u4FDD\u5B58\u3002");
    }

    [Fact]
    public async Task Saving_all_slots_persists_every_modified_slot_and_reports_configuration_saved()
    {
        var station = CreateStation(slotCount: 2);
        var viewModel = CreateStationConfigViewModel([station], ["COM1", "COM2", "COM7", "COM8"]);
        await viewModel.LoadAsync();

        viewModel.Slots[0].PortName = "COM7";
        viewModel.Slots[1].PortName = "COM8";

        viewModel.StatusMessage.Should().Be("\u5DF2\u68C0\u6D4B\u5230 4 \u4E2A\u4E32\u53E3 / \u5DF2\u914D\u7F6E 2 \u4E2A\u69FD\u4F4D / \u672A\u4FDD\u5B58 1 \u9879");

        await viewModel.SaveAllSlotsCommand.ExecuteAsync(null);

        viewModel.Slots.Select(slot => slot.PortName).Should().Equal("COM7", "COM8");
        viewModel.StatusMessage.Should().Be("\u914D\u7F6E\u5DF2\u4FDD\u5B58\u3002");
    }

    [Fact]
    public async Task Slot_rows_can_be_added_and_deleted_from_configuration()
    {
        var station = CreateStation();
        var viewModel = CreateStationConfigViewModel([station], ["COM1", "COM2"]);
        await viewModel.LoadAsync();

        await viewModel.AddSlotCommand.ExecuteAsync(null);
        await viewModel.DeleteSlotCommand.ExecuteAsync(viewModel.Slots[0]);

        viewModel.Slots.Should().ContainSingle(slot => slot.SlotNumber == 2 && slot.PortName == "COM2");
    }

    [Fact]
    public async Task Station_config_exposes_device_model_and_logical_point_sections()
    {
        var station = CreateStation();
        var viewModel = CreateStationConfigViewModel([station]);

        await viewModel.LoadAsync();

        viewModel.AdminSections.Should().Equal(
            "\u5DE5\u4F4D / \u69FD\u4F4D\u914D\u7F6E",
            "\u8BBE\u5907\u578B\u53F7\u914D\u7F6E");
        viewModel.DeviceModels.Should().Contain(model =>
            model.Points.Any(point => point.LogicalKey == "Instrument:Voltage" && point.RegisterAddress == "40001"));
        viewModel.DeviceModels.Should().Contain(model =>
            model.Points.Any(point => point.LogicalKey == "Vfd:Control" && point.RegisterAddress == "0x2000"));
        viewModel.LogicalPoints.Should().Contain(point => point.LogicalKey == "Vfd:Control" && point.Source == "VFD");
        viewModel.LogicalPoints.Should().Contain(point => point.LogicalKey == "Instrument:Voltage");
        viewModel.LogicalPoints.Should().Contain(point => point.LogicalKey == "Instrument:Current" && point.Unit == "A");
    }

    [Fact]
    public void Standard_vfd_model_uses_3300_manual_registers_for_core_points()
    {
        var catalog = new DeviceModelCatalog();
        var vfdModel = catalog.DeviceModels.Single(model => model.Name == "\u6807\u51C6 VFD");

        vfdModel.Points.Should().Contain(point =>
            point.LogicalKey == "Vfd:Control"
            && point.AccessMode == "\u5199\u5165"
            && point.FunctionCode == "06"
            && point.RegisterAddress == "0x2000"
            && point.WriteOptions.Any(option => option.Value == "1" && option.DisplayName == "\u6B63\u8F6C\u8FD0\u884C")
            && point.WriteOptions.Any(option => option.Value == "6" && option.DisplayName == "\u51CF\u901F\u505C\u673A"));
        vfdModel.Points.Should().Contain(point =>
            point.LogicalKey == "Vfd:State"
            && point.FunctionCode == "03"
            && point.RegisterAddress == "0x3000"
            && point.DataType == "Enum");
        vfdModel.Points.Should().Contain(point =>
            point.LogicalKey == "Vfd:Voltage"
            && point.FunctionCode == "03"
            && point.RegisterAddress == "0x1003"
            && point.DataType == "Decimal"
            && point.Unit == "V");
        vfdModel.Points.Should().Contain(point =>
            point.LogicalKey == "Vfd:Current"
            && point.RegisterAddress == "0x1004"
            && point.Unit == "A");
        vfdModel.Points.Should().Contain(point =>
            point.LogicalKey == "Vfd:FaultCode"
            && point.RegisterAddress == "0x8000"
            && point.DataType == "Enum");
    }

    [Fact]
    public async Task Device_model_and_logical_point_rows_are_editable()
    {
        var station = CreateStation();
        var viewModel = CreateStationConfigViewModel([station]);
        await viewModel.LoadAsync();

        var instrumentModel = viewModel.DeviceModels.Single(model => model.Points.Any(point => point.LogicalKey == "Instrument:Voltage"));
        var voltagePoint = instrumentModel.Points.Single(point => point.LogicalKey == "Instrument:Voltage");

        instrumentModel.Name = "\u4EA4\u6D41\u7535\u538B\u8868";
        voltagePoint.RegisterAddress = "40002";
        voltagePoint.Notes = "\u73B0\u573A\u4FEE\u6B63\u540E\u7684\u7535\u538B\u5BC4\u5B58\u5668";

        viewModel.DeviceModels.Should().Contain(model => model.Name == "\u4EA4\u6D41\u7535\u538B\u8868");
        viewModel.LogicalPoints.Should().Contain(point => point.LogicalKey == "Instrument:Voltage"
            && point.RegisterAddress == "40002"
            && point.Notes == "\u73B0\u573A\u4FEE\u6B63\u540E\u7684\u7535\u538B\u5BC4\u5B58\u5668");
    }

    [Fact]
    public async Task Four_digit_register_addresses_are_displayed_as_hex_addresses()
    {
        var station = CreateStation();
        var viewModel = CreateStationConfigViewModel([station]);
        await viewModel.LoadAsync();

        var vfdModel = viewModel.DeviceModels.Single(model => model.Points.Any(point => point.LogicalKey == "Vfd:Control"));
        var controlPoint = vfdModel.Points.Single(point => point.LogicalKey == "Vfd:Control");

        controlPoint.RegisterAddress = "2000";

        controlPoint.RegisterAddress.Should().Be("0x2000");
    }

    [Fact]
    public async Task Selected_device_model_exposes_only_its_logical_points()
    {
        var station = CreateStation();
        var viewModel = CreateStationConfigViewModel([station]);

        await viewModel.LoadAsync();

        var voltageModel = viewModel.DeviceModels.Single(model => model.Points.Any(point => point.LogicalKey == "Instrument:Voltage"));
        viewModel.SelectedDeviceModel = voltageModel;

        viewModel.SelectedDeviceModelPoints.Should().BeEquivalentTo(
            voltageModel.Points,
            options => options.WithStrictOrdering());
        viewModel.SelectedDeviceModelPointCountText.Should().Be("1 \u4E2A\u903B\u8F91\u70B9\u4F4D");
    }

    [Fact]
    public async Task Adding_logical_point_creates_custom_point_under_selected_device_model()
    {
        var station = CreateStation();
        var viewModel = CreateStationConfigViewModel([station]);

        await viewModel.LoadAsync();
        var selectedModel = viewModel.SelectedDeviceModel!;
        var originalCount = selectedModel.Points.Count;

        await viewModel.AddLogicalPointCommand.ExecuteAsync(null);

        selectedModel.Points.Should().HaveCount(originalCount + 1);
        viewModel.SelectedLogicalPoint.Should().BeSameAs(selectedModel.Points.Last());
        viewModel.SelectedLogicalPoint!.IsCustom.Should().BeTrue();
        viewModel.SelectedLogicalPoint.LogicalKey.Should().StartWith("Vfd:Custom");
        viewModel.SelectedLogicalPoint.DisplayName.Should().Be("\u65B0\u589E\u903B\u8F91\u70B9\u4F4D");
        viewModel.SelectedDeviceModelPointCountText.Should().Be($"{originalCount + 1} \u4E2A\u903B\u8F91\u70B9\u4F4D");
    }

    [Fact]
    public async Task Deleting_logical_point_only_removes_custom_points()
    {
        var station = CreateStation();
        var viewModel = CreateStationConfigViewModel([station]);

        await viewModel.LoadAsync();
        var builtInPoint = viewModel.SelectedDeviceModel!.Points[0];
        viewModel.SelectedLogicalPoint = builtInPoint;

        await viewModel.DeleteLogicalPointCommand.ExecuteAsync(null);

        viewModel.SelectedDeviceModel.Points.Should().Contain(builtInPoint);
        viewModel.StatusMessage.Should().Be("\u5185\u7F6E\u903B\u8F91\u70B9\u4F4D\u4E0D\u5141\u8BB8\u5220\u9664\uFF0C\u53EF\u4EE5\u76F4\u63A5\u4FEE\u6539\u914D\u7F6E\u3002");

        await viewModel.AddLogicalPointCommand.ExecuteAsync(null);
        var customPoint = viewModel.SelectedLogicalPoint!;

        await viewModel.DeleteLogicalPointCommand.ExecuteAsync(null);

        viewModel.SelectedDeviceModel.Points.Should().NotContain(customPoint);
        viewModel.StatusMessage.Should().Be("\u81EA\u5B9A\u4E49\u903B\u8F91\u70B9\u4F4D\u5DF2\u5220\u9664\u3002");
    }

    [Fact]
    public void Barcode_rule_view_model_exposes_default_rules()
    {
        var viewModel = new BarcodeRuleViewModel(new BarcodeRuleService());

        viewModel.EmployeeRuleDisplay.Should().Contain("EMP");
        viewModel.VfdRuleDisplay.Should().Contain("VFD");
    }

    private static StationConfigViewModel CreateStationConfigViewModel(
        IReadOnlyList<Station> stations,
        IReadOnlyList<string>? availablePorts = null,
        StationConfigurationChangeNotifier? notifier = null)
    {
        return new StationConfigViewModel(
            new StationConfigurationService(new FakeStationRepository(stations)),
            new FakeSerialPortCatalog(availablePorts ?? ["COM1"]),
            notifier ?? new StationConfigurationChangeNotifier());
    }

    private static Station CreateStation(int slotCount = 1)
    {
        var station = new Station(Guid.NewGuid(), "\u4E00\u53F7\u6D4B\u8BD5\u5DE5\u4F4D");
        for (var number = 1; number <= slotCount; number++)
        {
            var slot = new StationSlot(
                Guid.NewGuid(),
                new SlotNumber(number),
                new SlotCommunicationConfig(
                    new SerialPortName($"COM{number}"),
                    new ModbusAddress((byte)number),
                    new ModbusAddress((byte)(number + 10)),
                    new ModbusAddress((byte)(number + 20)),
                    9600));
            var instrument = new SlotInstrument(Guid.NewGuid(), "\u7535\u538B\u8868");
            instrument.AddPoint(new InstrumentPoint(Guid.NewGuid(), "Voltage", "\u7535\u538B", DataType.Number, "V"));
            slot.AddInstrument(instrument);
            station.AddSlot(slot);
        }

        return station;
    }

    private sealed class FakeSerialPortCatalog : ISerialPortCatalog
    {
        private readonly IReadOnlyList<string> _ports;

        public FakeSerialPortCatalog(IReadOnlyList<string> ports)
        {
            _ports = ports;
        }

        public IReadOnlyList<string> ListPortNames() => _ports;
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

        public Task SaveAsync(Station station, CancellationToken ct) => Task.CompletedTask;
    }
}
