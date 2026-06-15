using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests;

/// <summary>
/// Verifies typed satellite queue configuration for PostgreSQL: verbatim names, disable flags,
/// and byte-identical output when no satellite configuration is changed.
/// </summary>
public class PostgresSatelliteTests
{
    [Fact]
    public void Describe_Should_RenameErrorSatellite_When_ErrorQueueNamed()
    {
        // arrange
        // The verbatim name "LEGACY.Orders.Error" must survive unchanged; the naming convention
        // must not kebab-case or otherwise transform it.
        var runtime = CreateRuntime(t =>
        {
            t.BindExplicitly();
            t.Queue("orders").AutoProvision(true).Handler<OrderCreatedHandler>()
                .ErrorQueue("LEGACY.Orders.Error");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        PostgresDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_StayByteIdentical_When_EndpointQueueUntouched()
    {
        // arrange
        // When no satellite configuration is provided the conventional names must be produced,
        // byte-identical to the pre-typed-satellite output.
        var runtime = CreateRuntime(t =>
        {
            t.BindExplicitly();
            t.Queue("orders").AutoProvision(true).Handler<OrderCreatedHandler>();
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        PostgresDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_OmitErrorSatellite_When_ErrorDisabled()
    {
        // arrange
        // DisableErrorQueue removes the error satellite from topology entirely; no queue with
        // the conventional "_error" suffix should appear.
        var runtime = CreateRuntime(t =>
        {
            t.BindExplicitly();
            t.Queue("orders").AutoProvision(true).Handler<OrderCreatedHandler>()
                .DisableErrorQueue();
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        PostgresDescribeSnapshot.Create(description).MatchSnapshot();
    }

    private static MessagingRuntime CreateRuntime(Action<IPostgresMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        builder.AddEventHandler<OrderCreatedHandler>();
        return builder
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test");
                configureTransport(t);
            })
            .BuildRuntime();
    }
}
