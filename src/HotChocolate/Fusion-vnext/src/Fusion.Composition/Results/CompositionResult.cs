namespace HotChocolate.Fusion.Results;

public readonly record struct CompositionResult
{
    public bool IsFailure { get; }

    public bool IsSuccess { get; }

    public List<Error> Errors { get; } = [];

    public CompositionResult()
    {
        IsSuccess = true;
    }

    private CompositionResult(Error error)
    {
        Errors = [error];
        IsFailure = true;
    }

    private CompositionResult(List<Error> errors)
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

    public static CompositionResult Success() => new();

    /// <summary>
    /// Creates a <see cref="CompositionResult"/> from an error.
    /// </summary>
    public static implicit operator CompositionResult(Error error)
    {
        return new CompositionResult(error);
    }

    /// <summary>
    /// Creates a <see cref="CompositionResult"/> from a list of errors.
    /// </summary>
    public static implicit operator CompositionResult(List<Error> errors)
    {
        return new CompositionResult(errors);
    }
}
