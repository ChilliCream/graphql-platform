namespace Mocha.Transport.NATS;

/// <summary>
/// Convention that auto-provisions streams and subjects in the topology for dispatch endpoints
/// when they do not already exist.
/// </summary>
public sealed class NatsDispatchEndpointTopologyConvention : INatsDispatchEndpointTopologyConvention
{
    /// <summary>
    /// Discovers and creates missing topology resources (streams, subjects) needed by the dispatch endpoint.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="endpoint">The dispatch endpoint being configured.</param>
    /// <param name="configuration">The endpoint configuration specifying the target subject name.</param>
    public void DiscoverTopology(
        IMessagingConfigurationContext context,
        NatsDispatchEndpoint endpoint,
        NatsDispatchEndpointConfiguration configuration)
    {
        if (configuration.SubjectName is null)
        {
            return;
        }

        var topology = (NatsMessagingTopology)endpoint.Transport.Topology;

        // Ensure the subject exists in the topology
        if (topology.Subjects.All(s => s.Name != configuration.SubjectName))
        {
            // Find or create the stream that captures this subject
            var stream = topology.GetStreamForSubject(configuration.SubjectName);
            var streamName = stream?.Name;

            if (stream is null)
            {
                // Create a stream for this subject using a convention-based name
                streamName = "mocha-" + configuration.SubjectName;

                if (topology.Streams.All(s => s.Name != streamName))
                {
                    topology.AddStream(new NatsStreamConfiguration
                    {
                        Name = streamName,
                        Subjects = [configuration.SubjectName]
                    });
                }
            }

            topology.AddSubject(new NatsSubjectConfiguration
            {
                Name = configuration.SubjectName,
                StreamName = streamName
            });
        }

        // Ensure convention subjects exist for all routes bound to this endpoint
        foreach (var (runtimeType, kind) in configuration.Routes)
        {
            var conventionSubjectName =
                kind == OutboundRouteKind.Publish
                    ? context.Naming.GetPublishEndpointName(runtimeType)
                    : context.Naming.GetSendEndpointName(runtimeType);

            if (configuration.SubjectName == conventionSubjectName)
            {
                continue;
            }

            if (topology.Subjects.All(s => s.Name != conventionSubjectName))
            {
                // Ensure there's a stream for the convention subject
                var conventionStream = topology.GetStreamForSubject(conventionSubjectName);
                var conventionStreamName = conventionStream?.Name;

                if (conventionStream is null)
                {
                    conventionStreamName = "mocha-" + conventionSubjectName;
                    if (topology.Streams.All(s => s.Name != conventionStreamName))
                    {
                        topology.AddStream(new NatsStreamConfiguration
                        {
                            Name = conventionStreamName,
                            Subjects = [conventionSubjectName]
                        });
                    }
                }

                topology.AddSubject(new NatsSubjectConfiguration
                {
                    Name = conventionSubjectName,
                    StreamName = conventionStreamName
                });
            }
        }
    }
}
