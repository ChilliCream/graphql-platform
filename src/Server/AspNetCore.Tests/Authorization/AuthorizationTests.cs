using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.AspNetCore.Authorization
{
    public class AuthorizationTests
        : IClassFixture<TestServerFactory>
    {
        public AuthorizationTests(TestServerFactory testServerFactory)
        {
            TestServerFactory = testServerFactory;
        }

        private TestServerFactory TestServerFactory { get; }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        public void AuthorizeDirective_Defined(IQueryExecutor executor)
        {
            // arrange
            ISchema schema = executor.Schema;

            // assert
            Assert.Contains(
                schema.DirectiveTypes,
                x => x.GetType() == typeof(AuthorizeDirectiveType));
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        public async Task DefaultPolicy_NotFound(IQueryExecutor executor)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddAuthorization(options =>
                    {
                        options.DefaultPolicy = null;
                    });

                    services.AddGraphQL(executor);
                },
                context =>
                {
                    context.User = new ClaimsPrincipal(
                        new ClaimsIdentity("testauth"));
                });

            var request = "{ default }";
            var contentType = "application/graphql";

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, contentType, null);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            var json = await message.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert
                .DeserializeObject<ClientQueryResult>(json);
            Assert.NotNull(result.Errors);
            result.MatchSnapshot();
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        public async Task NoAuthServices_Autheticated_True(
            IQueryExecutor executor)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(executor);
                },
                context =>
                {
                    context.User = new ClaimsPrincipal(
                        new ClaimsIdentity("testauth"));
                });

            var request = "{ default }";
            var contentType = "application/graphql";

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, contentType, null);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            var json = await message.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert
                .DeserializeObject<ClientQueryResult>(json);
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        public async Task NoAuthServices_Autheticated_False(
            IQueryExecutor executor)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(executor);
                },
                context =>
                {
                    context.User = new ClaimsPrincipal(
                        new ClaimsIdentity());
                });

            var request = "{ default }";
            var contentType = "application/graphql";

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, contentType, null);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            var json = await message.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert
                .DeserializeObject<ClientQueryResult>(json);
            Assert.NotNull(result.Errors);
            result.MatchSnapshot();
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        public async Task Policy_NotFound(IQueryExecutor executor)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddAuthorization(options =>
                    {
                        options.AddPolicy("FooBar", policy =>
                            policy.RequireAssertion(context =>
                                context.User.HasClaim(c =>
                                    c.Type == ClaimTypes.DateOfBirth)));
                    });

                    services.AddGraphQL(executor);
                },
                context =>
                {
                    context.User = new ClaimsPrincipal(
                        new ClaimsIdentity("testauth"));
                });

            var request = "{ age }";
            var contentType = "application/graphql";

            // ac
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, contentType, null);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            var json = await message.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert
                .DeserializeObject<ClientQueryResult>(json);
            Assert.NotNull(result.Errors);
            result.MatchSnapshot();
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        public async Task Policy_NotAuthorized(IQueryExecutor executor)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddAuthorization(options =>
                    {
                        options.AddPolicy("HasAgeDefined", policy =>
                            policy.RequireAssertion(context =>
                                context.User.HasClaim(c =>
                                    c.Type == ClaimTypes.DateOfBirth)));
                    });

                    services.AddGraphQL(executor);
                },
                context =>
                {
                    context.User = new ClaimsPrincipal(
                        new ClaimsIdentity("testauth"));
                });

            var request = "{ age }";
            var contentType = "application/graphql";

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, contentType, null);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            var json = await message.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert
                .DeserializeObject<ClientQueryResult>(json);
            Assert.NotNull(result.Errors);
            result.MatchSnapshot();
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        public async Task Policy_Authorized(IQueryExecutor executor)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddAuthorization(options =>
                    {
                        options.AddPolicy("HasAgeDefined", policy =>
                            policy.RequireAssertion(context =>
                                context.User.HasClaim(c =>
                                    c.Type == ClaimTypes.DateOfBirth)));
                    });

                    services.AddGraphQL(executor);
                },
                context =>
                {
                    var identity = new ClaimsIdentity("testauth");
                    identity.AddClaim(new Claim(
                        ClaimTypes.DateOfBirth,
                        "2013-05-30"));
                    context.User = new ClaimsPrincipal(identity);
                });

            var request = "{ age }";
            var contentType = "application/graphql";

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, contentType, null);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            var json = await message.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert
                .DeserializeObject<ClientQueryResult>(json);
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        public async Task Roles_UserHasNoRoles_NotAuthorized(
            IQueryExecutor executor)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(executor);
                },
                context =>
                {
                    context.User = new ClaimsPrincipal(
                        new ClaimsIdentity("testauth"));
                });

            var request = "{ roles }";
            var contentType = "application/graphql";

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, contentType, null);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            var json = await message.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert
                .DeserializeObject<ClientQueryResult>(json);
            Assert.NotNull(result.Errors);
            result.MatchSnapshot();
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        public async Task Roles_UserHasDifferentRoles_NotAuthorized(
            IQueryExecutor executor)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(executor);
                },
                context =>
                {
                    var identity = new ClaimsIdentity("testauth");
                    identity.AddClaim(new Claim(
                        ClaimTypes.Role,
                        "b"));
                    context.AddIdentity(identity);
                });

            var request = "{ roles }";
            var contentType = "application/graphql";

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, contentType, null);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            var json = await message.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert
                .DeserializeObject<ClientQueryResult>(json);
            Assert.NotNull(result.Errors);
            result.MatchSnapshot();
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        public async Task Roles_UserHasOneOfTheRoles_NotAuthorized(
            IQueryExecutor executor)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(executor);
                },
                context =>
                {
                    var identity = new ClaimsIdentity("testauth");
                    identity.AddClaim(new Claim(
                        ClaimTypes.Role,
                        "a"));
                    identity.AddClaim(new Claim(
                        ClaimTypes.Role,
                        "c"));
                    context.AddIdentity(identity);
                });

            var request = "{ roles_ab }";
            var contentType = "application/graphql";

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, contentType, null);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            var json = await message.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert
                .DeserializeObject<ClientQueryResult>(json);
            Assert.NotNull(result.Errors);
            result.MatchSnapshot();
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        public async Task Roles_UserHasAllOfTheRoles_Authorized(
            IQueryExecutor executor)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(executor);
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

            var request = "{ roles_ab }";
            var contentType = "application/graphql";

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, contentType, null);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            var json = await message.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert
                .DeserializeObject<ClientQueryResult>(json);
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        public async Task Roles_Authorized(IQueryExecutor executor)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(executor);
                },
                context =>
                {
                    var identity = new ClaimsIdentity("testauth");
                    identity.AddClaim(new Claim(
                        ClaimTypes.Role,
                        "a"));
                    context.User = new ClaimsPrincipal(identity);
                });

            var request = "{ roles }";
            var contentType = "application/graphql";

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, contentType, null);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            var json = await message.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert
                .DeserializeObject<ClientQueryResult>(json);
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        public async Task PipedAuthorizeDirectives_Authorized(
            IQueryExecutor executor)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddAuthorization(options =>
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

                    services.AddGraphQL(executor);
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

            var request = "{ piped }";
            var contentType = "application/graphql";

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, contentType, null);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            var json = await message.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert
                .DeserializeObject<ClientQueryResult>(json);
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        public async Task PipedAuthorizeDirectives_SecondFails_NotAuthorized(
            IQueryExecutor executor)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddAuthorization(options =>
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

                    services.AddGraphQL(executor);
                },
                context =>
                {
                    var identity = new ClaimsIdentity("testauth");
                    identity.AddClaim(new Claim(
                        ClaimTypes.DateOfBirth,
                        "2013-05-30"));
                    context.AddIdentity(identity);
                });

            var request = "{ piped }";
            var contentType = "application/graphql";

            // act
            HttpResponseMessage message =
                await server.SendPostRequestAsync(request, contentType, null);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            var json = await message.Content.ReadAsStringAsync();
            ClientQueryResult result = JsonConvert
                .DeserializeObject<ClientQueryResult>(json);
            Assert.NotNull(result.Errors);

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
            Action<IServiceCollection> configureServices,
            Action<IHttpContext> configureUser)
        {
            return TestServerFactory.Create(
                builder =>
                {
                    return builder
                        .ConfigureServices(configureServices)
                        .Configure(app =>
                        {
                            app.UseGraphQL(new QueryMiddlewareOptions
                            {
                                OnCreateRequest = (ctx, r, ct) =>
                                {
                                    configureUser(ctx);
                                    r.SetProperty(
                                        nameof(ClaimsPrincipal),
                                        ctx.User);
                                    return Task.CompletedTask;
                                }
                            });
                        });
                });
        }
    }
}
