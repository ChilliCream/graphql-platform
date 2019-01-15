using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
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

        [Fact]
        public async Task DefaultPolicy_NotFound()
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddAuthorization(options =>
                    {
                        options.DefaultPolicy = null;
                    });

                    services.AddGraphQL(CreateExecuter());
                },
                context =>
                {
                    // no user
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
            result.Snapshot();
        }

        [Fact]
        public async Task Policy_NotFound()
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

                    services.AddGraphQL(CreateExecuter());
                },
                context =>
                {
                    // no user
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
            result.Snapshot();
        }

        [Fact]
        public async Task Policy_NotAuthorized()
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

                    services.AddGraphQL(CreateExecuter());
                },
                context =>
                {
                    // no user
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
            result.Snapshot();
        }

        [Fact]
        public async Task Policy_Authorized()
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

                    services.AddGraphQL(CreateExecuter());
                },
                context =>
                {
                    var identity = new ClaimsIdentity();
                    identity.AddClaim(new Claim(
                        ClaimTypes.DateOfBirth,
                        "2013-05-30"));
                    context.User.AddIdentity(identity);
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
            result.Snapshot();
        }

        [Fact]
        public async Task Roles_UserHasNoRoles_NotAuthorized()
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(CreateExecuter());
                },
                context =>
                {
                    // no user
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
            result.Snapshot();
        }

        [Fact]
        public async Task Roles_UserHasDifferentRoles_NotAuthorized()
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(CreateExecuter());
                },
                context =>
                {
                    var identity = new ClaimsIdentity();
                    identity.AddClaim(new Claim(
                        ClaimTypes.Role,
                        "b"));
                    context.User.AddIdentity(identity);
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
            result.Snapshot();
        }

        [Fact]
        public async Task Roles_UserHasOneOfTheRoles_NotAuthorized()
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(CreateExecuter());
                },
                context =>
                {
                    var identity = new ClaimsIdentity();
                    identity.AddClaim(new Claim(
                        ClaimTypes.Role,
                        "a"));
                    identity.AddClaim(new Claim(
                        ClaimTypes.Role,
                        "c"));
                    context.User.AddIdentity(identity);
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
            result.Snapshot();
        }

        [Fact]
        public async Task Roles_UserHasAllOfTheRoles_Authorized()
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(CreateExecuter());
                },
                context =>
                {
                    var identity = new ClaimsIdentity();
                    identity.AddClaim(new Claim(
                        ClaimTypes.Role,
                        "a"));
                    identity.AddClaim(new Claim(
                        ClaimTypes.Role,
                        "b"));
                    context.User.AddIdentity(identity);
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
            result.Snapshot();
        }

        [Fact]
        public async Task Roles_Authorized()
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(CreateExecuter());
                },
                context =>
                {
                    var identity = new ClaimsIdentity();
                    identity.AddClaim(new Claim(
                        ClaimTypes.Role,
                        "a"));
                    context.User.AddIdentity(identity);
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
            result.Snapshot();
        }


        private TestServer CreateTestServer(
            Action<IServiceCollection> configureServices,
            Action<HttpContext> configureUser)
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
                                OnCreateRequest = (ctx, r, p, ct) =>
                                {
                                    configureUser(ctx);
                                    return Task.CompletedTask;
                                }
                            });
                        });
                });
        }


        private static IQueryExecuter CreateExecuter()
        {
            return Schema.Create(
                @"
                    type Query {
                        default: String @authorize
                        age: String @authorize(policy: ""HasAgeDefined"")
                        roles: String @authorize(roles: [""a""])
                        roles_ab: String @authorize(roles: [""a"" ""b""])
                        piped: String 
                            @authorize(policy: ""a"") 
                            @authorize(policy: ""b"")
                    }
                ",
                configuration =>
                {
                    configuration.RegisterAuthorizeDirectiveType();
                    configuration.Use(next => context =>
                    {
                        context.Result = "foo";
                        return next.Invoke(context);
                    });
                })
                .MakeExecutable();
        }
    }
}
