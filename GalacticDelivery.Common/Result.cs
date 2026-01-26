namespace GalacticDelivery.Common;

public sealed record Error(string Code, string Message);

public readonly struct Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success()
    {
        return new Result(true, null);
    }

    public static Result Failure(Error error)
    {
        return new Result(false, error ?? throw new ArgumentNullException(nameof(error)));
    }

    public Result<T> Bind<T>(Func<Result<T>> binder)
    {
        if (binder is null)
        {
            throw new ArgumentNullException(nameof(binder));
        }

        return IsSuccess ? binder() : Result<T>.Failure(Error!);
    }

    public T Match<T>(Func<T> onSuccess, Func<Error, T> onFailure)
    {
        if (onSuccess is null)
        {
            throw new ArgumentNullException(nameof(onSuccess));
        }
        if (onFailure is null)
        {
            throw new ArgumentNullException(nameof(onFailure));
        }

        return IsSuccess ? onSuccess() : onFailure(Error!);
    }
}

public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public Error? Error { get; }

    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, null);
    }

    public static Result<T> Failure(Error error)
    {
        return new Result<T>(false, default, error ?? throw new ArgumentNullException(nameof(error)));
    }

    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
        if (mapper is null)
        {
            throw new ArgumentNullException(nameof(mapper));
        }

        return IsSuccess ? Result<TOut>.Success(mapper(Value!)) : Result<TOut>.Failure(Error!);
    }

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
    {
        if (binder is null)
        {
            throw new ArgumentNullException(nameof(binder));
        }

        return IsSuccess ? binder(Value!) : Result<TOut>.Failure(Error!);
    }

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        if (onSuccess is null)
        {
            throw new ArgumentNullException(nameof(onSuccess));
        }
        if (onFailure is null)
        {
            throw new ArgumentNullException(nameof(onFailure));
        }

        return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }
}
