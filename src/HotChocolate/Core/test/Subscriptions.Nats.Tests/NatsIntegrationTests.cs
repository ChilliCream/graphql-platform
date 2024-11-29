using AlterNats;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
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

    protected override void ConfigurePubSub(IRequestExecutorBuilder graphqlBuilder)
    {
        // register NATS
        graphqlBuilder.Services
            .AddNats(poolSize: 1, options => options with
            {
                Url = _natsResource.NatsConnectionString,
            })
            .AddLogging();

        // register subscription provider
        graphqlBuilder.AddNatsSubscriptions();
    }
}
