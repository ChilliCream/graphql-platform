using System.Net;
using System.Security.Claims;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.AspNetCore.Authorization;

public class AuthorizationTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Authorize_WithoutArgs_NoClaimsIdentity_NotAuthenticated(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization();
            },
            context => context.User = new ClaimsPrincipal());

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ default }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Authorize_WithoutArgs_HasClaimsIdentity_Authenticated(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization();
            },
            context => context.User = new ClaimsPrincipal(new ClaimsIdentity("abc")));

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ default }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task DefaultPolicy_Allow_Anonymous(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var policyBuilder = new AuthorizationPolicyBuilder();
        policyBuilder.RequireAssertion(_ => true);

        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization(
                    options => options.DefaultPolicy = policyBuilder.Build());
            },
            context => context.User = new ClaimsPrincipal());

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ default }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task DefaultPolicy_Disallow_Anonymous(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var policyBuilder = new AuthorizationPolicyBuilder();
        policyBuilder.RequireAssertion(_ => false);

        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization(
                    options => options.DefaultPolicy = policyBuilder.Build());
            },
            context => context.User = new ClaimsPrincipal());

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ default }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task AuthServiceIsAlwaysAdded(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
            },
            context =>
            {
                context.User = new ClaimsPrincipal(
                    new ClaimsIdentity("testauth"));
            });

        // ac
        var result =
            await server.PostAsync(new ClientQueryRequest { Query = "{ age }", });

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "ContentType": "application/graphql-response+json; charset=utf-8",
              "StatusCode": "OK",
              "Data": {
                "age": null
              },
              "Errors": [
                {
                  "message": "The `HasAgeDefined` authorization policy does not exist.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 3
                    }
                  ],
                  "path": [
                    "age"
                  ],
                  "extensions": {
                    "code": "AUTH_POLICY_NOT_FOUND"
                  }
                }
              ],
              "Extensions": null
            }
            """);
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Policy_NotFound(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("FooBar", policy =>
                        policy.RequireAssertion(context =>
                            context.User.HasClaim(c =>
                                c.Type == ClaimTypes.DateOfBirth)));
                });
            },
            context =>
            {
                context.User = new ClaimsPrincipal(
                    new ClaimsIdentity("testauth"));
            });

        // ac
        var result =
            await server.PostAsync(new ClientQueryRequest { Query = "{ age }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Policy_NotAuthorized(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
           builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("HasAgeDefined", policy =>
                        policy.RequireAssertion(context =>
                            context.User.HasClaim(c =>
                                c.Type == ClaimTypes.DateOfBirth)));
                });
            },
            context =>
            {
                context.User = new ClaimsPrincipal(
                    new ClaimsIdentity("testauth"));
            });

        // act
        var result =
            await server.PostAsync(new ClientQueryRequest { Query = "{ age }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Policy_Resources_Is_IResolverContext(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("HasAgeDefined", policy =>
                        policy.RequireAssertion(context =>
                            context.Resource is IResolverContext));
                });
            },
            context =>
            {
                context.User = new ClaimsPrincipal(
                    new ClaimsIdentity("testauth"));
            });

        // act
        var result =
            await server.PostAsync(new ClientQueryRequest { Query = "{ age }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Policy_Authorized(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("HasAgeDefined", policy =>
                        policy.RequireAssertion(context =>
                            context.User.HasClaim(c =>
                                c.Type == ClaimTypes.DateOfBirth)));
                });
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                identity.AddClaim(new Claim(
                    ClaimTypes.DateOfBirth,
                    "2013-05-30"));
                context.User = new ClaimsPrincipal(identity);
            });

        // act
        var result =
            await server.PostAsync(new ClientQueryRequest { Query = "{ age }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Roles_And_Policy_UserNeitherHasRoleOrMatchesPolicy_NotAuthorized(
        Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("HasAgeDefined", policy =>
                        policy.RequireAssertion(context =>
                            context.User.HasClaim(c =>
                                c.Type == ClaimTypes.DateOfBirth)));
                });
            },
            context =>
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity("testauth"));
            });

        // act
        var result =
            await server.PostAsync(new ClientQueryRequest { Query = "{ rolesAndPolicy }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Roles_And_Policy_UserHasOneOfTheRolesAndMatchesPolicy_Authorized(
        Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("HasAgeDefined", policy =>
                        policy.RequireAssertion(context =>
                            context.User.HasClaim(c =>
                                c.Type == ClaimTypes.DateOfBirth)));
                });
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                identity.AddClaim(new Claim(
                    ClaimTypes.DateOfBirth,
                    "2013-05-30"));
                identity.AddClaim(new Claim(ClaimTypes.Role, "a"));
                context.User = new ClaimsPrincipal(identity);
            });

        // act
        var result =
            await server.PostAsync(new ClientQueryRequest { Query = "{ rolesAndPolicy }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Roles_And_Policy_UserHasOneOfTheRolesAndMissesPolicy_NotAuthorized(
        Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("HasAgeDefined", policy =>
                        policy.RequireAssertion(context =>
                            context.User.HasClaim(c =>
                                c.Type == ClaimTypes.DateOfBirth)));
                });
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                identity.AddClaim(new Claim(ClaimTypes.Role, "a"));
                context.User = new ClaimsPrincipal(identity);
            });

        // act
        var result =
            await server.PostAsync(new ClientQueryRequest { Query = "{ rolesAndPolicy }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Roles_And_Policy_UserMatchesPolicyButIsntInOneOfTheRoles_NotAuthorized(
        Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("HasAgeDefined", policy =>
                        policy.RequireAssertion(context =>
                            context.User.HasClaim(c =>
                                c.Type == ClaimTypes.DateOfBirth)));
                });
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                identity.AddClaim(new Claim(
                    ClaimTypes.DateOfBirth,
                    "2013-05-30"));
                context.User = new ClaimsPrincipal(identity);
            });

        // act
        var result =
            await server.PostAsync(new ClientQueryRequest { Query = "{ rolesAndPolicy }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task AuthorizationService_CalledWith_RolesAuthorizationRequirement(
        Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);

                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("HasAgeDefined", policy =>
                        policy.RequireAssertion(context =>
                            context.User.HasClaim(c =>
                                c.Type == ClaimTypes.DateOfBirth)));
                });

                builder.Services.RemoveAll<IAuthorizationService>();
                builder.Services.AddTransient<DefaultAuthorizationService>();
                builder.Services.AddTransient<IAuthorizationService, FallbackRoleAuthorizationService>();
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");

                identity.AddClaim(new Claim(
                    ClaimTypes.DateOfBirth,
                    "2013-05-30"));
                identity.AddClaim(new Claim(ClaimTypes.Role, "a"));
                context.User = new ClaimsPrincipal(identity);
            });

        // act
        var result =
            await server.PostAsync(new ClientQueryRequest { Query = "{ rolesAndPolicy }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Roles_UserHasNoRoles_NotAuthorized(
        Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                builder.Services.AddAuthorizationCore();
                configure(builder);
            },
            context =>
            {
                context.User = new ClaimsPrincipal(
                    new ClaimsIdentity("testauth"));
            });

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ roles }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Roles_UserHasDifferentRoles_NotAuthorized(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                builder.Services.AddAuthorizationCore();
                configure(builder);
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                identity.AddClaim(new Claim(
                    ClaimTypes.Role,
                    "b"));
                context.User.AddIdentity(identity);
            });

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ roles }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Roles_UserHasNoneOfTheRoles_NotAuthorized(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                builder.Services.AddAuthorizationCore();
                configure(builder);
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                identity.AddClaim(new Claim(
                    ClaimTypes.Role,
                    "c"));
                context.User.AddIdentity(identity);
            });

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ roles_ab }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Roles_UserHasAllOfTheRoles_Authorized(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                builder.Services.AddAuthorizationCore();
                configure(builder);
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                identity.AddClaim(new Claim(
                    ClaimTypes.Role,
                    "a"));
                identity.AddClaim(new Claim(
                    ClaimTypes.Role,
                    "b"));
                context.User = new ClaimsPrincipal(identity);
            });

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ roles_ab }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Roles_UserHasOneOfTheRoles_Authorized(
        Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                builder.Services.AddAuthorizationCore();
                configure(builder);
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                identity.AddClaim(new Claim(
                    ClaimTypes.Role,
                    "a"));
                context.User = new ClaimsPrincipal(identity);
            });

        // act
        var result =
            await server.PostAsync(new ClientQueryRequest { Query = "{ roles_ab }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Roles_Authorized(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                builder.Services.AddAuthorizationCore();
                configure(builder);
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                identity.AddClaim(new Claim(ClaimTypes.Role, "a"));
                context.User = new ClaimsPrincipal(identity);
            });

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ roles }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task PipedAuthorizeDirectives_Authorized(
        Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("a", policy =>
                        policy.RequireAssertion(context =>
                            context.User.HasClaim(c =>
                                c.Type == ClaimTypes.DateOfBirth)));

                    options.AddPolicy("b", policy =>
                        policy.RequireAssertion(context =>
                            context.User.HasClaim(c =>
                                c.Type == ClaimTypes.Country)));
                });
            },
            context =>
            {
                var identity = new ClaimsIdentity("testAuth");
                identity.AddClaim(new Claim(
                    ClaimTypes.DateOfBirth,
                    "2013-05-30"));
                identity.AddClaim(new Claim(
                    ClaimTypes.Country,
                    "US"));
                context.User = new ClaimsPrincipal(identity);
            });

        // act
        var result = await server.PostAsync(new ClientQueryRequest { Query = "{ piped }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task PipedAuthorizeDirectives_SecondFails_NotAuthorized(
        Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("a", policy =>
                        policy.RequireAssertion(context =>
                            context.User.HasClaim(c =>
                                c.Type == ClaimTypes.DateOfBirth)));

                    options.AddPolicy("b", policy =>
                        policy.RequireAssertion(context =>
                            context.User.HasClaim(c =>
                                c.Type == ClaimTypes.Country)));
                });
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                identity.AddClaim(new Claim(
                    ClaimTypes.DateOfBirth,
                    "2013-05-30"));
                context.User.AddIdentity(identity);
            });

        // act
        var result =
            await server.PostAsync(new ClientQueryRequest { Query = "{ piped }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Policy_Is_Executed_After_Resolver_User_Is_Allowed(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("a", policy =>
                        policy.RequireAssertion(context =>
                        {
                            if (context.Resource is IMiddlewareContext m
                                && m.Result is string s
                                && s == "foo")
                            {
                                return true;
                            }
                            return false;
                        }));
                });
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                identity.AddClaim(new Claim(
                    ClaimTypes.DateOfBirth,
                    "2013-05-30"));
                context.User.AddIdentity(identity);
            });

        // act
        var result =
            await server.PostAsync(new ClientQueryRequest { Query = "{ afterResolver }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Theory]
    [ClassData(typeof(AuthorizationTestData))]
    [ClassData(typeof(AuthorizationAttributeTestData))]
    public async Task Policy_Is_Executed_After_Resolver_User_Is_Denied(Action<IRequestExecutorBuilder> configure)
    {
        // arrange
        var server = CreateTestServer(
            builder =>
            {
                configure(builder);
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("a", policy =>
                        policy.RequireAssertion(context =>
                        {
                            if (context.Resource is IMiddlewareContext m
                                && m.Result is string s
                                && s == "bar")
                            {
                                return true;
                            }
                            return false;
                        }));
                });
            },
            context =>
            {
                var identity = new ClaimsIdentity("testauth");
                identity.AddClaim(new Claim(
                    ClaimTypes.DateOfBirth,
                    "2013-05-30"));
                context.User.AddIdentity(identity);
            });

        // act
        var result =
            await server.PostAsync(new ClientQueryRequest { Query = "{ afterResolver }", });

        // assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.MatchSnapshot();
    }

    [Fact]
    public void AddAuthorizeDirectiveType_SchemaBuilderIsNull_ArgNullExec()
    {
        // arrange
        // act
        static void Action() =>
            AuthorizeSchemaBuilderExtensions
                .AddAuthorizeDirectiveType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
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

    private sealed class FallbackRoleAuthorizationService(DefaultAuthorizationService defaultAuthorizationService) : IAuthorizationService
    {
        public async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
        {
            var effectiveRequirements = requirements.ToList();

            if (!effectiveRequirements.OfType<RolesAuthorizationRequirement>().Any()) {
                effectiveRequirements.Add(new RolesAuthorizationRequirement(allowedRoles: ["b"]));
            }

            return await defaultAuthorizationService.AuthorizeAsync(user, resource, effectiveRequirements);
        }

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName) {
            throw new NotImplementedException();
        }
    }
}
