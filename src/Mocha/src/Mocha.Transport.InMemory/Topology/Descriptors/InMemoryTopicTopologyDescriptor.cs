namespace Mocha.Transport.InMemory;

/// <summary>
/// Descriptor implementation for configuring an in-memory topic topology entity.
/// </summary>
internal sealed class InMemoryTopicTopologyDescriptor
    : MessagingDescriptorBase<InMemoryTopicConfiguration>
    , IInMemoryTopicTopologyDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTopicTopologyDescriptor"/> class.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The initial topic name.</param>
    public InMemoryTopicTopologyDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new InMemoryTopicConfiguration { Name = name };
    }

    /// <inheritdoc />
    protected internal override InMemoryTopicConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IInMemoryTopicTopologyDescriptor Name(string name)
    {
        Configuration.Name = name;
        return this;
    }

    /// <summary>
    /// Creates the final topic configuration.
    /// </summary>
    /// <returns>The configured topic configuration.</returns>
    public InMemoryTopicConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new topic descriptor with the specified name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The topic name.</param>
    /// <returns>A new topic descriptor.</returns>
    public static InMemoryTopicTopologyDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
