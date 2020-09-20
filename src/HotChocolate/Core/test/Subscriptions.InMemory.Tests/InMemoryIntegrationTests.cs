using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Subscriptions.InMemory
{
    public class InMemoryIntegrationTests
    {
        [Fact]
        public async Task SubscribeAndComplete()
        {
            // arrange
            IServiceProvider services = new ServiceCollection()
                .AddGraphQL()
                .AddInMemorySubscriptions()
                .AddQueryType(d => d
                    .Name("foo")
                    .Field("a")
                    .Resolver("b"))
                .AddSubscriptionType<Subscription>()
                .Services
                .BuildServiceProvider();

            var sender = services.GetRequiredService<ITopicEventSender>();
            var executorResolver = services.GetRequiredService<IRequestExecutorResolver>();
            IRequestExecutor executor = await executorResolver.GetRequestExecutorAsync();

            var cts = new CancellationTokenSource(10000);

            // act
            var result = (ISubscriptionResult)await executor.ExecuteAsync(
                "subscription { onMessage }",
                cts.Token);

            // assert
            await sender.SendAsync("OnMessage", "bar", cts.Token);
            await sender.CompleteAsync("OnMessage");

            await foreach (IQueryResult response in result.ReadResultsAsync()
                .WithCancellation(cts.Token))
            {
                Assert.Null(response.Errors);
                Assert.Equal("bar", response.Data!["onMessage"]);
            }

            await result.DisposeAsync();
        }

        public class FooType : InputObjectType
        {
            protected override void Configure(
                IInputObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Abc");
                descriptor.Field("def").Type<StringType>();
            }
        }

        public class Subscription
        {
            [Subscribe]
            public string OnMessage([EventMessage] string message) => message;
        }
    }
}
