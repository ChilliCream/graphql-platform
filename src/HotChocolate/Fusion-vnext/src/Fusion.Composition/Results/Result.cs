namespace HotChocolate.Fusion.Results;

public readonly record struct Result
{
    public bool IsFailure { get; }

    public bool IsSuccess { get; }

    public List<Error> Errors { get; } = [];

    public Result()
    {
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

    public static Result Success() => new();

    /// <summary>
    /// Creates a <see cref="Result"/> from an error.
    /// </summary>
    public static implicit operator Result(Error error)
    {
        return new Result(error);
    }

    /// <summary>
    /// Creates a <see cref="Result"/> from a list of errors.
    /// </summary>
    public static implicit operator Result(List<Error> errors)
    {
        return new Result(errors);
    }
}
