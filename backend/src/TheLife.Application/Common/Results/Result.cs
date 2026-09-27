namespace TheLife.Application.Common.Results;

/// <summary>
/// Either a successful value or an <see cref="Error"/>.
/// Thanks to the implicit conversions a handler can simply write
/// <c>return postDto;</c> or <c>return PostErrors.NotFound;</c>.
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T value)
    {
        _value = value;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        Error = error;
        IsSuccess = false;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    /// <summary>Only set when <see cref="IsFailure"/> is true.</summary>
    public Error? Error { get; }

    /// <summary>The value. Reading it on a failed result is a programming error and throws.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot read the value of a failed result.");

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}

/// <summary>
/// "No value". Used as the response type of commands that only need to report success,
/// e.g. <c>ICommand&lt;Unit&gt;</c>. Return it with <c>return Unit.Value;</c>.
/// </summary>
public readonly record struct Unit
{
    public static readonly Unit Value = new();
}
