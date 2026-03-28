namespace Mocha.Transport.NATS;

/// <summary>
/// Convention that auto-provisions streams, subjects, and consumers in the topology for receive endpoints,
/// creating the necessary JetStream resources for each inbound route.
/// </summary>
public sealed class NatsReceiveEndpointTopologyConvention : INatsReceiveEndpointTopologyConvention
{
    /// <summary>
    /// Discovers and creates missing topology resources (streams, subjects, consumers) needed by the receive endpoint
    /// based on its inbound message routes.
    /// </summary>
    /// <param name="context">The messaging configuration context providing naming and routing information.</param>
    /// <param name="endpoint">The receive endpoint being configured.</param>
    /// <param name="configuration">The endpoint configuration specifying the source subject and consumer name.</param>
    /// <exception cref="InvalidOperationException">Thrown if the subject name is not set on the configuration.</exception>
    public void DiscoverTopology(
        IMessagingConfigurationContext context,
        NatsReceiveEndpoint endpoint,
        NatsReceiveEndpointConfiguration configuration)
    {
        if (configuration.SubjectName is null)
        {
            throw new InvalidOperationException("Subject name is required");
        }

        var topology = (NatsMessagingTopology)endpoint.Transport.Topology;

        // Collect all subjects needed for this endpoint's routes
        var subjects = new List<string>();

        if (endpoint.Kind is ReceiveEndpointKind.Reply)
        {
            // Reply endpoints only need their own subject
            subjects.Add(configuration.SubjectName);
        }
        else if (endpoint.Kind is ReceiveEndpointKind.Error or ReceiveEndpointKind.Skipped)
        {
            subjects.Add(configuration.SubjectName);
        }
        else
        {
            var routes = context.Router.GetInboundByEndpoint(endpoint);
            foreach (var route in routes)
            {
                if (route.MessageType is null)
                {
                    continue;
                }

                var publishSubjectName = context.Naming.GetPublishEndpointName(route.MessageType.RuntimeType);
                if (!subjects.Contains(publishSubjectName))
                {
                    subjects.Add(publishSubjectName);
                }

                var sendSubjectName = context.Naming.GetSendEndpointName(route.MessageType.RuntimeType);
                if (sendSubjectName != publishSubjectName && !subjects.Contains(sendSubjectName))
                {
                    subjects.Add(sendSubjectName);
                }
            }

            // If no routes discovered, use the endpoint's own subject
            if (subjects.Count == 0)
            {
                subjects.Add(configuration.SubjectName);
            }
        }

        // Determine stream name based on endpoint
        var streamName = endpoint.Kind == ReceiveEndpointKind.Reply
            ? "_mocha_reply_" + configuration.SubjectName
            : "mocha-" + configuration.Name;

        // Ensure the stream exists
        if (topology.GetStreamForSubject(subjects[0]) is null
            && topology.Streams.All(s => s.Name != streamName))
        {
            var streamConfig = new NatsStreamConfiguration
            {
                Name = streamName,
                Subjects = [.. subjects],
                AutoProvision = configuration.AutoProvision
            };

            // Reply streams get TTL to prevent orphan accumulation
            if (endpoint.Kind == ReceiveEndpointKind.Reply)
            {
                streamConfig.MaxAge = TimeSpan.FromMinutes(5);
                streamConfig.MaxMsgs = 1000;
            }

            topology.AddStream(streamConfig);
        }

        // Ensure subjects exist in the topology
        foreach (var subjectName in subjects)
        {
            if (topology.Subjects.All(s => s.Name != subjectName))
            {
                topology.AddSubject(new NatsSubjectConfiguration
                {
                    Name = subjectName,
                    StreamName = streamName
                });
            }
        }

        // Ensure the consumer exists
        var consumerName = configuration.ConsumerName ?? configuration.Name!;
        if (topology.Consumers.All(c => c.Name != consumerName))
        {
            topology.AddConsumer(new NatsConsumerConfiguration
            {
                Name = consumerName,
                StreamName = streamName,
                FilterSubject = subjects.Count == 1 ? subjects[0] : null,
                MaxAckPending = configuration.MaxPrefetch,
                AutoProvision = configuration.AutoProvision
            });
        }
    }
}
