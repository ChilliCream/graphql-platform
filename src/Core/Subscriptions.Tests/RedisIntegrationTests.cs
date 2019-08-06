using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;
using HotChocolate.Types;
using HotChocolate.Execution;

namespace HotChocolate.Subscriptions.Redis
{
    public class RedisIntegrationTests
    {
        private readonly IEventSender _sender;
        private readonly ConfigurationOptions _configuration;

        public RedisIntegrationTests()
        {
            string endpoint =
                Environment.GetEnvironmentVariable("REDIS_ENDPOINT")
                ?? "localhost:6379";

            string password =
                Environment.GetEnvironmentVariable("REDIS_PASSWORD");

            _configuration = new ConfigurationOptions
            {
                Ssl = !string.IsNullOrEmpty(password),
                AbortOnConnectFail = false,
                Password = password
            };

            _configuration.EndPoints.Add(endpoint);

            var redisEventRegistry = new RedisEventRegistry(
                ConnectionMultiplexer.Connect(_configuration),
                new JsonPayloadSerializer());

            _sender = redisEventRegistry;
        }

        [Fact]
        public async Task Subscribe()
        {
            // arrange
            var services = new ServiceCollection();
            services.AddRedisSubscriptionProvider(_configuration);

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

            // act
            IExecutionResult result = executor.Execute(
                "subscription { " + name + " }");
            var outgoing = new EventMessage(eventDescription, "bar");
            await _sender.SendAsync(outgoing);

            // assert
            var stream = (IResponseStream)result;
            IReadOnlyQueryResult message = await stream.ReadAsync(cts.Token);
            Assert.Equal("baz", message.Data.First().Value);
            stream.Dispose();
        }
    }
}
