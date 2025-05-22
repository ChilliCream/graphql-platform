using System.Net;
using System.Reflection;
using System.Security.Claims;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

public class AuthorizationRequirementDataTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task Authorized()
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                builder.Services.AddTransient<IAuthorizationHandler, CustomAuthorizationHandler>();
                builder
                    .AddQueryType<Query>()
                    .AddAuthorization();
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                identity.AddClaim(new Claim(
                    "foo",
                    "bar"));
                context.User = new ClaimsPrincipal(identity);
            });

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ foo }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.Errors);
    }

    [Fact]
    public async Task NotAuthorized()
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                builder.Services.AddTransient<IAuthorizationHandler, CustomAuthorizationHandler>();
                builder
                    .AddQueryType<Query>()
                    .AddAuthorization();
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                identity.AddClaim(new Claim(
                    "foo",
                    "foo"));
                context.User = new ClaimsPrincipal(identity);
            });

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ foo }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Multiple_Authorized()
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                builder.Services.AddTransient<IAuthorizationHandler, CustomAuthorizationHandler>();
                builder
                    .AddQueryType<Query>()
                    .AddAuthorization();
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                identity.AddClaim(new Claim(
                    "foo",
                    "bar"));
                identity.AddClaim(new Claim(
                    "bar",
                    "baz"));
                context.User = new ClaimsPrincipal(identity);
            });

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ fooMultiple }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.Errors);
    }

    [Fact]
    public async Task Multiple_NotAuthorized()
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                builder.Services.AddTransient<IAuthorizationHandler, CustomAuthorizationHandler>();
                builder
                    .AddQueryType<Query>()
                    .AddAuthorization();
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                identity.AddClaim(new Claim(
                    "foo",
                    "bar"));
                identity.AddClaim(new Claim(
                    "bar",
                    "bar"));
                context.User = new ClaimsPrincipal(identity);
            });

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ fooMultiple }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    public class Query
    {
        [CustomAuthorize("foo", "bar", Apply = HotChocolate.Authorization.ApplyPolicy.BeforeResolver)]
        public string? GetFoo() => "foo";

        [CustomAuthorize("foo", "bar", Apply = HotChocolate.Authorization.ApplyPolicy.BeforeResolver)]
        [CustomAuthorize("bar", "baz", Apply = HotChocolate.Authorization.ApplyPolicy.BeforeResolver)]
        public string? GetFooMultiple() => "foo";
    }

    private class CustomAuthorizeAttribute(string type, string value)
        : HotChocolate.Authorization.AuthorizeAttribute,
        IAuthorizationRequirement,
        IAuthorizationRequirementData
    {
        public string Type => type;

        public string Value => value;

        public IEnumerable<IAuthorizationRequirement> GetRequirements()
        {
            yield return this;
        }

        protected internal override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is IObjectTypeDescriptor type)
            {
                type.Directive(CreateDirective());
            }
            else if (descriptor is IObjectFieldDescriptor field)
            {
                field.Directive(CreateDirective());
            }
        }

        private HotChocolate.Authorization.AuthorizeDirective CreateDirective()
        {
            return new HotChocolate.Authorization.AuthorizeDirective(metadata: [.. GetRequirements()]);
        }
    }

    private class CustomAuthorizationHandler : AuthorizationHandler<CustomAuthorizeAttribute>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            CustomAuthorizeAttribute requirement)
        {
            if (context.User.HasClaim(requirement.Type, requirement.Value))
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
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
