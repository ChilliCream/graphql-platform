using System.Net;
using System.Security.Claims;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

public class AuthorizationPolicyProviderTess(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task Policies_Are_Cached_If_PolicyProvider_Allows_Caching()
    {
        // arrange
        var policyProvider = new CustomAuthorizationPolicyProvider(allowsCaching: true);

        var server = CreateTestServer(
            builder =>
            {
                builder.Services.AddSingleton<IAuthorizationPolicyProvider>(_ => policyProvider);

                builder
                    .AddQueryType<Query>()
                    .AddAuthorization();
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                context.User = new ClaimsPrincipal(identity);
            });

        // act
        var result1 =
            await server.PostAsync(new ClientQueryRequest { Query = "{ bar }", });
        var result2 =
            await server.PostAsync(new ClientQueryRequest { Query = "{ bar }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result1.StatusCode);
        Assert.Null(result1.Errors);
        Assert.Equal(HttpStatusCode.OK, result2.StatusCode);
        Assert.Null(result2.Errors);
        Assert.Equal(1, policyProvider.InvocationsOfGetPolicyAsync);
    }

    [Fact]
    public async Task Policies_Are_Not_Cached_If_PolicyProvider_Disallows_Caching()
    {
        // arrange
        var policyProvider = new CustomAuthorizationPolicyProvider(allowsCaching: false);

        var server = CreateTestServer(
            builder =>
            {
                builder.Services.AddSingleton<IAuthorizationPolicyProvider>(_ => policyProvider);

                builder
                    .AddQueryType<Query>()
                    .AddAuthorization();
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                context.User = new ClaimsPrincipal(identity);
            });

        // act
        var result1 =
            await server.PostAsync(new ClientQueryRequest { Query = "{ bar }", });
        var result2 =
            await server.PostAsync(new ClientQueryRequest { Query = "{ bar }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result1.StatusCode);
        Assert.Null(result1.Errors);
        Assert.Equal(HttpStatusCode.OK, result2.StatusCode);
        Assert.Null(result2.Errors);
        Assert.Equal(2, policyProvider.InvocationsOfGetPolicyAsync);
    }

    public class Query
    {
        [HotChocolate.Authorization.Authorize(Policy = "policy")]
        public string Bar() => "bar";
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

    public class CustomAuthorizationPolicyProvider(bool allowsCaching) : IAuthorizationPolicyProvider
    {
        public int InvocationsOfGetPolicyAsync { get; private set; }

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            InvocationsOfGetPolicyAsync++;

            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new DenyAnonymousAuthorizationRequirement())
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => throw new NotImplementedException();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => throw new NotImplementedException();

        public virtual bool AllowsCachingPolicies => allowsCaching;
    }
}
