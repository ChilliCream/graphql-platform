namespace HotChocolate.Resolvers;

/// <summary>
/// Represents the result of a single resolver invocation within a batch.
/// </summary>
public readonly struct ResolverResult
{
    private ResolverResult(object? value, IError? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>
    /// Gets the resolved value.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets the error if the resolution failed.
    /// </summary>
    public IError? Error { get; }

    /// <summary>
    /// Gets a value indicating whether this result represents an error.
    /// </summary>
    public bool IsError => Error is not null;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ResolverResult Ok(object? value) => new(value, null);

    /// <summary>
    /// Creates an error result.
    /// </summary>
    public static ResolverResult Fail(IError error) => new(null, error);

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    public static implicit operator ResolverResult(string? value) => Ok(value);

    /// <summary>
    /// Implicitly converts an error to an error result.
    /// </summary>
    public static implicit operator ResolverResult(Error error) => Fail(error);
}
