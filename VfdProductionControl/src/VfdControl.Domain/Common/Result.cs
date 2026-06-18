namespace VfdControl.Domain.Common;

public sealed class Result<T>
{
    public bool IsSuccess { get; }

    public T? Value { get; }

    public DomainError? Error { get; }

    private Result(bool isSuccess, T? value, DomainError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(string code, string message) => new(false, default, new DomainError(code, message));
}
