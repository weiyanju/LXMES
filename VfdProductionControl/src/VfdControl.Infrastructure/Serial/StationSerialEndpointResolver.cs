using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Stations;

namespace VfdControl.Infrastructure.Serial;

public sealed class StationSerialEndpointResolver
{
    private readonly IStationRepository _stationRepository;

    public StationSerialEndpointResolver(IStationRepository stationRepository)
    {
        _stationRepository = stationRepository;
    }

    public SerialDeviceEndpoint? Resolve(DeviceAddress address)
    {
        var slot = FindSlot(address.SlotId);
        var config = slot?.CommunicationConfig;
        if (config?.PortName is null)
        {
            return null;
        }

        var modbusAddress = address.Source == MeasurementSource.Instrument
            ? ResolveInstrumentAddress(config, address.EndpointName)
            : config.VfdAddress;

        return new SerialDeviceEndpoint(
            config.PortName.Value,
            modbusAddress.Value,
            config.BaudRate);
    }

    private StationSlot? FindSlot(Guid slotId)
    {
        var stations = _stationRepository.ListAsync(CancellationToken.None).GetAwaiter().GetResult();
        return stations
            .SelectMany(station => station.Slots)
            .SingleOrDefault(slot => slot.Id == slotId);
    }

    private static Domain.ValueObjects.ModbusAddress ResolveInstrumentAddress(
        SlotCommunicationConfig config,
        string endpointName)
    {
        return endpointName.Contains("Current", StringComparison.OrdinalIgnoreCase)
            ? config.CurrentMeterAddress
            : config.VoltageMeterAddress;
    }
}
