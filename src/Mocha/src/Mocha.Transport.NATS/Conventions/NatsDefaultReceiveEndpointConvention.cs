namespace Mocha.Transport.NATS;

/// <summary>
/// Default convention that assigns subject names, consumer names, error endpoints, and skipped endpoints
/// to NATS receive endpoint configurations that do not already have them set.
/// </summary>
public sealed class NatsDefaultReceiveEndpointConvention : INatsReceiveEndpointConfigurationConvention
{
    /// <inheritdoc />
    public void Configure(IMessagingConfigurationContext context, NatsReceiveEndpointConfiguration configuration)
    {
        configuration.SubjectName ??= configuration.Name;
        configuration.ConsumerName ??= configuration.Name;

        if (configuration is { Kind: ReceiveEndpointKind.Default, SubjectName: { } subjectName })
        {
            if (configuration.ErrorEndpoint is null)
            {
                var errorName = context.Naming.GetReceiveEndpointName(subjectName, ReceiveEndpointKind.Error);
                configuration.ErrorEndpoint = new UriBuilder
                {
                    Host = "",
                    Scheme = "nats",
                    Path = "s/" + errorName
                }.Uri;
            }

            if (configuration.SkippedEndpoint is null)
            {
                var skippedName = context.Naming.GetReceiveEndpointName(subjectName, ReceiveEndpointKind.Skipped);
                configuration.SkippedEndpoint = new UriBuilder
                {
                    Host = "",
                    Scheme = "nats",
                    Path = "s/" + skippedName
                }.Uri;
            }
        }
    }
}
