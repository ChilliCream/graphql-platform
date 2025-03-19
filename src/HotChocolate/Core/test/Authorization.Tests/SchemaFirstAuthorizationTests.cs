using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Authorization;

public class SchemaFirstAuthorizationTests
{
    [Fact]
    public async Task Authorize_Apply_Can_Be_Omitted()
    {
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(
                """
                type Query @authorize(roles: [ "policy_tester_noupdate", "policy_tester_update_noread", "authorizationHandlerTester" ])  {
                    hello: String @authorize(roles: ["admin"])
                }
                """)
            .AddResolver("Query", "hello", "world")
            .AddAuthorizationHandler<MockAuth>()
            .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    private sealed class MockAuth : IAuthorizationHandler
    {
        public ValueTask<AuthorizeResult> AuthorizeAsync(
            IMiddlewareContext context,
            AuthorizeDirective directive,
            CancellationToken cancellationToken = default)
            => new(AuthorizeResult.NotAllowed);

        public ValueTask<AuthorizeResult> AuthorizeAsync(
            AuthorizationContext context,
            IReadOnlyList<AuthorizeDirective> directives,
            CancellationToken cancellationToken = default)
            => new(AuthorizeResult.NotAllowed);
    }
}
