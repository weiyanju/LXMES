using Dapper;
using VfdControl.Application.Abstractions;
using VfdControl.Domain.Stations;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Infrastructure.Sql;

public sealed class SqlStationRepository : IStationRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public SqlStationRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Station>> ListAsync(CancellationToken ct)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var stationRows = (await connection.QueryAsync<StationRow>(
            "SELECT Id, Name FROM dbo.Stations WHERE IsActive = 1 ORDER BY Name;")).ToList();
        if (stationRows.Count == 0)
        {
            return [];
        }

        var slotRows = (await connection.QueryAsync<StationSlotRow>(
            """
            SELECT Id, StationId, SlotNumber, DisplayName, PortName, VfdAddress, VoltageMeterAddress, CurrentMeterAddress, BaudRate
            FROM dbo.StationSlots
            WHERE IsEnabled = 1
              AND StationId IN @stationIds
            ORDER BY SlotNumber;
            """,
            new { stationIds = stationRows.Select(station => station.Id).ToArray() })).ToList();

        return stationRows.Select(station => BuildStation(station, slotRows)).ToList();
    }

    public async Task<Station?> GetAsync(Guid stationId, CancellationToken ct)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var station = await connection.QuerySingleOrDefaultAsync<StationRow>(
            "SELECT Id, Name FROM dbo.Stations WHERE Id = @stationId AND IsActive = 1;",
            new { stationId });
        if (station is null)
        {
            return null;
        }

        var slots = (await connection.QueryAsync<StationSlotRow>(
            """
            SELECT Id, StationId, SlotNumber, DisplayName, PortName, VfdAddress, VoltageMeterAddress, CurrentMeterAddress, BaudRate
            FROM dbo.StationSlots
            WHERE StationId = @stationId AND IsEnabled = 1
            ORDER BY SlotNumber;
            """,
            new { stationId })).ToList();

        return BuildStation(station, slots);
    }

    public async Task SaveAsync(Station station, CancellationToken ct)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        await connection.ExecuteAsync(
            """
            MERGE dbo.Stations AS target
            USING (SELECT @Id AS Id) AS source
            ON target.Id = source.Id
            WHEN MATCHED THEN
                UPDATE SET Name = @Name, IsActive = 1, UpdatedAt = SYSDATETIMEOFFSET()
            WHEN NOT MATCHED THEN
                INSERT (Id, Name, IsActive, StationJson) VALUES (@Id, @Name, 1, @StationJson);
            """,
            new { station.Id, station.Name, StationJson = "{}" },
            transaction);

        await connection.ExecuteAsync(
            "DELETE FROM dbo.StationSlots WHERE StationId = @stationId;",
            new { stationId = station.Id },
            transaction);

        foreach (var slot in station.Slots)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO dbo.StationSlots
                    (Id, StationId, SlotNumber, DisplayName, PortName, VfdAddress, VoltageMeterAddress, CurrentMeterAddress, BaudRate, IsEnabled, ConfigJson)
                VALUES
                    (@Id, @StationId, @SlotNumber, @DisplayName, @PortName, @VfdAddress, @VoltageMeterAddress, @CurrentMeterAddress, @BaudRate, 1, @ConfigJson);
                """,
                new
                {
                    slot.Id,
                    StationId = station.Id,
                    SlotNumber = slot.Number.Value,
                    slot.DisplayName,
                    PortName = slot.CommunicationConfig.PortName?.Value,
                    VfdAddress = slot.CommunicationConfig.VfdAddress.Value,
                    VoltageMeterAddress = slot.CommunicationConfig.VoltageMeterAddress.Value,
                    CurrentMeterAddress = slot.CommunicationConfig.CurrentMeterAddress.Value,
                    slot.CommunicationConfig.BaudRate,
                    ConfigJson = "{}"
                },
                transaction);
        }

        await transaction.CommitAsync(ct);
    }

    private static Station BuildStation(StationRow row, IReadOnlyList<StationSlotRow> slots)
    {
        var station = new Station(row.Id, row.Name);
        foreach (var slot in slots.Where(slot => slot.StationId == row.Id).OrderBy(slot => slot.SlotNumber))
        {
            station.AddSlot(new StationSlot(
                slot.Id,
                new SlotNumber(slot.SlotNumber),
                new SlotCommunicationConfig(
                    string.IsNullOrWhiteSpace(slot.PortName) ? null : new SerialPortName(slot.PortName),
                    new ModbusAddress(slot.VfdAddress),
                    new ModbusAddress(slot.VoltageMeterAddress),
                    new ModbusAddress(slot.CurrentMeterAddress),
                    slot.BaudRate),
                slot.DisplayName));
        }

        return station;
    }

    private sealed record StationRow(Guid Id, string Name);

    private sealed record StationSlotRow(
        Guid Id,
        Guid StationId,
        int SlotNumber,
        string DisplayName,
        string? PortName,
        byte VfdAddress,
        byte VoltageMeterAddress,
        byte CurrentMeterAddress,
        int BaudRate);
}
