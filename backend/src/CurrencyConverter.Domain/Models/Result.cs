namespace CurrencyConverter.Domain.Models;

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, null, null);
    public static Result Failure(string error, string? errorCode = null) => new(false, error, errorCode);
    public static Result<T> Success<T>(T value) => new(value);
    public static Result<T> Failure<T>(string error, string? errorCode = null) => new(error, errorCode);
}

public class Result<T> : Result
{
    public T? Value { get; }

    internal Result(T value) : base(true, null, null)
    {
        Value = value;
    }

    internal Result(string error, string? errorCode) : base(false, error, errorCode)
    {
        Value = default;
    }
}
