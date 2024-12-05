namespace HotChocolate.Fusion.Results;

public readonly record struct Result<TValue>
{
    public bool IsFailure { get; }

    public bool IsSuccess { get; }

    public List<Error> Errors { get; } = [];

    public TValue Value => IsSuccess
        ? _value
        : throw new Exception("Value may not be accessed on an unsuccessful result.");

    private readonly TValue _value = default!;

    private Result(TValue value)
    {
        _value = value;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        Errors = [error];
        IsFailure = true;
    }

    private Result(List<Error> errors)
    {
        if (errors.Count == 0)
        {
            IsSuccess = true;
        }
        else
        {
            Errors = [.. errors];
            IsFailure = true;
        }
    }

    /// <summary>
    /// Creates a <see cref="Result{TValue}"/> from a value.
    /// </summary>
    public static implicit operator Result<TValue>(TValue value)
    {
        return new Result<TValue>(value);
    }

    /// <summary>
    /// Creates a <see cref="Result{TValue}"/> from an error.
    /// </summary>
    public static implicit operator Result<TValue>(Error error)
    {
        return new Result<TValue>(error);
    }

    /// <summary>
    /// Creates a <see cref="Result{TValue}"/> from a list of errors.
    /// </summary>
    public static implicit operator Result<TValue>(List<Error> errors)
    {
        return new Result<TValue>(errors);
    }

    /// <summary>
    /// Creates a <see cref="Result{TValue}"/> from a <see cref="Result"/>.
    /// </summary>
    public static implicit operator Result<TValue>(Result result)
    {
        return new Result<TValue>(result.Errors);
    }
}
