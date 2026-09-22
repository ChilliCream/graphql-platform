namespace HotChocolate.Execution;

/// <summary>
/// Stores the operation id computed for the current request.
/// </summary>
internal sealed class OperationIdInfo : RequestFeature
{
    /// <summary>
    /// Gets or sets the operation id. <c>null</c> means the id has not been computed yet.
    /// </summary>
    public string? Value { get; set; }

    /// <inheritdoc />
    protected internal override void Reset() => Value = null;
}
