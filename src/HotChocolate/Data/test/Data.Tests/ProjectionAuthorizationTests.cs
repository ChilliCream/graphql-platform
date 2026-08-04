using HotChocolate.Authorization;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class ProjectionAuthorizationTests
{
    [Fact]
    public async Task Projection_Should_NotLeakInternalSelections_When_AuthorizedIsProjectedFieldIsNotSelected()
    {
        // arrange
        // Secret is force projected and protected. The client does not select it, so the
        // projection optimizers inject internal selections (_combinedSelectionSet,
        // __projection_alias_N). Those are engine-internal and must never surface to the
        // client, neither their data nor any error nor their names in an error path.
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddAuthorizationHandler(_ => new DenyAllHandler())
            .AddProjections()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              items {
                items {
                  id
                  name
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": {
                  "items": [
                    {
                      "id": 1,
                      "name": "ok"
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Projection_Should_SurfaceAuthErrorWithoutLeak_When_AuthorizedIsProjectedFieldIsSelected()
    {
        // arrange
        // The client explicitly selects the protected field, so the authorization error is
        // expected. It must be reported once, with the client facing path, and must not be
        // duplicated under the internal _combinedSelectionSet selection. The selected field
        // is non-null, so the denial nulls the parent via standard non-null propagation.
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddAuthorizationHandler(_ => new DenyAllHandler())
            .AddProjections()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              items {
                items {
                  id
                  name
                  secret
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "items",
                    "items",
                    0,
                    "secret"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": {
                "items": {
                  "items": null
                }
              }
            }
            """);
    }

    public class Query
    {
        [UseOffsetPaging]
        [UseProjection]
        public IQueryable<Item> GetItems()
            => new[] { new Item { Id = 1, Name = "ok", Secret = "hidden" } }.AsQueryable();
    }

    public class Item
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;

        [IsProjected(true)]
        [Authorize("DenyAll", Apply = ApplyPolicy.AfterResolver)]
        public string Secret { get; set; } = default!;
    }

    private sealed class DenyAllHandler : IAuthorizationHandler
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
            => new(AuthorizeResult.Allowed);
    }
}
