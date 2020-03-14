using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using Xunit;
using HotChocolate.Types;

namespace HotChocolate.Subscriptions
{
    public class InMemorySubscriptionTests
    {
        [Fact]
        public async Task Subscribe_RaiseEvent_No_Arguments()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInMemorySubscriptions();
            IServiceProvider services = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryRoot>()
                .AddMutationType<MutationRoot>()
                .AddSubscriptionType<SubscriptionRoot>()
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            var responseStream =
                await executor.ExecuteAsync(
                        QueryRequestBuilder.New()
                            .SetQuery("subscription { onFoo }")
                            .SetServices(services)
                            .Create())
                as IResponseStream;

            // act
            await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("mutation { foo(a: \"bar\") }")
                    .SetServices(services)
                    .Create());

            // assert
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                await foreach (IReadOnlyQueryResult result in
                    responseStream.WithCancellation(cts.Token))
                {
                    Assert.Equal("bar", result.Data["onFoo"]);
                    break;
                }
            }
        }

        [Fact]
        public async Task Subscribe_RaiseEvent_With_Argument()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInMemorySubscriptions();
            IServiceProvider services = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryRoot>()
                .AddMutationType<MutationRoot>()
                .AddSubscriptionType<SubscriptionRoot>()
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            var responseStream =
                await executor.ExecuteAsync(
                        QueryRequestBuilder.New()
                            .SetQuery("subscription { onBar(topic: \"foo\") }")
                            .SetServices(services)
                            .Create())
                as IResponseStream;

            // act
            await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("mutation { foo(a: \"bar\") }")
                    .SetServices(services)
                    .Create());

            // assert
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                await foreach (IReadOnlyQueryResult result in
                    responseStream.WithCancellation(cts.Token))
                {
                    Assert.Equal("bar", result.Data["onBar"]);
                    break;
                }
            }
        }

        [Fact]
        public async Task Subscribe_RaiseEvent_With_Argument_As_Variables()
        {
            // arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInMemorySubscriptions();
            IServiceProvider services = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryRoot>()
                .AddMutationType<MutationRoot>()
                .AddSubscriptionType<SubscriptionRoot>()
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            var responseStream =
                await executor.ExecuteAsync(
                        QueryRequestBuilder.New()
                            .SetQuery("subscription($a: String!) { onBar(topic: $a) }")
                            .AddVariableValue("a", "foo")
                            .SetServices(services)
                            .Create())
                as IResponseStream;

            // act
            await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("mutation { foo(a: \"bar\") }")
                    .SetServices(services)
                    .Create());

            // assert
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                await foreach (IReadOnlyQueryResult result in
                    responseStream.WithCancellation(cts.Token))
                {
                    Assert.Equal("bar", result.Data["onBar"]);
                    break;
                }
            }
        }

        public class QueryRoot
        {
            public string Foo { get; set; }
        }

        public class MutationRoot
        {
            public async Task<string> FooAsync(
                string a,
                [Service]ITopicEventSender eventDispatcher,
                CancellationToken cancellationToken)
            {
                await eventDispatcher.SendAsync("foo", a);
                return "done";
            }
        }

        public class SubscriptionRoot
        {
            [SubscribeAndResolve]
            public async Task<IAsyncEnumerable<string>> OnFooAsync(
                [Service]ITopicEventReceiver eventTopicObserver,
                CancellationToken cancellationToken)
            {
                return await eventTopicObserver.SubscribeAsync<string, string>(
                    "foo", cancellationToken);
            }

            [SubscribeAndResolve]
            public async Task<IAsyncEnumerable<string>> OnBarAsync(
                string topic,
                [Service]ITopicEventReceiver eventTopicObserver,
                CancellationToken cancellationToken)
            {
                return await eventTopicObserver.SubscribeAsync<string, string>(
                    topic, cancellationToken);
            }
        }
    }
}
