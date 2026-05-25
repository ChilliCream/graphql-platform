using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.UnsignedTypes;

public class UnsignedTypesTest : ServerTestBase
{
    public UnsignedTypesTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Fact]
    public async Task Execute_UnsignedTypes_Test()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(
            builder => builder.AddTypeExtension(typeof(QueryType)),
            out var port);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient(
            UnsignedTypesClient.ClientName,
            c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
        serviceCollection.AddWebSocketClient(
            UnsignedTypesClient.ClientName,
            c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
        serviceCollection.AddUnsignedTypesClient();
        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<UnsignedTypesClient>();

        // act
        var result = await client.UnsignedTypes.ExecuteAsync(ct);

        // assert
        Assert.Equal(1, result.Data!.UnsignedByte);
        Assert.Equal(256, result.Data!.UnsignedShort);
        Assert.Equal(65536U, result.Data!.UnsignedInt);
        Assert.Equal(4294967296UL, result.Data!.UnsignedLong);
    }

    [QueryType]
    private static class QueryType
    {
        public static byte GetUnsignedByte(byte input) => input;

        public static ushort GetUnsignedShort(ushort input) => input;

        public static uint GetUnsignedInt(uint input) => input;

        public static ulong GetUnsignedLong(ulong input) => input;
    }
}
