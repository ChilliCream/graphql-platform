using System.Diagnostics.CodeAnalysis;

namespace HotChocolate;

/// <summary>
/// An aggregate error allows to pass a collection of error in a single error object.
/// </summary>
public sealed record AggregateError : Error
{
    /// <summary>
    /// Initializes a new instance of <see cref="AggregateError"/>.
    /// </summary>
    /// <param name="errors">
    /// The errors.
    /// </param>
    [SetsRequiredMembers]
    public AggregateError(IEnumerable<IError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        Message = ExecutionAbstractionsResources.AggregateError_Message;
        Errors = [.. errors];

        if (Errors.Count == 0)
        {
            throw new ArgumentException(
                "At least one error is required.",
                nameof(errors));
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AggregateError"/>.
    /// </summary>
    /// <param name="errors">
    /// The errors.
    /// </param>
    [SetsRequiredMembers]
    public AggregateError(params IError[] errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Length == 0)
        {
            throw new ArgumentException(
                "At least one error is required.",
                nameof(errors));
        }

        Message = ExecutionAbstractionsResources.AggregateError_Message;
        Errors = errors;
    }

    /// <summary>
    /// Gets the actual errors.
    /// </summary>
    public IReadOnlyList<IError> Errors { get; init; }
}
