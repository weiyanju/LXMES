namespace VfdControl.Application.Common;

public class AppResult
{
    protected AppResult(bool isSuccess, string message, string? errorCode)
    {
        IsSuccess = isSuccess;
        Message = message;
        ErrorCode = errorCode;
    }

    public bool IsSuccess { get; }

    public string Message { get; }

    public string? ErrorCode { get; }

    public static AppResult Success(string message = "") => new(true, message, null);

    public static AppResult Failure(string message, string? errorCode = null) => new(false, message, errorCode);
}

public sealed class AppResult<T> : AppResult
{
    private AppResult(bool isSuccess, string message, string? errorCode, T? value)
        : base(isSuccess, message, errorCode)
    {
        Value = value;
    }

    public T? Value { get; }

    public static AppResult<T> Success(T value, string message = "") => new(true, message, null, value);

    public new static AppResult<T> Failure(string message, string? errorCode = null) => new(false, message, errorCode, default);
}
