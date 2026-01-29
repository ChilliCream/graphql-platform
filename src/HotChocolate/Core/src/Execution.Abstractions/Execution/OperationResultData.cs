namespace HotChocolate.Execution;

/// <summary>
/// Represents the data portion of a GraphQL operation result with its associated formatter and memory management.
/// </summary>
public readonly struct OperationResultData
{
    /// <summary>
    /// Initializes a new instance of <see cref="OperationResultData"/>.
    /// </summary>
    /// <param name="value">
    /// The object representing the data property of the GraphQL response.
    /// </param>
    /// <param name="isValueNull">
    /// <c>true</c> if the value object represents a null value; otherwise, <c>false</c>.
    /// This allows distinguishing between a null data payload and an object that serializes to null.
    /// </param>
    /// <param name="formatter">
    /// The formatter that can serialize the operation result and its data to JSON.
    /// </param>
    /// <param name="memoryHolder">
    /// The memory holder that needs to be disposed after the operation result was handled.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> or <paramref name="formatter"/> is <c>null</c>.
    /// </exception>
    public OperationResultData(object value, bool isValueNull, IRawJsonFormatter formatter, IDisposable? memoryHolder)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(formatter);

        Value = value;
        IsValueNull = isValueNull;
        Formatter = formatter;
        MemoryHolder = memoryHolder;
    }

    /// <summary>
    /// Gets the object representing the `Data` property of the GraphQL response.
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// Gets a value indicating whether the value object represents a null value.
    /// This allows distinguishing between a null data payload and an object that serializes to null.
    /// </summary>
    public bool IsValueNull { get; }

    /// <summary>
    /// Gets the formatter that can serialize the operation result and its data.
    /// </summary>
    public IRawJsonFormatter Formatter { get; }

    /// <summary>
    /// Gets the memory holder that needs to be disposed after the operation result was handled.
    /// </summary>
    public IDisposable? MemoryHolder { get; }
}
