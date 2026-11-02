using CookieCrumble;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests;

public class PostgresDispatchTargetTests
{
    [Fact]
    public void GetDispatchEndpoint_Should_AddUnprovisionedQueue_When_QueueNotDeclaredUnderExplicitBinding()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t => t.BindExplicitly());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = runtime.GetDispatchEndpoint(new Uri("postgres:q/reporting.events"));

        // assert
        var postgresEndpoint = Assert.IsType<PostgresDispatchEndpoint>(endpoint);
        Assert.NotNull(postgresEndpoint.Queue);
        Assert.False(postgresEndpoint.Queue.AutoProvision);
        Assert.Equal(TopologyOrigin.Endpoint, postgresEndpoint.Queue.Origin);
        Assert.Same(postgresEndpoint.Queue, postgresEndpoint.Destination);
        PostgresDescribeSnapshot.Create(transport.Describe()).MatchInlineSnapshot(
            """
            {
              "Schema": "postgres",
              "TransportType": "PostgresMessagingTransport",
              "Entities": [
                {
                  "Kind": "queue",
                  "Name": "reporting.events",
                  "AutoProvision": false,
                  "Origin": "endpoint"
                }
              ],
              "Links": []
            }
            """);
    }

    [Fact]
    public void GetDispatchEndpoint_Should_AddUnprovisionedTopic_When_TopicNotDeclaredUnderExplicitBinding()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t => t.BindExplicitly());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = runtime.GetDispatchEndpoint(new Uri("postgres:t/reporting-events"));

        // assert
        var postgresEndpoint = Assert.IsType<PostgresDispatchEndpoint>(endpoint);
        Assert.NotNull(postgresEndpoint.Topic);
        Assert.False(postgresEndpoint.Topic.AutoProvision);
        Assert.Equal(TopologyOrigin.Endpoint, postgresEndpoint.Topic.Origin);
        Assert.Same(postgresEndpoint.Topic, postgresEndpoint.Destination);
        PostgresDescribeSnapshot.Create(transport.Describe()).MatchInlineSnapshot(
            """
            {
              "Schema": "postgres",
              "TransportType": "PostgresMessagingTransport",
              "Entities": [
                {
                  "Kind": "topic",
                  "Name": "reporting-events",
                  "AutoProvision": false,
                  "Origin": "endpoint"
                }
              ],
              "Links": []
            }
            """);
    }

    [Fact]
    public void GetDispatchEndpoint_Should_ProvisionQueue_When_QueueNotDeclaredUnderImplicitBinding()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t => t.BindImplicitly());

        // act
        var endpoint = runtime.GetDispatchEndpoint(new Uri("postgres:q/reporting.events"));

        // assert
        var postgresEndpoint = Assert.IsType<PostgresDispatchEndpoint>(endpoint);
        Assert.NotNull(postgresEndpoint.Queue);
        Assert.Null(postgresEndpoint.Queue.AutoProvision);
        Assert.Equal(TopologyOrigin.Convention, postgresEndpoint.Queue.Origin);
    }
}
