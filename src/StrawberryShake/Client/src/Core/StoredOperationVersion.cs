namespace StrawberryShake;

/// <summary>
/// A snapshot of the current stored operation.
/// </summary>
public readonly struct StoredOperationVersion
{
    /// <summary>
    /// Creates a new instance of <see cref="StoredOperationVersion"/>.
    /// </summary>
    /// <param name="request">
    /// The operation request.
    /// </param>
    /// <param name="result">
    /// The last result.
    /// </param>
    /// <param name="subscribers">
    /// The count of subscribers that are listening to this operation.
    /// </param>
    /// <param name="lastModified">
    /// The time when this operation was last modified.
    /// </param>
    public StoredOperationVersion(
        OperationRequest request,
        IOperationResult? result,
        int subscribers,
        DateTime lastModified)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        Result = result;
        Subscribers = subscribers;
        LastModified = lastModified;
    }

    /// <summary>
    /// Gets the operation request.
    /// </summary>
    public OperationRequest Request { get; }

    /// <summary>
    /// Gets the last result.
    /// </summary>
    public IOperationResult? Result { get; }

    /// <summary>
    /// Gets the count of subscribers that are listening to this operation.
    /// </summary>
    public int Subscribers { get; }

    /// <summary>
    /// Gets the time when this operation was last modified.
    /// </summary>
    public DateTime LastModified { get; }
}
