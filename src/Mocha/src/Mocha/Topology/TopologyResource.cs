namespace Mocha;

/// <summary>
/// Base class for topology resources (queues, exchanges, topics) that are managed by a <see cref="MessagingTopology"/>.
/// </summary>
public abstract class TopologyResource
{
    /// <summary>
    /// Gets the topology configuration used during initialization.
    /// </summary>
    protected TopologyConfiguration Configuration { get; private set; } = null!;

    /// <summary>
    /// Gets or sets the topology that owns this resource.
    /// </summary>
    public MessagingTopology Topology { get; set; } = null!;

    /// <summary>
    /// Gets the address URI of this topology resource.
    /// </summary>
    public Uri Address { get; protected set; } = null!;

    /// <summary>
    /// Initializes this resource from the specified configuration.
    /// </summary>
    /// <param name="configuration">The topology configuration.</param>
    public void Initialize(TopologyConfiguration configuration)
    {
        Configuration = configuration;

        OnInitialize(configuration);
    }

    protected abstract void OnInitialize(TopologyConfiguration configuration);

    /// <summary>
    /// Completes initialization, releasing the configuration reference.
    /// </summary>
    public void Complete()
    {
        OnComplete(Configuration);
        Configuration = null!;
    }

    protected virtual void OnComplete(TopologyConfiguration configuration) { }
}
