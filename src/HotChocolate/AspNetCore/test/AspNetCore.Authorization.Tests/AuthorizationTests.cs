using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore.Authorization
{
    public class AuthorizationTests
        : ServerTestBase
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
                    builder.Services.AddAuthorization(options =>
                    {
                        options.DefaultPolicy = null;
                    });
                },
                context =>
                {
                    context.User = new ClaimsPrincipal(
                        new ClaimsIdentity("testauth"));
                });

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest { Query = "{ default }" });

            // assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            result.MatchSnapshot();
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task NoAuthServices_Authenticated_True(Action<IRequestExecutorBuilder> configure)
        {
            // arrange
            TestServer server = CreateTestServer(
                builder =>
                {
                    configure(builder);
                },
                context =>
                {
                    context.User = new ClaimsPrincipal(
                        new ClaimsIdentity("testauth"));
                });

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest { Query = "{ default }" });

            // assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            result.MatchSnapshot();
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task NoAuthServices_Authenticated_False(Action<IRequestExecutorBuilder> configure)
        {
            // arrange
            TestServer server = CreateTestServer(
                builder =>
                {
                    configure(builder);
                },
                context =>
                {
                    context.User = new ClaimsPrincipal(
                        new ClaimsIdentity());
                });

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest { Query = "{ default }" });

            // assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            result.MatchSnapshot();
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task Policy_NotFound(Action<IRequestExecutorBuilder> configure)
        {
            // arrange
            TestServer server = CreateTestServer(
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
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest { Query = "{ age }" });

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
            TestServer server = CreateTestServer(
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
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest { Query = "{ age }" });

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
            TestServer server = CreateTestServer(
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
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest { Query = "{ age }" });

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
            TestServer server = CreateTestServer(
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
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest { Query = "{ age }" });

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
            TestServer server = CreateTestServer(
                builder =>
                {
                    configure(builder);
                },
                context =>
                {
                    context.User = new ClaimsPrincipal(
                        new ClaimsIdentity("testauth"));
                });

            // act
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest { Query = "{ roles }" });

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
            TestServer server = CreateTestServer(
                builder =>
                {
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
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest { Query = "{ roles }" });

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
            TestServer server = CreateTestServer(
                builder =>
                {
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
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest { Query = "{ roles_ab }" });

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
            TestServer server = CreateTestServer(
                builder =>
                {
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
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest { Query = "{ roles_ab }" });

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
            TestServer server = CreateTestServer(
                builder =>
                {
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
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest { Query = "{ roles_ab }" });

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
            TestServer server = CreateTestServer(
                builder =>
                {
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
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest { Query = "{ roles }" });

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
            TestServer server = CreateTestServer(
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
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest { Query = "{ piped }" });

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
            TestServer server = CreateTestServer(
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
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest { Query = "{ piped }" });

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
            TestServer server = CreateTestServer(
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
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest { Query = "{ afterResolver }" });

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
            TestServer server = CreateTestServer(
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
            ClientQueryResult result =
                await server.PostAsync(new ClientQueryRequest { Query = "{ afterResolver }" });

            // assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            result.MatchSnapshot();
        }

        [Fact]
        public void AddAuthorizeDirectiveType_SchemaBuilderIsNull_ArgNullExec()
        {
            // arrange
            // act
            Action action = () =>
                AuthorizeSchemaBuilderExtensions
                    .AddAuthorizeDirectiveType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
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
}
