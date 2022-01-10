using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Opa.Native;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore.Authorization;

public class AuthorizationTests : ServerTestBase
{
    public AuthorizationTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task DefaultPolicy_NotFound(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        TestServer server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization();
            },
            context =>
            {
                ConnectionInfo connection = context.Request.HttpContext.Connection;
                connection.LocalIpAddress = IPAddress.Loopback;
                connection.LocalPort = 5555;
                connection.RemoteIpAddress = IPAddress.Loopback;
                connection.RemotePort = 7777;
            });

        await using OpaHandle h = await OpaProcess.StartServerAsync();

        // act
        ClientQueryResult result =
            await server.PostAsync(new ClientQueryRequest { Query = "{ default }" });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    private TestServer CreateTestServer(
        Action<IRequestExecutorBuilder> build,
        Action<HttpContext> configureUser)
    {
        return ServerFactory.Create(
            services =>
            {
                build(services
                    .AddRouting()
                    .AddGraphQLServer()
                    .AddHttpRequestInterceptor(
                        (context, requestExecutor, requestBuilder, cancellationToken) =>
                        {
                            configureUser(context);
                            return default;
                        }));
            },
            app =>
            {
                app.UseRouting();
                app.UseEndpoints(b => b.MapGraphQL());
            });
    }
}
