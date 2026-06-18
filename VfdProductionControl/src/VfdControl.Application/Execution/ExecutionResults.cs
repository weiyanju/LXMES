using VfdControl.Domain.Enums;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Application.Execution;

public sealed record CommandResult(
    bool IsSuccess,
    string Message,
    string? ErrorCode = null,
    TimeSpan? Elapsed = null,
    string RequestJson = "{}",
    string ResponseJson = "{}")
{
    public static CommandResult Success(string message = "", string requestJson = "{}", string responseJson = "{}")
    {
        return new(true, message, RequestJson: requestJson, ResponseJson: responseJson);
    }

    public static CommandResult Failure(string message, string? errorCode = null, string requestJson = "{}", string responseJson = "{}")
    {
        return new(false, message, errorCode, RequestJson: requestJson, ResponseJson: responseJson);
    }
}

public sealed record CommandResult<T>(
    bool IsSuccess,
    string Message,
    T? Value,
    string? ErrorCode = null,
    TimeSpan? Elapsed = null,
    string RequestJson = "{}",
    string ResponseJson = "{}")
{
    public static CommandResult<T> Success(T value, string message = "", string requestJson = "{}", string responseJson = "{}")
    {
        return new(true, message, value, RequestJson: requestJson, ResponseJson: responseJson);
    }

    public static CommandResult<T> Failure(string message, string? errorCode = null, string requestJson = "{}", string responseJson = "{}")
    {
        return new(false, message, default, errorCode, RequestJson: requestJson, ResponseJson: responseJson);
    }
}

public sealed record DeviceRunResult(
    Guid DeviceRunId,
    Conclusion Conclusion,
    IReadOnlyList<StepRunSnapshot> Steps);

public sealed record SlotStepProgressSnapshot(
    Guid SlotId,
    Guid DeviceRunId,
    int Sequence,
    string StepName,
    Conclusion Conclusion,
    string Message,
    TimeSpan? Remaining = null,
    bool IsRunning = false);

public sealed record StationSessionResult(
    Guid SessionId,
    Conclusion Conclusion,
    IReadOnlyList<DeviceRunResult> DeviceRuns);

public sealed record StationSessionSnapshot(
    Guid SessionId,
    Guid StationId,
    string OperatorCode,
    DateTimeOffset StartedAt);

public sealed record DeviceRunSnapshot(
    Guid DeviceRunId,
    Guid SessionId,
    Guid SlotId,
    string Barcode,
    Conclusion Conclusion);

public sealed record StepRunSnapshot(
    Guid StepRunId,
    Guid DeviceRunId,
    int Sequence,
    string StepName,
    Conclusion Conclusion,
    string Message = "");

public sealed record CommandTraceSnapshot(
    Guid TraceId,
    Guid StepRunId,
    Guid SlotId,
    string CommandName,
    string RequestJson,
    string ResponseJson,
    bool IsSuccess,
    DateTimeOffset CreatedAt);

public sealed record DeviceRunSummary(
    Guid DeviceRunId,
    Guid SessionId,
    string Barcode,
    Conclusion Conclusion,
    DateTimeOffset StartedAt);

public sealed record DeviceRunQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Barcode = null,
    Conclusion? Conclusion = null);

public sealed record OperatorInfo(
    string EmployeeCode,
    string DisplayName);

public sealed record DeviceBarcodeInfo(
    string Barcode,
    string Model,
    string SerialNumber);

public sealed record SessionResultMessage(
    Guid SessionId,
    Conclusion Conclusion);

public sealed record DeviceRunResultMessage(
    Guid DeviceRunId,
    string Barcode,
    Conclusion Conclusion);
