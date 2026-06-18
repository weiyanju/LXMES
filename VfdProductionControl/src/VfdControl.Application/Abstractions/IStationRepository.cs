using VfdControl.Domain.Stations;

namespace VfdControl.Application.Abstractions;

public interface IStationRepository
{
    Task<IReadOnlyList<Station>> ListAsync(CancellationToken ct);

    Task<Station?> GetAsync(Guid stationId, CancellationToken ct);

    Task SaveAsync(Station station, CancellationToken ct);
}
