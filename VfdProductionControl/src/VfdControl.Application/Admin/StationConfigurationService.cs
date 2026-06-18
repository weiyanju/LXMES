using VfdControl.Application.Abstractions;
using VfdControl.Application.Common;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Stations;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Application.Admin;

public sealed class StationConfigurationService
{
    private const string NoPortDisplay = "\u65E0";

    private readonly IStationRepository _stationRepository;

    public StationConfigurationService(IStationRepository stationRepository)
    {
        _stationRepository = stationRepository;
    }

    public Task<IReadOnlyList<Station>> ListStationsAsync(CancellationToken ct)
    {
        return _stationRepository.ListAsync(ct);
    }

    public async Task<AppResult<Station>> CreateStationAsync(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return AppResult<Station>.Failure("\u5DE5\u4F4D\u540D\u79F0\u4E0D\u80FD\u4E3A\u7A7A\u3002", "Station.NameRequired");
        }

        var station = new Station(Guid.NewGuid(), name.Trim());
        await _stationRepository.SaveAsync(station, ct);
        return AppResult<Station>.Success(station);
    }

    public async Task<AppResult<StationSlot>> AddSlotAsync(
        Guid stationId,
        int slotNumber,
        string portName,
        byte deviceAddress,
        int baudRate,
        CancellationToken ct)
    {
        var station = await _stationRepository.GetAsync(stationId, ct);
        if (station is null)
        {
            return AppResult<StationSlot>.Failure("\u5DE5\u4F4D\u4E0D\u5B58\u5728\u3002", "Station.NotFound");
        }

        if (station.Slots.Any(slot => slot.Number.Value == slotNumber))
        {
            return AppResult<StationSlot>.Failure("\u69FD\u4F4D\u7F16\u53F7\u5DF2\u5B58\u5728\u3002", "StationSlot.Duplicate");
        }

        var config = CreateCommunicationConfig(portName, deviceAddress, baudRate);
        if (!config.IsSuccess || config.Value is null)
        {
            return AppResult<StationSlot>.Failure(config.Message, config.ErrorCode);
        }

        var slot = new StationSlot(Guid.NewGuid(), new SlotNumber(slotNumber), config.Value);
        var validation = ValidateAddressConflicts(station, slot, config.Value);
        if (!validation.IsSuccess)
        {
            return AppResult<StationSlot>.Failure(validation.Message, validation.ErrorCode);
        }

        station.AddSlot(slot);
        await _stationRepository.SaveAsync(station, ct);
        return AppResult<StationSlot>.Success(slot);
    }

    public async Task<AppResult<StationSlot>> AddNextSlotAsync(
        Guid stationId,
        string? portName,
        int vfdAddress,
        int voltageMeterAddress,
        int currentMeterAddress,
        int baudRate,
        CancellationToken ct)
    {
        var station = await _stationRepository.GetAsync(stationId, ct);
        if (station is null)
        {
            return AppResult<StationSlot>.Failure("\u5DE5\u4F4D\u4E0D\u5B58\u5728\u3002", "Station.NotFound");
        }

        var slotNumber = station.Slots.Count == 0
            ? 1
            : station.Slots.Max(slot => slot.Number.Value) + 1;
        var config = CreateCommunicationConfig(portName, vfdAddress, voltageMeterAddress, currentMeterAddress, baudRate);
        if (!config.IsSuccess || config.Value is null)
        {
            return AppResult<StationSlot>.Failure(config.Message, config.ErrorCode);
        }

        var slot = new StationSlot(Guid.NewGuid(), new SlotNumber(slotNumber), config.Value);
        var validation = ValidateAddressConflicts(station, slot, config.Value);
        if (!validation.IsSuccess)
        {
            return AppResult<StationSlot>.Failure(validation.Message, validation.ErrorCode);
        }

        station.AddSlot(slot);
        await _stationRepository.SaveAsync(station, ct);
        return AppResult<StationSlot>.Success(slot);
    }

    public async Task<AppResult<StationSlot>> UpdateSlotSerialPortAsync(
        Guid stationId,
        int slotNumber,
        string portName,
        CancellationToken ct)
    {
        var station = await _stationRepository.GetAsync(stationId, ct);
        var slot = station?.Slots.SingleOrDefault(item => item.Number.Value == slotNumber);
        if (station is null || slot is null)
        {
            return AppResult<StationSlot>.Failure("\u69FD\u4F4D\u4E0D\u5B58\u5728\u3002", "StationSlot.NotFound");
        }

        return await UpdateSlotConfigurationAsync(
            stationId,
            slotNumber,
            portName,
            slot.CommunicationConfig.VfdAddress.Value,
            slot.CommunicationConfig.VoltageMeterAddress.Value,
            slot.CommunicationConfig.CurrentMeterAddress.Value,
            slot.CommunicationConfig.BaudRate,
            ct);
    }

    public async Task<AppResult<StationSlot>> UpdateSlotConfigurationAsync(
        Guid stationId,
        int slotNumber,
        string? portName,
        int vfdAddress,
        int voltageMeterAddress,
        int currentMeterAddress,
        int baudRate,
        CancellationToken ct)
    {
        var station = await _stationRepository.GetAsync(stationId, ct);
        var slot = station?.Slots.SingleOrDefault(item => item.Number.Value == slotNumber);
        if (station is null || slot is null)
        {
            return AppResult<StationSlot>.Failure("\u69FD\u4F4D\u4E0D\u5B58\u5728\u3002", "StationSlot.NotFound");
        }

        var config = CreateCommunicationConfig(portName, vfdAddress, voltageMeterAddress, currentMeterAddress, baudRate);
        if (!config.IsSuccess || config.Value is null)
        {
            return AppResult<StationSlot>.Failure(config.Message, config.ErrorCode);
        }

        var validation = ValidateAddressConflicts(station, slot, config.Value);
        if (!validation.IsSuccess)
        {
            return AppResult<StationSlot>.Failure(validation.Message, validation.ErrorCode);
        }

        slot.UpdateCommunicationConfig(config.Value);
        await _stationRepository.SaveAsync(station, ct);
        return AppResult<StationSlot>.Success(slot);
    }

    public async Task<AppResult<StationSlot>> UpdateSlotDisplayNameAsync(
        Guid stationId,
        int slotNumber,
        string displayName,
        CancellationToken ct)
    {
        var station = await _stationRepository.GetAsync(stationId, ct);
        var slot = station?.Slots.SingleOrDefault(item => item.Number.Value == slotNumber);
        if (station is null || slot is null)
        {
            return AppResult<StationSlot>.Failure("\u69FD\u4F4D\u4E0D\u5B58\u5728\u3002", "StationSlot.NotFound");
        }

        slot.UpdateDisplayName(displayName);
        await _stationRepository.SaveAsync(station, ct);
        return AppResult<StationSlot>.Success(slot);
    }

    public async Task<AppResult> DeleteSlotAsync(Guid stationId, int slotNumber, CancellationToken ct)
    {
        var station = await _stationRepository.GetAsync(stationId, ct);
        if (station is null)
        {
            return AppResult.Failure("\u5DE5\u4F4D\u4E0D\u5B58\u5728\u3002", "Station.NotFound");
        }

        if (!station.RemoveSlot(slotNumber))
        {
            return AppResult.Failure("\u69FD\u4F4D\u4E0D\u5B58\u5728\u3002", "StationSlot.NotFound");
        }

        await _stationRepository.SaveAsync(station, ct);
        return AppResult.Success();
    }

    public async Task<AppResult> AssignPortsBySlotOrderAsync(
        Guid stationId,
        IReadOnlyList<string> availablePorts,
        CancellationToken ct)
    {
        var station = await _stationRepository.GetAsync(stationId, ct);
        if (station is null)
        {
            return AppResult.Failure("\u5DE5\u4F4D\u4E0D\u5B58\u5728\u3002", "Station.NotFound");
        }

        var normalizedPorts = availablePorts
            .Where(port => !string.IsNullOrWhiteSpace(port))
            .Select(port => port.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var orderedSlots = station.Slots.OrderBy(slot => slot.Number.Value).ToList();
        for (var index = 0; index < orderedSlots.Count; index++)
        {
            var slot = orderedSlots[index];
            var portName = index < normalizedPorts.Count ? normalizedPorts[index] : null;
            var config = CreateCommunicationConfig(
                portName,
                slot.CommunicationConfig.VfdAddress.Value,
                slot.CommunicationConfig.VoltageMeterAddress.Value,
                slot.CommunicationConfig.CurrentMeterAddress.Value,
                slot.CommunicationConfig.BaudRate);
            if (!config.IsSuccess || config.Value is null)
            {
                return AppResult.Failure(config.Message, config.ErrorCode);
            }

            slot.UpdateCommunicationConfig(config.Value);
        }

        await _stationRepository.SaveAsync(station, ct);
        return AppResult.Success();
    }

    public async Task<AppResult<SlotInstrument>> AddInstrumentAsync(
        Guid stationId,
        int slotNumber,
        string name,
        CancellationToken ct)
    {
        var station = await _stationRepository.GetAsync(stationId, ct);
        var slot = station?.Slots.SingleOrDefault(item => item.Number.Value == slotNumber);
        if (station is null || slot is null)
        {
            return AppResult<SlotInstrument>.Failure("\u69FD\u4F4D\u4E0D\u5B58\u5728\u3002", "StationSlot.NotFound");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return AppResult<SlotInstrument>.Failure("\u4EEA\u8868\u540D\u79F0\u4E0D\u80FD\u4E3A\u7A7A\u3002", "Instrument.NameRequired");
        }

        var instrument = new SlotInstrument(Guid.NewGuid(), name.Trim());
        slot.AddInstrument(instrument);
        await _stationRepository.SaveAsync(station, ct);
        return AppResult<SlotInstrument>.Success(instrument);
    }

    public async Task<AppResult<InstrumentPoint>> AddInstrumentPointAsync(
        Guid stationId,
        int slotNumber,
        Guid instrumentId,
        string key,
        string name,
        DataType dataType,
        string unit,
        CancellationToken ct)
    {
        var station = await _stationRepository.GetAsync(stationId, ct);
        var slot = station?.Slots.SingleOrDefault(item => item.Number.Value == slotNumber);
        var instrument = slot?.Instruments.SingleOrDefault(item => item.Id == instrumentId);
        if (station is null || slot is null || instrument is null)
        {
            return AppResult<InstrumentPoint>.Failure("\u4EEA\u8868\u4E0D\u5B58\u5728\u3002", "Instrument.NotFound");
        }

        var point = new InstrumentPoint(Guid.NewGuid(), key.Trim(), name.Trim(), dataType, unit.Trim());
        instrument.AddPoint(point);
        await _stationRepository.SaveAsync(station, ct);
        return AppResult<InstrumentPoint>.Success(point);
    }

    private static AppResult<SlotCommunicationConfig> CreateCommunicationConfig(
        string portName,
        byte deviceAddress,
        int baudRate)
    {
        return CreateCommunicationConfig(portName, deviceAddress, deviceAddress + 10, deviceAddress + 20, baudRate);
    }

    private static AppResult<SlotCommunicationConfig> CreateCommunicationConfig(
        string? portName,
        int vfdAddress,
        int voltageMeterAddress,
        int currentMeterAddress,
        int baudRate)
    {
        if (baudRate <= 0)
        {
            return AppResult<SlotCommunicationConfig>.Failure("\u6CE2\u7279\u7387\u5FC5\u987B\u5927\u4E8E 0\u3002", "Serial.BaudRateInvalid");
        }

        if (!IsValidModbusAddress(vfdAddress) ||
            !IsValidModbusAddress(voltageMeterAddress) ||
            !IsValidModbusAddress(currentMeterAddress))
        {
            return AppResult<SlotCommunicationConfig>.Failure("Modbus \u5730\u5740\u5FC5\u987B\u5728 1-247 \u4E4B\u95F4\u3002", "Modbus.AddressInvalid");
        }

        if (new[] { vfdAddress, voltageMeterAddress, currentMeterAddress }.Distinct().Count() != 3)
        {
            return AppResult<SlotCommunicationConfig>.Failure(
                "\u540C\u4E00\u69FD\u4F4D\u7684\u53D8\u9891\u5668\u3001\u7535\u538B\u8868\u3001\u7535\u6D41\u8868\u5730\u5740\u4E0D\u80FD\u91CD\u590D\u3002",
                "StationSlot.AddressConflict");
        }

        var normalizedPortName = NormalizePortName(portName);
        return AppResult<SlotCommunicationConfig>.Success(new SlotCommunicationConfig(
            normalizedPortName is null ? null : new SerialPortName(normalizedPortName),
            new ModbusAddress((byte)vfdAddress),
            new ModbusAddress((byte)voltageMeterAddress),
            new ModbusAddress((byte)currentMeterAddress),
            baudRate));
    }

    private static AppResult ValidateAddressConflicts(
        Station station,
        StationSlot targetSlot,
        SlotCommunicationConfig candidateConfig)
    {
        if (candidateConfig.PortName is null)
        {
            return AppResult.Success();
        }

        var addresses = new List<byte>();
        foreach (var slot in station.Slots)
        {
            var config = ReferenceEquals(slot, targetSlot) ? candidateConfig : slot.CommunicationConfig;
            if (!string.Equals(config.PortName?.Value, candidateConfig.PortName.Value, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            addresses.Add(config.VfdAddress.Value);
            addresses.Add(config.VoltageMeterAddress.Value);
            addresses.Add(config.CurrentMeterAddress.Value);
        }

        if (!station.Slots.Contains(targetSlot))
        {
            addresses.Add(candidateConfig.VfdAddress.Value);
            addresses.Add(candidateConfig.VoltageMeterAddress.Value);
            addresses.Add(candidateConfig.CurrentMeterAddress.Value);
        }

        return addresses.Count == addresses.Distinct().Count()
            ? AppResult.Success()
            : AppResult.Failure("\u540C\u4E00\u4E32\u53E3\u4E0B\u7684 Modbus \u5730\u5740\u4E0D\u80FD\u91CD\u590D\u3002", "StationSlot.AddressConflict");
    }

    private static bool IsValidModbusAddress(int address)
    {
        return address is >= 1 and <= 247;
    }

    private static string? NormalizePortName(string? portName)
    {
        if (string.IsNullOrWhiteSpace(portName) || string.Equals(portName.Trim(), NoPortDisplay, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return portName.Trim().ToUpperInvariant();
    }
}
