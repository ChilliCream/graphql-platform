namespace HotChocolate.Execution.Configuration;

/// <summary>
/// This struct is used as a oneOf to allow for both a sync and an async hook
/// into the request executor created event.
///
/// The event can be used to capture the request executor after it was created.
/// </summary>
public readonly struct OnRequestExecutorCreatedAction
{
    /// <summary>
    /// Initializes a new instance of <see cref="OnRequestExecutorCreatedAction"/>.
    /// </summary>
    /// <param name="created">
    /// The synchronous action.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="created"/> is <c>null</c>.
    /// </exception>
    public OnRequestExecutorCreatedAction(OnRequestExecutorCreated created)
    {
        Created = created ?? throw new ArgumentNullException(nameof(created));
        CreatedAsync = default;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OnRequestExecutorCreatedAction"/>.
    /// </summary>
    /// <param name="createdAsync">
    /// The asynchronous action.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="createdAsync"/> is <c>null</c>.
    /// </exception>
    public OnRequestExecutorCreatedAction(OnRequestExecutorCreatedAsync createdAsync)
    {
        Created = default;
        CreatedAsync = createdAsync ?? throw new ArgumentNullException(nameof(createdAsync));
    }

    /// <summary>
    /// Gets the synchronous action.
    /// </summary>
    public OnRequestExecutorCreated? Created { get; }

    /// <summary>
    /// Gets the asynchronous action.
    /// </summary>
    public OnRequestExecutorCreatedAsync? CreatedAsync { get; }
}

/// <summary>
/// This delegate is used to configure the request executor options.
/// </summary>
public delegate void OnRequestExecutorCreated(
    ConfigurationContext context,
    IRequestExecutor executor);

/// <summary>
/// This delegate is used to configure the request executor options.
/// </summary>
public delegate ValueTask OnRequestExecutorCreatedAsync(
    ConfigurationContext context,
    IRequestExecutor executor,
    CancellationToken cancellationToken);
