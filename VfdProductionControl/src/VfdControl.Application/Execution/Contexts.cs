using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;
using VfdControl.Domain.Stations;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Application.Execution;

public sealed record DeviceAddress(
    Guid SlotId,
    MeasurementSource Source,
    string EndpointName);

public sealed record ReadCommand(
    string PointName,
    string? RegisterAddress = null);

public sealed record ReadStringCommand(
    string PointName,
    string? RegisterAddress = null);

public sealed record WriteCommand(
    string CommandName,
    string? Value = null);

public sealed record DeviceRunContext(
    Guid SessionId,
    Guid DeviceRunId,
    StationSlot Slot,
    Barcode Barcode,
    ProcessPlanVersion PlanVersion,
    Action<SlotStepProgressSnapshot>? ProgressHandler = null);

public sealed record StationSessionContext(
    Guid SessionId,
    Station Station,
    EmployeeCode OperatorCode,
    ProcessPlanVersion PlanVersion,
    IReadOnlyList<SlotBarcodeBinding> SlotBindings,
    Action<SlotStepProgressSnapshot>? ProgressHandler = null);

public sealed record SlotBarcodeBinding(
    StationSlot Slot,
    Barcode Barcode);

public sealed record BarcodeScannedEventArgs(
    string Text,
    DateTimeOffset ScannedAt);
