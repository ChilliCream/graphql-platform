using HotChocolate;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.LocalTypes;

public class LocalTypesTest : ServerTestBase
{
    public LocalTypesTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Fact]
    public async Task Execute_LocalTypes_Test()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(
            builder => builder.AddTypeExtension(typeof(QueryType)),
            out var port);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient(
            LocalTypesClient.ClientName,
            c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
        serviceCollection.AddWebSocketClient(
            LocalTypesClient.ClientName,
            c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
        serviceCollection.AddLocalTypesClient();
        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<LocalTypesClient>();

        // act
        var result = await client.LocalTypes.ExecuteAsync(ct);

        // assert
        Assert.Equal(new DateOnly(2021, 10, 10), result.Data!.LocalDate);
        Assert.Equal(new DateTime(2021, 10, 10, 10, 10, 10), result.Data!.LocalDateTime);
        Assert.Equal(new TimeOnly(10, 10, 10), result.Data!.LocalTime);
    }

    [QueryType]
    private static class QueryType
    {
        public static DateOnly GetLocalDate(DateOnly input) => input;

        [GraphQLType<LocalDateTimeType>]
        public static DateTime GetLocalDateTime([GraphQLType<LocalDateTimeType>] DateTime input)
            => input;

        public static TimeOnly GetLocalTime(TimeOnly input) => input;
    }
}
