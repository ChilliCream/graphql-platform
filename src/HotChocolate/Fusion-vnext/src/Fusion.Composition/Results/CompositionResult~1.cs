using System.Collections.Immutable;
using HotChocolate.Fusion.Errors;

namespace HotChocolate.Fusion.Results;

public readonly record struct CompositionResult<TValue>
{
    public bool IsFailure { get; }

    public bool IsSuccess { get; }

    public ImmutableArray<CompositionError> Errors { get; } = [];

    public TValue Value => IsSuccess
        ? _value
        : throw new Exception("Value may not be accessed on an unsuccessful result.");

    private readonly TValue _value = default!;

    private CompositionResult(TValue value)
    {
        _value = value;
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

    /// <summary>
    /// Creates a <see cref="CompositionResult{TValue}"/> from a value.
    /// </summary>
    public static implicit operator CompositionResult<TValue>(TValue value)
    {
        return new CompositionResult<TValue>(value);
    }

    /// <summary>
    /// Creates a <see cref="CompositionResult{TValue}"/> from a composition error.
    /// </summary>
    public static implicit operator CompositionResult<TValue>(CompositionError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CompositionResult<TValue>(error);
    }

    /// <summary>
    /// Creates a <see cref="CompositionResult{TValue}"/> from an array of composition errors.
    /// </summary>
    public static implicit operator CompositionResult<TValue>(
        ImmutableArray<CompositionError> errors)
    {
        return new CompositionResult<TValue>(errors);
    }

    /// <summary>
    /// Creates a <see cref="CompositionResult{TValue}"/> from a <see cref="CompositionResult"/>.
    /// </summary>
    public static implicit operator CompositionResult<TValue>(CompositionResult result)
    {
        return new CompositionResult<TValue>(result.Errors);
    }
}
