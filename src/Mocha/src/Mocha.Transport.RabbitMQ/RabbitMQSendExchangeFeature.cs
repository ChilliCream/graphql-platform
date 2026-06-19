namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Feature stored on a message type to carry a partial send exchange declaration.
/// The stored configuration is merged onto the convention exchange during topology discovery
/// using the 3.5 merge rules.
/// </summary>
internal sealed class RabbitMQSendExchangeFeature : IRabbitMQExchangeContributionDescriptor
{
    /// <summary>
    /// Gets the configuration accumulated by contribution descriptor calls.
    /// </summary>
    internal RabbitMQExchangeConfiguration Configuration { get; } = new()
    {
        Origin = TopologyOrigin.Declared
    };

    /// <inheritdoc />
    public IRabbitMQExchangeContributionDescriptor Type(string type)
    {
        Configuration.Type = type;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQExchangeContributionDescriptor Durable(bool durable = true)
    {
        Configuration.Durable = durable;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQExchangeContributionDescriptor AutoDelete(bool autoDelete = true)
    {
        Configuration.AutoDelete = autoDelete;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQExchangeContributionDescriptor WithArgument(string key, object value)
    {
        Configuration.Arguments ??= new Dictionary<string, object>();
        Configuration.Arguments[key] = value;
        return this;
    }

    /// <inheritdoc />
    public IRabbitMQExchangeContributionDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }
}
