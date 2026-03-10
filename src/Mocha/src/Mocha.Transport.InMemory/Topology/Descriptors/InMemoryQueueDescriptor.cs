namespace Mocha.Transport.InMemory;

/// <summary>
/// Descriptor implementation for configuring a InMemory queue.
/// </summary>
internal sealed class InMemoryQueueDescriptor
    : MessagingDescriptorBase<InMemoryQueueConfiguration>
    , IInMemoryQueueDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryQueueDescriptor"/> class.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The initial queue name.</param>
    public InMemoryQueueDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new InMemoryQueueConfiguration { Name = name };
    }

    /// <inheritdoc />
    protected override InMemoryQueueConfiguration Configuration { get; set; }

    /// <inheritdoc />
    public IInMemoryQueueDescriptor Name(string name)
    {
        Configuration.Name = name;
        return this;
    }

    /// <summary>
    /// Creates the final queue configuration.
    /// </summary>
    /// <returns>The configured queue configuration.</returns>
    public InMemoryQueueConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new queue descriptor with the specified name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The queue name.</param>
    /// <returns>A new queue descriptor.</returns>
    public static InMemoryQueueDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
