using static HotChocolate.Properties.AbstractionResources;

namespace HotChocolate;

/// <summary>
/// An aggregate error allows to pass a collection of error in a single error object.
/// </summary>
public class AggregateError : Error
{
    /// <summary>
    /// Initializes a new instance of <see cref="AggregateError"/>.
    /// </summary>
    /// <param name="errors">
    /// The errors.
    /// </param>
    public AggregateError(IEnumerable<IError> errors)
        : base(AggregateError_Message)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        Errors = errors.ToArray();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AggregateError"/>.
    /// </summary>
    /// <param name="errors">
    /// The errors.
    /// </param>
    public AggregateError(params IError[] errors)
        : base(AggregateError_Message)
    {
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    /// <summary>
    /// Gets the actual errors.
    /// </summary>
    public IReadOnlyList<IError> Errors { get; }
}
