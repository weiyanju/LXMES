using VfdControl.Domain.Enums;

namespace VfdControl.Domain.Stations;

public sealed record InstrumentPoint(
    Guid Id,
    string Key,
    string Name,
    DataType DataType,
    string Unit);
