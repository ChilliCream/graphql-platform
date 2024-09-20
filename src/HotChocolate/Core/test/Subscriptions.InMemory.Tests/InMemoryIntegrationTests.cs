using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using Xunit.Abstractions;

namespace HotChocolate.Subscriptions.InMemory;

public class InMemoryIntegrationTests : SubscriptionIntegrationTestBase
{
    public InMemoryIntegrationTests(ITestOutputHelper output)
        : base(output)
    {
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
    public virtual async Task Invalid_Message_Type()
    {
        // arrange
        using var cts = new CancellationTokenSource(5000);
        await using var services = CreateServer<Subscription3>();
        var sender = services.GetRequiredService<ITopicEventSender>();

        var result = await services.ExecuteRequestAsync(
            "subscription { onMessage2(arg1: \"a\", arg2: \"b\") }",
            cancellationToken: cts.Token);

        // we need to execute the read for the subscription to start receiving.
        await using var responseStream = result.ExpectResponseStream();

        // act
        async Task Send() => await sender.SendAsync("OnMessage2_a_b", 1, cts.Token).ConfigureAwait(false);

        // assert
        var exception = await Assert.ThrowsAsync<InvalidMessageTypeException>(Send);
        Assert.Equal(typeof(string), exception.TopicMessageType);
        Assert.Equal(typeof(int), exception.RequestedMessageType);
    }

    protected override void ConfigurePubSub(IRequestExecutorBuilder graphqlBuilder)
        => graphqlBuilder.AddInMemorySubscriptions();
}
