using FluentAssertions;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Stations;
using VfdControl.Domain.ValueObjects;
using VfdControl.Infrastructure.Serial;

namespace VfdControl.Infrastructure.Tests.Serial;

public class StationSerialEndpointResolverTests
{
    [Fact]
    public void Resolve_returns_vfd_endpoint_from_slot_configuration()
    {
        var slotId = Guid.NewGuid();
        var station = new Station(Guid.NewGuid(), "Station");
        station.AddSlot(new StationSlot(
            slotId,
            new SlotNumber(1),
            new SlotCommunicationConfig(
                new SerialPortName("COM6"),
                new ModbusAddress(1),
                new ModbusAddress(11),
                new ModbusAddress(21),
                9600)));
        var resolver = new StationSerialEndpointResolver(new FakeStationRepository([station]));

        var endpoint = resolver.Resolve(new DeviceAddress(slotId, MeasurementSource.Vfd, "Control"));

        endpoint.Should().Be(new SerialDeviceEndpoint("COM6", 1, 9600));
    }

    private sealed class FakeStationRepository : IStationRepository
    {
        private readonly IReadOnlyList<Station> _stations;

        public FakeStationRepository(IReadOnlyList<Station> stations)
        {
            _stations = stations;
        }

        public Task<IReadOnlyList<Station>> ListAsync(CancellationToken ct) => Task.FromResult(_stations);

        public Task<Station?> GetAsync(Guid stationId, CancellationToken ct)
        {
            return Task.FromResult(_stations.SingleOrDefault(station => station.Id == stationId));
        }

        public Task SaveAsync(Station station, CancellationToken ct) => Task.CompletedTask;
    }
}
