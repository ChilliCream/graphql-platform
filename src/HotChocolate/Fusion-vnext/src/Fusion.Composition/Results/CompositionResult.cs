using System.Collections.Immutable;
using HotChocolate.Fusion.Errors;

namespace HotChocolate.Fusion.Results;

public readonly record struct CompositionResult
{
    public bool IsFailure { get; }

    public bool IsSuccess { get; }

    public ImmutableArray<CompositionError> Errors { get; } = [];

    public CompositionResult()
    {
        IsSuccess = true;
    }

    private CompositionResult(CompositionError error)
    {
        Errors = [error];
        IsFailure = true;
    }

    private CompositionResult(ImmutableArray<CompositionError> errors)
    {
        if (errors.Length == 0)
        {
            IsSuccess = true;
        }
        else
        {
            Errors = errors;
            IsFailure = true;
        }
    }

    public static CompositionResult Success() => new();

    /// <summary>
    /// Creates a <see cref="CompositionResult"/> from a composition error.
    /// </summary>
    public static implicit operator CompositionResult(CompositionError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CompositionResult(error);
    }

    /// <summary>
    /// Creates a <see cref="CompositionResult"/> from an array of composition errors.
    /// </summary>
    public static implicit operator CompositionResult(ImmutableArray<CompositionError> errors)
    {
        return new CompositionResult(errors);
    }

    /// <summary>
    /// Creates a <see cref="CompositionResult"/> from a list of composition errors.
    /// </summary>
    public static implicit operator CompositionResult(List<CompositionError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        return new CompositionResult([.. errors]);
    }
}
