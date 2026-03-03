namespace HotChocolate;

/// <summary>
/// Represents a single result in a batch resolver invocation.
/// A batch result is a union of a value or a GraphQL error,
/// allowing partial success in batch operations.
/// </summary>
/// <typeparam name="TValue">The value type.</typeparam>
public readonly record struct BatchResult<TValue>
{
    /// <summary>
    /// Creates a new value result.
    /// </summary>
    /// <param name="value">The value.</param>
    public BatchResult(TValue value)
    {
        Value = value;
        Error = null;
        IsError = false;
    }

    /// <summary>
    /// Creates a new error result.
    /// </summary>
    /// <param name="error">The GraphQL error.</param>
    public BatchResult(IError error)
    {
        Error = error ?? throw new ArgumentNullException(nameof(error));
        Value = default!;
        IsError = true;
    }

    /// <summary>
    /// Gets the value. If <see cref="IsError"/> is <c>true</c>, returns
    /// <c>null</c> or <c>default</c> depending on the type.
    /// </summary>
    public TValue Value { get; }

    /// <summary>
    /// Gets the error. If <see cref="IsError"/> is <c>false</c>, returns <c>null</c>.
    /// </summary>
    public IError? Error { get; }

    /// <summary>
    /// Gets a value indicating whether this result represents an error.
    /// </summary>
    public bool IsError { get; }

    /// <summary>
    /// Gets a value indicating whether this result represents a value.
    /// </summary>
    public bool IsSuccess => !IsError;

    /// <summary>
    /// Creates a new value result.
    /// </summary>
    /// <param name="value">An arbitrary value.</param>
    /// <returns>A value result.</returns>
    public static BatchResult<TValue> Resolve(TValue value)
        => new(value);

    /// <summary>
    /// Creates a new error result.
    /// </summary>
    /// <param name="error">A GraphQL error.</param>
    /// <returns>An error result.</returns>
    public static BatchResult<TValue> Reject(IError error)
        => new(error);

    /// <summary>
    /// Creates a new value result.
    /// </summary>
    /// <param name="value">An arbitrary value.</param>
    public static implicit operator BatchResult<TValue>(TValue value)
        => new(value);

    /// <summary>
    /// Extracts the value from a result.
    /// </summary>
    /// <param name="result">An arbitrary result.</param>
    public static implicit operator TValue(BatchResult<TValue> result)
        => result.Value;
}
