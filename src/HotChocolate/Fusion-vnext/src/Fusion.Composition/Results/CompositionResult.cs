using HotChocolate.Fusion.Errors;

namespace HotChocolate.Fusion.Results;

public readonly record struct CompositionResult
{
    public bool IsFailure { get; }

    public bool IsSuccess { get; }

    public List<CompositionError> Errors { get; } = [];

    public CompositionResult()
    {
        IsSuccess = true;
    }

    private CompositionResult(CompositionError error)
    {
        Errors = [error];
        IsFailure = true;
    }

    private CompositionResult(List<CompositionError> errors)
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
    /// Creates a <see cref="CompositionResult"/> from a composition error.
    /// </summary>
    public static implicit operator CompositionResult(CompositionError error)
    {
        return new CompositionResult(error);
    }

    /// <summary>
    /// Creates a <see cref="CompositionResult"/> from a list of composition errors.
    /// </summary>
    public static implicit operator CompositionResult(List<CompositionError> errors)
    {
        return new CompositionResult(errors);
    }
}
