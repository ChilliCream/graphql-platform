using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Authorization;

public class BatchResolverAuthorizationTests
{
    [Theory]
    [InlineData("{ users { value } }", "value")]
    [InlineData("{ users { valueAfterResolver } }", "valueAfterResolver")]
    [InlineData("{ users { item { value } } }", "item")]
    [InlineData("{ users { itemAfterResolver { value } } }", "itemAfterResolver")]
    [InlineData("{ users { document { ... on Item { value } } } }", "document")]
    public async Task BatchResolver_Should_ReturnAuthorizationErrors_When_NotAllowed(
        string query,
        string field)
    {
        // arrange
        var handler = new AuthHandler((_, _) => AuthorizeResult.NotAllowed);

        // act
        var result = await ExecuteAsync(handler, query);

        // assert
        AssertNotAuthorized(result, field);
    }

    [Theory]
    [InlineData("{ users { value } }", "value")]
    [InlineData("{ users { valueAfterResolver } }", "valueAfterResolver")]
    [InlineData("{ users { item { value } } }", "item")]
    [InlineData("{ users { itemAfterResolver { value } } }", "itemAfterResolver")]
    [InlineData("{ users { document { ... on Item { value } } } }", "document")]
    public async Task BatchResolver_Should_ResolveValues_When_Allowed(
        string query,
        string field)
    {
        // arrange
        var handler = new AuthHandler((_, _) => AuthorizeResult.Allowed);

        // act
        var result = await ExecuteAsync(handler, query);

        // assert
        Assert.Empty(result.ExpectOperationResult().Errors);
        Assert.All(
            GetUsers(result),
            user => Assert.NotEqual(JsonValueKind.Null, GetValueKind(user, field)));
    }

