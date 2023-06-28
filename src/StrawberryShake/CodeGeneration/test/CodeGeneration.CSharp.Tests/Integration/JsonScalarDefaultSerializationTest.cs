using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Types;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.JsonScalarDefaultSerialization;

public class JsonScalarDefaultSerializationTest : ServerTestBase
{
    public JsonScalarDefaultSerializationTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Fact]
    public async Task Execute_JsonScalarDefaultSerialization_Test()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddHttpClient();

        serviceCollection
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddType<JsonType>();

        serviceCollection
            .AddJsonScalarDefaultSerializationClient()
            .ConfigureInMemoryClient();

        var services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<JsonScalarDefaultSerializationClient>();

        // act
        var result = await client.GetJson.ExecuteAsync(ct);

        // assert
        result.MatchSnapshot();
    }

    public class Query
    {
        public JsonDocument GetJson1() => JsonDocument.Parse("[]");

        public JsonDocument GetJson2() => JsonDocument.Parse("abc");
    }
}
