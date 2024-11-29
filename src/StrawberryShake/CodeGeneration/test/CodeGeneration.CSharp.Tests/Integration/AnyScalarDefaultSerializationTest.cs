using HotChocolate;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using StrawberryShake.Transport.WebSockets;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.AnyScalarDefaultSerialization;

public class AnyScalarDefaultSerializationTest : ServerTestBase
{
    public AnyScalarDefaultSerializationTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Fact]
    public async Task Execute_AnyScalarDefaultSerialization_Test()
    {
        // arrange
        using var cts = new CancellationTokenSource(20_000);
        using var host = TestServerHelper.CreateServer(
            builder =>
            {
                builder.AddTypeExtension<QueryResolvers>();
            },
            out var port);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient(
            AnyScalarDefaultSerializationClient.ClientName,
            c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
        serviceCollection.AddWebSocketClient(
            AnyScalarDefaultSerializationClient.ClientName,
            c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
        serviceCollection.AddAnyScalarDefaultSerializationClient();
        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client =
            services.GetRequiredService<AnyScalarDefaultSerializationClient>();

        // act
        IOperationResult<IGetJsonResult> result = await client.GetJson.ExecuteAsync(cts.Token);

        // assert
        Assert.Empty(result.Errors);
        result.Data?.Json.ToString().MatchSnapshot();
    }

    [ExtendObjectType(OperationTypeNames.Query)]
    public class QueryResolvers
    {
        [GraphQLType(typeof(NonNullType<AnyType>))]
        public Dictionary<string, object> GetJson() => new() { { "abc", "def" }, };
    }
}
