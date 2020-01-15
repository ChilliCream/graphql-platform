using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
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
        public void AuthorizeDirective_Defined(ISchema schema)
        {
            // arrange
            // assert
            Assert.Contains(
                schema.DirectiveTypes,
                x => x.GetType() == typeof(AuthorizeDirectiveType));
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task DefaultPolicy_NotFound(ISchema schema)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddAuthorization(options =>
                    {
                        options.DefaultPolicy = null;
                    });

                    services.AddGraphQL(schema);
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
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task NoAuthServices_Authenticated_True(ISchema schema)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(schema);
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
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task NoAuthServices_Authenticated_False(ISchema schema)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(schema);
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
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task Policy_NotFound(ISchema schema)
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

                    services.AddGraphQL(schema);
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
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task Policy_NotAuthorized(ISchema schema)
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

                    services.AddGraphQL(schema);
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
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task Policy_Resources_Is_IResolverContext(ISchema schema)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddAuthorization(options =>
                    {
                        options.AddPolicy("HasAgeDefined", policy =>
                            policy.RequireAssertion(context =>
                                context.Resource is IResolverContext));
                    });

                    services.AddGraphQL(schema);
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
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task Policy_Authorized(ISchema schema)
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

                    services.AddGraphQL(schema);
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
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task Roles_UserHasNoRoles_NotAuthorized(
            ISchema schema)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(schema);
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
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task Roles_UserHasDifferentRoles_NotAuthorized(ISchema schema)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(schema);
                },
                context =>
                {
                    var identity = new ClaimsIdentity("testauth");
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

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task Roles_UserHasNoneOfTheRoles_NotAuthorized(ISchema schema)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(schema);
                },
                context =>
                {
                    var identity = new ClaimsIdentity("testauth");
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

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task Roles_UserHasAllOfTheRoles_Authorized(ISchema schema)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(schema);
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
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task Roles_UserHasOneOfTheRoles_Authorized(
            ISchema schema)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(schema);
                },
                context =>
                {
                    var identity = new ClaimsIdentity("testauth");
                    identity.AddClaim(new Claim(
                        ClaimTypes.Role,
                        "a"));
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
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task Roles_Authorized(ISchema schema)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddGraphQL(schema);
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
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task PipedAuthorizeDirectives_Authorized(
            ISchema schema)
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

                    services.AddGraphQL(schema);
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
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task PipedAuthorizeDirectives_SecondFails_NotAuthorized(
            ISchema schema)
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

                    services.AddGraphQL(schema);
                },
                context =>
                {
                    var identity = new ClaimsIdentity("testauth");
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

        [Theory]
        [ClassData(typeof(AuthorizationTestData))]
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task Policy_Is_Executed_After_Resolver_User_Is_Allowed(ISchema schema)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddAuthorization(options =>
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

                    services.AddGraphQL(schema);
                },
                context =>
                {
                    var identity = new ClaimsIdentity("testauth");
                    identity.AddClaim(new Claim(
                        ClaimTypes.DateOfBirth,
                        "2013-05-30"));
                    context.User.AddIdentity(identity);
                });

            var request = "{ afterResolver }";
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
        [ClassData(typeof(AuthorizationAttributeTestData))]
        public async Task Policy_Is_Executed_After_Resolver_User_Is_Denied(ISchema schema)
        {
            // arrange
            TestServer server = CreateTestServer(
                services =>
                {
                    services.AddAuthorization(options =>
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

                    services.AddGraphQL(schema);
                },
                context =>
                {
                    var identity = new ClaimsIdentity("testauth");
                    identity.AddClaim(new Claim(
                        ClaimTypes.DateOfBirth,
                        "2013-05-30"));
                    context.User.AddIdentity(identity);
                });

            var request = "{ afterResolver }";
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
            Action<HttpContext> configureUser)
        {
            return ServerFactory.Create(
                services =>
                {
                    configureServices(services);
                    services.AddQueryRequestInterceptor(
                        (ctx, r, ct) =>
                        {
                            configureUser(ctx);
                            return Task.CompletedTask;
                        });
                },
                app => app.UseGraphQL());
        }
    }
}
