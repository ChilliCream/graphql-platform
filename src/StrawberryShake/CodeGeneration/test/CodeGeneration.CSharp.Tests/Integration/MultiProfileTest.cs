using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StrawberryShake.Transport.WebSockets;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.MultiProfile
{
    public class MultiProfileTest : ServerTestBase
    {
        public MultiProfileTest(TestServerFactory serverFactory) : base(serverFactory)
        {
        }

        [Fact]
        public void Execute_MultiProfile_Test()
        {
            // arrange
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
            using IWebHost host = TestServerHelper.CreateServer(
                _ => { },
                out var port);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient(
                MultiProfileClient.ClientName,
                c => c.BaseAddress = new Uri("http://localhost:" + port + "/graphql"));
            serviceCollection.AddWebSocketClient(
                MultiProfileClient.ClientName,
                c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));


            // act
            serviceCollection.AddMultiProfileClient(
                profile: MultiProfileClientProfileKind.Default);

            IServiceProvider services = serviceCollection.BuildServiceProvider();
            MultiProfileClient client = services.GetRequiredService<MultiProfileClient>();

            // assert
            Assert.NotNull(client);
        }
    }
}
