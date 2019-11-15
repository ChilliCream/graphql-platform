using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using StrawberryShake.Http.Subscriptions;
using StrawberryShake.Http.Subscriptions.Messages;
using StrawberryShake.Serializers;
using StrawberryShake.Transport;
using StrawberryShake.Transport.WebSockets;
using Xunit;

namespace StrawberryShake.Http
{
    public class Foo
    {
        [Fact]
        public async Task Bar()
        {
            using (IWebHost host = TestServerHelper.CreateServer(out int port))
            {
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddWebSocketClient(
                    "Foo",
                    c => c.Uri = new Uri("ws://localhost:" + port));
                serviceCollection.AddWebSocketConnectionPool();

                IServiceProvider services =
                    serviceCollection.BuildServiceProvider();
                ISocketConnectionPool connectionPool =
                    services.GetRequiredService<ISocketConnectionPool>();
                ISocketConnection connection =
                    await connectionPool.RentAsync("Foo");

                var valueSerializerResolver = new ValueSerializerResolver(
                    ValueSerializers.All);
                var operationFormater = new JsonOperationFormatter(
                    valueSerializerResolver);
                var subscriptionManager = new SubscriptionManager(
                    operationFormater);

                var messagePipeline = new MessagePipeline(
                    connection,
                    subscriptionManager,
                    new IMessageHandler[]
                    {
                        new DataResultMessageHandler(subscriptionManager)
                    });

                var operation = new Operation();
                var resultParser = new OnReviewResultParser();
                var subscription = new Subscription<OnReview>(operation, resultParser);

                await subscriptionManager.RegisterAsync(subscription, connection);

                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    new Uri("http://localhost:" + port));
                request.Content = new StringContent(
                    "{ \"query\" : \"mutation { createReview(episode: " +
                    "NEWHOPE, review: { stars: 5 }) { stars } }\" }",
                    Encoding.UTF8,
                    "application/json");

                var client = new HttpClient();
                client.BaseAddress = new Uri("http://localhost:" + port);
                await client.SendAsync(request);

                try
                {
                    messagePipeline.Start();

                    using (var cts = new CancellationTokenSource(10000))
                    {
                        await using (IAsyncEnumerator<IOperationResult<OnReview>> enumerator =
                            subscription.GetAsyncEnumerator(CancellationToken.None))
                        {
                            await enumerator.MoveNextAsync();
                            enumerator.Current.MatchSnapshot(o => o.IgnoreField("ResultType"));
                        }
                    }
                }
                finally
                {
                    await messagePipeline.DisposeAsync();
                    await connection.CloseAsync(
                        "Ciao",
                        SocketCloseStatus.NormalClosure);
                }
            }
        }

        private class Operation
            : IOperation
        {
            public string Name => "abc";

            public IDocument Document { get; } = new Document();

            public OperationKind Kind { get; } = OperationKind.Subscription;

            public Type ResultType => typeof(OnReview);

            public IReadOnlyList<VariableValue> GetVariableValues() =>
                Array.Empty<VariableValue>();
        }

        private class Document
            : IDocument
        {
            public ReadOnlySpan<byte> HashName =>
                throw new NotSupportedException();

            public ReadOnlySpan<byte> Hash => Encoding.UTF8.GetBytes("ABC");

            public ReadOnlySpan<byte> Content => Encoding.UTF8.GetBytes(
                "subscription abc { onReview(episode: NEWHOPE) { stars } }");
        }

        public class OnReview
        {
            public int Stars { get; set; }
        }

        public class OnReviewResultParser
            : JsonResultParserBase<OnReview>
        {
            protected override OnReview ParserData(JsonElement parent)
            {
                JsonElement onReview = parent.GetProperty("onReview");

                return new OnReview
                {
                    Stars = onReview.GetProperty("stars").GetInt32()
                };
            }
        }
    }
}
