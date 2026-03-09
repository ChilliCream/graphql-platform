using HotChocolate.Execution.Configuration;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using NATS.Extensions.Microsoft.DependencyInjection;
using Squadron;
using Xunit.Abstractions;

namespace HotChocolate.Subscriptions.Nats;

public class NatsIntegrationTests : SubscriptionIntegrationTestBase, IClassFixture<NatsResource>
{
    private readonly NatsResource _natsResource;

    public NatsIntegrationTests(NatsResource natsResource, ITestOutputHelper output)
        : base(output)
    {
        _natsResource = natsResource;
    }

    [Fact]
    public override Task Subscribe_Infer_Topic()
        => base.Subscribe_Infer_Topic();

    [Fact]
    public override Task Subscribe_Static_Topic()
        => base.Subscribe_Static_Topic();

    [Fact]
    public override Task Subscribe_Topic_With_Arguments()
        => base.Subscribe_Topic_With_Arguments();

    [Fact]
    public override Task Subscribe_Topic_With_Arguments_2_Subscriber()
        => base.Subscribe_Topic_With_Arguments_2_Subscriber();

    [Fact]
    public override Task Subscribe_Topic_With_Arguments_2_Topics()
        => base.Subscribe_Topic_With_Arguments_2_Topics();

    [Fact]
    public override Task Subscribe_Topic_With_2_Arguments()
        => base.Subscribe_Topic_With_2_Arguments();

    [Fact]
    public override Task Subscribe_And_Complete_Topic()
        => base.Subscribe_And_Complete_Topic();

    [Fact]
    public override Task Subscribe_And_Complete_Topic_With_ValueTypeMessage()
        => base.Subscribe_And_Complete_Topic_With_ValueTypeMessage();

    [Fact]
    public async Task Subscribe_With_Different_Prefixes_Should_Not_Leak_Messages()
    {
        using var cts = new CancellationTokenSource(Timeout);
        await using var primary = CreateServer(builder =>
        {
            builder
                .AddSubscriptionType<Subscription>()
                .ModifyOptions(o => o.StrictValidation = false);
            builder.Services.AddSingleton(new SubscriptionOptions { TopicPrefix = "primary" });
        });
        await using var secondary = CreateServer(builder =>
        {
            builder
                .AddSubscriptionType<Subscription>()
                .ModifyOptions(o => o.StrictValidation = false);
            builder.Services.AddSingleton(new SubscriptionOptions { TopicPrefix = "secondary" });
        });

        var result = await primary.ExecuteRequestAsync(
            "subscription { onMessage }",
            cancellationToken: cts.Token);
        await using var responseStream = result.ExpectResponseStream();
        var results = responseStream.ReadResultsAsync().ConfigureAwait(false);

        var primarySender = primary.GetRequiredService<ITopicEventSender>();
        var secondarySender = secondary.GetRequiredService<ITopicEventSender>();

        await secondarySender.SendAsync("OnMessage", "secondary", cts.Token);
        await secondarySender.CompleteAsync("OnMessage");

        await primarySender.SendAsync("OnMessage", "primary", cts.Token);
        await primarySender.CompleteAsync("OnMessage");

        var snapshot = new Snapshot();

        await foreach (var response in results.WithCancellation(cts.Token).ConfigureAwait(false))
        {
            snapshot.Add(response);
        }

        snapshot.MatchInline(
            """
            {
              "data": {
                "onMessage": "primary"
              }
            }
            """);
    }

    protected override void ConfigurePubSub(IRequestExecutorBuilder graphqlBuilder)
    {
        // register NATS client
        graphqlBuilder.Services
            .AddNatsClient(
                builder => builder.ConfigureOptions(
                    options => options.Configure(
                        nats => nats.Opts = nats.Opts with
                        {
                            Url = _natsResource.NatsConnectionString
                        })))
            .AddLogging();

        // register subscription provider
        graphqlBuilder.AddNatsSubscriptions();
    }
}