    [Fact]
    public async Task BatchResolver_Should_OnlyReceiveAllowedParents_When_AuthorizedBeforeResolver()
    {
        // arrange
        var handler = new AuthHandler(
            (context, _) => context.Parent<User>().Name is "a"
                ? AuthorizeResult.Allowed
                : AuthorizeResult.NotAllowed);

        // act
        var result = await ExecuteAsync(handler, "{ users { name parentNames } }");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "users",
                    1,
                    "parentNames"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": {
                "users": [
                  {
                    "name": "a",
                    "parentNames": "a"
                  },
                  {
                    "name": "b",
                    "parentNames": null
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_OnlyReceiveAllowedParents_When_FirstParentIsNotAllowed()
    {
        // arrange
        var handler = new AuthHandler(
            (context, _) => context.Parent<User>().Name is "b"
                ? AuthorizeResult.Allowed
                : AuthorizeResult.NotAllowed);

        // act
        var result = await ExecuteAsync(handler, "{ users { name parentNames } }");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "users",
                    0,
                    "parentNames"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": {
                "users": [
                  {
                    "name": "a",
                    "parentNames": null
                  },
                  {
                    "name": "b",
                    "parentNames": "b"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task BatchResolver_Should_ApplyPoliciesInFieldPipelineOrder_When_FieldAndTypeAreAuthorized()
    {
        // arrange
        var policies = new List<string?>();
        var handler = new AuthHandler(
            (context, directive) =>
            {
                if (context.Parent<User>().Name is "a")
                {
                    policies.Add(directive.Policy);
                }

                return AuthorizeResult.Allowed;
            });

        // act
        await ExecuteAsync(handler, "{ users { regular { value } } }");
        var expected = policies.ToArray();
        policies.Clear();
        await ExecuteAsync(handler, "{ users { batched { value } } }");

        // assert
        Assert.Equal(["FIELD_1", "FIELD_2", "TYPE_2", "TYPE_1"], expected);
        Assert.Equal(expected, policies);
    }

    [Fact]
    public async Task BatchResolver_Should_ReturnAuthorizationError_When_RootTypeIsNotAllowed()
    {
        // arrange
        var handler = new AuthHandler((_, _) => AuthorizeResult.NotAllowed);

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d =>
                    {
                        d.Name("Query");
                        d.Authorize();
                        d.Field("regular")
                            .Type<StringType>()
                            .Resolve("value");
                        d.Field("batched")
                            .Type<StringType>()
                            .ResolveBatch(ResolveValues);
                    })
                .AddAuthorizationHandler(_ => handler)
                .ExecuteRequestAsync("{ regular batched }", cancellationToken: TestContext.Current.CancellationToken);

        // assert
        using var json = JsonDocument.Parse(result.ToJson());
        var data = json.RootElement.GetProperty("data");
        var operationResult = result.ExpectOperationResult();
        Assert.Equal(
            ["batched", "regular"],
            operationResult.Errors.Select(e => e.Path!.ToString().TrimStart('/')).Order());
        Assert.All(
            operationResult.Errors,
            e => Assert.Equal(ErrorCodes.Authentication.NotAuthorized, e.Code));
        Assert.Equal(
            [JsonValueKind.Null, JsonValueKind.Null],
            [data.GetProperty("regular").ValueKind, data.GetProperty("batched").ValueKind]);
    }

    [Fact]
    public async Task BatchResolver_Should_ReturnAuthorizationErrors_When_SchemaFirstFieldIsNotAllowed()
    {
        // arrange
        var handler = new AuthHandler((_, _) => AuthorizeResult.NotAllowed);

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(
                    """
                    type Query {
                      users: [User!]!
                    }

                    type User {
                      name: String!
                      value: String @authorize(policy: "READ_VALUE")
                    }
                    """)
                .BindRuntimeType<Query>()
                .BindRuntimeType<User>()
                .AddTypeExtension(
                    new ObjectTypeExtension(
                        d => d.Name("User").Field("value").ResolveBatch(ResolveValues)))
                .AddAuthorizationHandler(_ => handler)
                .ExecuteRequestAsync(
                    "{ users { name value } }",
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        AssertNotAuthorized(result, "value");
    }

    [Fact]
    public async Task BatchResolver_Should_ReturnAuthorizationErrors_When_InheritedFromInterfaceAndNotAllowed()
    {
        // arrange
        var handler = new AuthHandler((_, _) => AuthorizeResult.NotAllowed);

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d =>
                    {
                        d.Name("Query");
                        d.Field("users")
                            .Type<ListType<ObjectType<User>>>()
                            .Resolve(new List<User> { new("a"), new("b") });
                    })
                .AddInterfaceType<INamed>(
                    d => d.Field("value").ResolveBatchWith<NamedResolvers>(t => t.Value(null!)))
                .AddObjectType<User>(
                    d =>
                    {
                        d.Implements<InterfaceType<INamed>>();
                        d.Field("value").Type<StringType>().Authorize();
                    })
                .AddAuthorizationHandler(_ => handler)
                .ExecuteRequestAsync(
                    "{ users { name value } }",
                    cancellationToken: TestContext.Current.CancellationToken);

        // assert
        AssertNotAuthorized(result, "value");
    }

    private static async Task<IExecutionResult> ExecuteAsync(
        IAuthorizationHandler handler,
        string query)
        => await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddTypeExtension<UserExtensions>()
            .AddType<Item>()
            .AddAuthorizationHandler(_ => handler)
            .ExecuteRequestAsync(query, cancellationToken: TestContext.Current.CancellationToken);

    private static ValueTask<IReadOnlyList<ResolverResult>> ResolveValues(
        IReadOnlyList<IResolverContext> contexts)
    {
        var results = new ResolverResult[contexts.Count];
        Array.Fill(results, ResolverResult.Ok("value"));
        return new ValueTask<IReadOnlyList<ResolverResult>>(results);
    }

    private static void AssertNotAuthorized(IExecutionResult result, string field)
    {
        Assert.Collection(
            result.ExpectOperationResult().Errors,
            e => Assert.Equal(ErrorCodes.Authentication.NotAuthorized, e.Code),
            e => Assert.Equal(ErrorCodes.Authentication.NotAuthorized, e.Code));
        Assert.All(
            GetUsers(result),
            user => Assert.Equal(JsonValueKind.Null, GetValueKind(user, field)));
    }

    private static List<JsonElement> GetUsers(IExecutionResult result)
    {
        using var document = JsonDocument.Parse(result.ToJson());
        return document.RootElement
            .GetProperty("data")
            .GetProperty("users")
            .EnumerateArray()
            .Select(t => t.Clone())
            .ToList();
    }

    private static JsonValueKind GetValueKind(JsonElement user, string field)
        => user.GetProperty(field).ValueKind;

    public sealed class Query
    {
        public List<User> Users => [new User("a"), new User("b")];
    }

    public interface INamed
    {
        string Name { get; }
    }

    public sealed record User(string Name) : INamed;

    [UnionType]
    public interface IDocument;

    [Authorize]
    public sealed record Item(string Value) : IDocument;

    [Authorize(ApplyPolicy.AfterResolver)]
    public sealed record ItemAfterResolver(string Value);

    [Authorize("TYPE_1")]
    [Authorize("TYPE_2")]
    public sealed record ItemWithPolicies(string Value);

    [ExtendObjectType<User>]
    public sealed class UserExtensions
    {
        [Authorize]
        [BatchResolver]
        public List<string?> Value([Parent] List<User> users)
            => users.ConvertAll<string?>(u => "value " + u.Name);

        [Authorize(ApplyPolicy.AfterResolver)]
        [BatchResolver]
        public List<string?> ValueAfterResolver([Parent] List<User> users)
            => users.ConvertAll<string?>(u => "value " + u.Name);

        [BatchResolver]
        public List<Item?> Item([Parent] List<User> users)
            => users.ConvertAll<Item?>(u => new Item("value " + u.Name));

        [BatchResolver]
        public List<ItemAfterResolver?> ItemAfterResolver([Parent] List<User> users)
            => users.ConvertAll<ItemAfterResolver?>(u => new ItemAfterResolver("value " + u.Name));

        [BatchResolver]
        public List<IDocument?> Document([Parent] List<User> users)
            => users.ConvertAll<IDocument?>(u => new Item("value " + u.Name));

        [Authorize]
        [BatchResolver]
        public List<string?> ParentNames([Parent] List<User> users)
            => users.ConvertAll<string?>(_ => string.Join(",", users.Select(u => u.Name)));

        [Authorize("FIELD_1")]
        [Authorize("FIELD_2")]
        public ItemWithPolicies? Regular([Parent] User user)
            => new("value " + user.Name);

        [Authorize("FIELD_1")]
        [Authorize("FIELD_2")]
        [BatchResolver]
        public List<ItemWithPolicies?> Batched([Parent] List<User> users)
            => users.ConvertAll<ItemWithPolicies?>(u => new ItemWithPolicies("value " + u.Name));
    }

    public sealed class NamedResolvers
    {
        [BatchResolver]
        public List<string?> Value([Parent] List<User> users)
            => users.ConvertAll<string?>(u => "value " + u.Name);
    }

    private sealed class AuthHandler(
        Func<IMiddlewareContext, AuthorizeDirective, AuthorizeResult> resolver)
        : IAuthorizationHandler
    {
        public ValueTask<AuthorizeResult> AuthorizeAsync(
            IMiddlewareContext context,
            AuthorizeDirective directive,
            CancellationToken cancellationToken = default)
            => new(resolver(context, directive));

        public ValueTask<AuthorizeResult> AuthorizeAsync(
            AuthorizationContext context,
            IReadOnlyList<AuthorizeDirective> directives,
            CancellationToken cancellationToken = default)
            => new(AuthorizeResult.Allowed);
    }
}
