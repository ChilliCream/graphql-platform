using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;
using HotChocolate.Types;
using HotChocolate.Execution;
using HotChocolate.Language;
using Squadron;

namespace HotChocolate.Subscriptions.Redis
{
    public class RedisIntegrationTests
        : IClassFixture<RedisResource>
    {
        private readonly ConnectionMultiplexer _connection;
        private readonly IEventSender _sender;

        public RedisIntegrationTests(RedisResource redisResource)
        {
            _connection = redisResource.GetConnection();
            _sender = new RedisEventRegistry(
                _connection,
                new JsonPayloadSerializer());
        }

        [Fact]
        public Task Subscribe()
        {
            return TestHelper.TryTest(async () =>
            {
                // arrange
                var services = new ServiceCollection();
                services.AddRedisSubscriptionProvider(_connection);

                IServiceProvider serviceProvider = services.BuildServiceProvider();

                var cts = new CancellationTokenSource(30000);
                string name = "field_" + Guid.NewGuid().ToString("N");
                IQueryExecutor executor = SchemaBuilder.New()
                    .AddServices(serviceProvider)
                    .AddQueryType(d => d
                        .Name("foo")
                        .Field("a")
                        .Resolver("b"))
                    .AddSubscriptionType(d => d.Name("bar")
                        .Field(name)
                        .Resolver("baz"))
                    .Create()
                    .MakeExecutable();

                var eventDescription = new EventDescription(name);
                var outgoing = new EventMessage(eventDescription, "bar");

                IExecutionResult result = executor.Execute(
                    "subscription { " + name + " }");

                // act
                await _sender.SendAsync(outgoing);

                // assert
                var stream = (IResponseStream)result;
                IReadOnlyQueryResult message = await stream.ReadAsync(cts.Token);
                Assert.Equal("baz", message.Data.First().Value);
                stream.Dispose();
            });
        }

        [Fact]
        public Task Subscribe_With_ObjectValue()
        {
            return TestHelper.TryTest(async () =>
            {
                // arrange
                var services = new ServiceCollection();
                services.AddRedisSubscriptionProvider(_connection);

                IServiceProvider serviceProvider = services.BuildServiceProvider();

                var cts = new CancellationTokenSource(30000);
                string name = "field_" + Guid.NewGuid().ToString("N");
                IQueryExecutor executor = SchemaBuilder.New()
                    .AddServices(serviceProvider)
                    .AddQueryType(d => d
                        .Name("foo")
                        .Field("a")
                        .Resolver("b"))
                    .AddSubscriptionType(d => d.Name("bar")
                        .Field(name)
                        .Argument("a", a => a.Type<FooType>())
                        .Resolver("baz"))
                    .Create()
                    .MakeExecutable();

                var eventDescription = new EventDescription(name,
                    new ArgumentNode("a",
                        new ObjectValueNode(
                            new ObjectFieldNode("def", "xyz"))));
                var outgoing = new EventMessage(eventDescription, "bar");

                IExecutionResult result = executor.Execute(
                    "subscription { " + name + "(a: { def: \"xyz\" }) }");

                // act
                await _sender.SendAsync(outgoing);

                // assert
                var stream = (IResponseStream)result;
                IReadOnlyQueryResult message = await stream.ReadAsync(cts.Token);
                Assert.Equal("baz", message.Data.First().Value);
                stream.Dispose();
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
