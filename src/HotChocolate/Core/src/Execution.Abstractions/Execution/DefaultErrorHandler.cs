namespace HotChocolate.Execution.Errors;

/// <summary>
/// The default implementation of <see cref="IErrorHandler"/>.
/// </summary>
public sealed class DefaultErrorHandler : IErrorHandler
{
    private readonly IErrorFilter[] _filters;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultErrorHandler"/> class.
    /// </summary>
    /// <param name="errorFilters">The error filters.</param>
    public DefaultErrorHandler(
        IEnumerable<IErrorFilter> errorFilters)
    {
        ArgumentNullException.ThrowIfNull(errorFilters);

        _filters = errorFilters.ToArray();
    }

    private DefaultErrorHandler()
    {
        _filters = [];
    }

    /// <inheritdoc />
    public IError Handle(IError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var current = error;

        foreach (var filter in _filters)
        {
            current = filter.OnError(current);

            if (current is null)
            {
                throw new InvalidOperationException(
                    "Unexpected Execution Error");
            }
        }

        return current;
    }

    /// <summary>
    /// Gets the default error handler.
    /// </summary>
    public static DefaultErrorHandler Default { get; } = new();
}
