using CookieCrumble;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.AspNetCore;

public class IntrospectionTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task Introspection_Request_When_Development_Success()
    {
        // arrange
        var client = GetClient(Environments.Development);

        // act
        var response = await client.PostAsync(
            """
            {
                __type(name: "Query") {
                    name
                }
            }
            """,
            Url);

        // assert
        response.HttpResponseMessage.MatchMarkdownSnapshot();
    }

#if NET7_0_OR_GREATER
    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task Introspection_Request_When_NOT_Development_Fail(string environment)
    {
        // arrange
        var client = GetClient(environment);

        // act
        var response = await client.PostAsync(
            """
            {
                __type(name: "Query") {
                    name
                }
            }
            """,
            Url);

        // assert
        response.HttpResponseMessage.MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task Introspection_Request_With_Rule_Removed_Fail(string environment)
    {
        // arrange
        var client = GetClient(environment, removeRule: true);

        // act
        var response = await client.PostAsync(
            """
            {
                __type(name: "Query") {
                    name
                }
            }
            """,
            Url);

        // assert
        response.HttpResponseMessage.MatchMarkdownSnapshot();
    }

#endif
    private GraphQLHttpClient GetClient(string environment, bool removeRule = false)
    {
        var server = CreateStarWarsServer(
            environment: environment,
            configureServices: s =>
            {
                if (removeRule)
                {
                    s.AddGraphQL().RemoveIntrospectionAllowedRule();
                }
            });
        return new DefaultGraphQLHttpClient(server.CreateClient());
    }
}
