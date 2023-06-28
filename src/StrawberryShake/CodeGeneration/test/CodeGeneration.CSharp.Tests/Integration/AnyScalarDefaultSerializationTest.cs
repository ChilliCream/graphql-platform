using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;
using Xunit;

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
        CancellationToken ct = new CancellationTokenSource(20_000).Token;
        using IWebHost host = TestServerHelper.CreateServer(
            _ => { },
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
        AnyScalarDefaultSerializationClient client = services.GetRequiredService<AnyScalarDefaultSerializationClient>();

        // act


        // assert

    }
}
