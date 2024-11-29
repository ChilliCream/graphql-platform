using System.Net;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Opa.Native;

namespace HotChocolate.AspNetCore.Authorization;

public class AuthorizationTests : ServerTestBase, IAsyncLifetime
{
    private OpaHandle? _opaHandle;

    public AuthorizationTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    private static void SetUpHttpContext(HttpContext context)
    {
        var connection = context.Connection;
        connection.LocalIpAddress = IPAddress.Loopback;
        connection.LocalPort = 5555;
        connection.RemoteIpAddress = IPAddress.Loopback;
        connection.RemotePort = 7777;
    }

    public async Task InitializeAsync() => _opaHandle = await OpaProcess.StartServerAsync();

    [Theory(Skip = "The local server needs to be packaged with squadron")]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Policy_NotFound(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization();
            },
            SetUpHttpContext);

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ age }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory(Skip = "The local server needs to be packaged with squadron")]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Policy_NotAuthorized(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization();
            },
            SetUpHttpContext + (Action<HttpContext>)(c =>
            {
                c.Request.Headers["Authorization"] =
                    "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lI" +
                    "iwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            }));

        var hasAgeDefinedPolicy = await File.ReadAllTextAsync("policies/has_age_defined.rego");
        using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8181"), };

        var putPolicyResponse = await client.PutAsync(
            "/v1/policies/has_age_defined",
            new StringContent(hasAgeDefinedPolicy));
        putPolicyResponse.EnsureSuccessStatusCode();

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ age }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory(Skip = "The local server needs to be packaged with squadron")]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Policy_Authorized(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization();
            },
            SetUpHttpContext + (Action<HttpContext>)(c =>
            {
                c.Request.Headers["Authorization"] =
                    "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lI" +
                    "iwiaWF0IjoxNTE2MjM5MDIyLCJiaXJ0aGRhdGUiOiIxNy0xMS0yMDAwIn0.p88IUnrabPMh6LVi4DIYsDeZozjfj4Ofwg" +
                    "jXBglnxac";
            }));

        var hasAgeDefinedPolicy = await File.ReadAllTextAsync("policies/has_age_defined.rego");
        using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8181"), };

        var putPolicyResponse = await client.PutAsync(
            "/v1/policies/has_age_defined",
            new StringContent(hasAgeDefinedPolicy));
        putPolicyResponse.EnsureSuccessStatusCode();

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ age }", });

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
                        (context, _, _, _) =>
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

    public async Task DisposeAsync()
    {
        if (_opaHandle is not null)
        {
            await _opaHandle.DisposeAsync();
        }
    }
}
