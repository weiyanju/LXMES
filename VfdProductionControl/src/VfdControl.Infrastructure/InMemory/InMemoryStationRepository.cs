using VfdControl.Application.Abstractions;
using VfdControl.Domain.Stations;

namespace VfdControl.Infrastructure.InMemory;

public sealed class InMemoryStationRepository : IStationRepository
{
    private readonly Dictionary<Guid, Station> _stations;

    public InMemoryStationRepository(IEnumerable<Station>? stations = null)
    {
        _stations = (stations ?? []).ToDictionary(station => station.Id);
    }

    public Task<IReadOnlyList<Station>> ListAsync(CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<Station>>(_stations.Values.ToList());
    }

    public Task<Station?> GetAsync(Guid stationId, CancellationToken ct)
    {
        _stations.TryGetValue(stationId, out var station);
        return Task.FromResult(station);
    }

    public Task SaveAsync(Station station, CancellationToken ct)
    {
        _stations[station.Id] = station;
        return Task.CompletedTask;
    }
}
