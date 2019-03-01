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

                    services.AddGraphQL(CreateExecutor());
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
            result.MatchSnapshot();
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

                    services.AddGraphQL(CreateExecutor());
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
            result.MatchSnapshot();
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

                    services.AddGraphQL(CreateExecutor());
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
            result.MatchSnapshot();
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

                    services.AddGraphQL(CreateExecutor());
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
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Roles_UserHasNoRoles_NotAuthorized()
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(CreateExecutor());
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
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Roles_UserHasDifferentRoles_NotAuthorized()
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(CreateExecutor());
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
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Roles_UserHasOneOfTheRoles_NotAuthorized()
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(CreateExecutor());
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
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Roles_UserHasAllOfTheRoles_Authorized()
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(CreateExecutor());
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
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Roles_Authorized()
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(CreateExecutor());
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
            result.MatchSnapshot();
        }

        [Fact]
        public async Task PipedAuthorizeDirectives_Authorized()
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

                    services.AddGraphQL(CreateExecutor());
                },
                context =>
                {
                    var identity = new ClaimsIdentity();
                    identity.AddClaim(new Claim(
                        ClaimTypes.DateOfBirth,
                        "2013-05-30"));
                    identity.AddClaim(new Claim(
                        ClaimTypes.Country,
                        "US"));
                    context.User.AddIdentity(identity);
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

        [Fact]
        public async Task PipedAuthorizeDirectives_SecondFails_NotAuthorized()
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

                    services.AddGraphQL(CreateExecutor());
                },
                context =>
                {
                    var identity = new ClaimsIdentity();
                    identity.AddClaim(new Claim(
                        ClaimTypes.DateOfBirth,
                        "2013-05-30"));
                    context.User.AddIdentity(identity);
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
                                OnCreateRequest = (ctx, r, ct) =>
                                {
                                    configureUser(ctx);
                                    return Task.CompletedTask;
                                }
                            });
                        });
                });
        }


        private static IQueryExecutor CreateExecutor()
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
