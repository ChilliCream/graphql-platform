using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Types;
using Squadron;
using StackExchange.Redis;
using Xunit;

namespace HotChocolate.Subscriptions.Redis
{
    public class RedisIntegrationTests
        : IClassFixture<RedisResource>
    {
        private readonly ConnectionMultiplexer _connection;
        private readonly ITopicEventSender _sender;

        public RedisIntegrationTests(RedisResource redisResource)
        {
            _connection = redisResource.GetConnection();
            _sender = new RedisPubSub(_connection, new JsonMessageSerializer());
        }

        [Fact]
        public Task Subscribe()
        {
            return TestHelper.TryTest(async () =>
            {
                // arrange
                var services = new ServiceCollection();
                services.AddRedisSubscriptions(_connection);

                IServiceProvider serviceProvider = services.BuildServiceProvider();

                var cts = new CancellationTokenSource(30000);
                string name = "field_" + Guid.NewGuid().ToString("N");
                IQueryExecutor executor = SchemaBuilder.New()
                    .AddServices(serviceProvider)
                    .AddQueryType(d => d
                        .Name("foo")
                        .Field("a")
                        .Resolver("b"))
                    .AddSubscriptionType(d => d
                        .Name("bar")
                        .Field(name)
                        .Resolver("baz")
                        .Subscribe(async ctx =>
                            await ctx.Service<ITopicEventReceiver>()
                                .SubscribeAsync<string, string>("foo", ctx.RequestAborted)))
                    .Create()
                    .MakeExecutable();

                IExecutionResult result = executor.Execute(
                    "subscription { " + name + " }");

                // act
                await _sender.SendAsync("foo", "bar");

                // assert
                var stream = (IResponseStream)result;
                IReadOnlyQueryResult message = null;
                await foreach (IReadOnlyQueryResult item in stream.WithCancellation(cts.Token))
                {
                    message = item;
                    break;
                }

                Assert.Equal("baz", message.Data.First().Value);
            });
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
    }
}
