using System.Net;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Authorization;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

public class AuthorizationTests : ServerTestBase, IAsyncLifetime
{
    private OpaProcess? _opaHandle;

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

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Policy_NotFound(Action<IRequestExecutorBuilder, int> configure)
    {
        // arrange
        var port = _opaHandle!.GetPort();
        var server = CreateTestServer(
            builder =>
            {
                configure(builder, port);
                builder.Services.AddAuthorization();
            },
            SetUpHttpContext);

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ age }" });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Policy_NotAuthorized(Action<IRequestExecutorBuilder, int> configure)
    {
        // arrange
        var port = _opaHandle!.GetPort();
        var server = CreateTestServer(
            builder =>
            {
                configure(builder, port);
                builder.Services.AddAuthorization();
            },
            SetUpHttpContext + (Action<HttpContext>)(c =>
            {
                c.Request.Headers["Authorization"] =
                    "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lI"
                    + "iwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            }));

        var hasAgeDefinedPolicy = await File.ReadAllTextAsync("Policies/has_age_defined.rego");
        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };

        var putPolicyResponse = await client.PutAsync(
            "/v1/policies/has_age_defined",
            new StringContent(hasAgeDefinedPolicy));
        putPolicyResponse.EnsureSuccessStatusCode();

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ age }" });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Policy_Authorized(Action<IRequestExecutorBuilder, int> configure)
    {
        // arrange
        var port = _opaHandle!.GetPort();
        var server = CreateTestServer(
            builder =>
            {
                configure(builder, port);
                builder.Services.AddAuthorization();
            },
            SetUpHttpContext + (Action<HttpContext>)(c =>
            {
                c.Request.Headers["Authorization"] =
                    "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lI"
                    + "iwiaWF0IjoxNTE2MjM5MDIyLCJiaXJ0aGRhdGUiOiIxNy0xMS0yMDAwIn0.p88IUnrabPMh6LVi4DIYsDeZozjfj4Ofwg"
                    + "jXBglnxac";
            }));

        var hasAgeDefinedPolicy = await File.ReadAllTextAsync("Policies/has_age_defined.rego");
        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };

        var putPolicyResponse = await client.PutAsync(
            "/v1/policies/has_age_defined",
            new StringContent(hasAgeDefinedPolicy));
        putPolicyResponse.EnsureSuccessStatusCode();

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ age }" });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Policy_Authorized_WithExtensions(Action<IRequestExecutorBuilder, int> configure)
    {
        // arrange
        var port = _opaHandle!.GetPort();
        var server = CreateTestServer(
            builder =>
            {
                configure(builder, port);
                builder.Services.AddAuthorization();
                builder.AddOpaQueryRequestExtensionsHandler(Policies.HasDefinedAge,
                    context => context.Resource is IMiddlewareContext or AuthorizationContext
                        ? new Dictionary<string, string> { { "secret", "secret" } }
                        : null);
            },
            SetUpHttpContext + (Action<HttpContext>)(c =>
            {
                // The token is the same but swapped alg and typ,
                // as a result Base64 representation is not the one as expected by Rego rule
                // See policies/has_age_defined.rego file for details
                c.Request.Headers["Authorization"] =
                    "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9."
                    + "eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJiaXJ0aGRhdGUiOiIxNy0x"
                    + "MS0yMDAwIn0.01Hb6X-HXl9ASf3X82Mt63RMpZ4SVJZT9hTI2dYet-k";
            }));

        var hasAgeDefinedPolicy = await File.ReadAllTextAsync("Policies/has_age_defined.rego");
        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };

        var putPolicyResponse = await client.PutAsync(
            "/v1/policies/has_age_defined",
            new StringContent(hasAgeDefinedPolicy));
        putPolicyResponse.EnsureSuccessStatusCode();

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ age }" });

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
