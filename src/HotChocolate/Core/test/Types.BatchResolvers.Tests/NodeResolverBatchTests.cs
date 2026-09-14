using HotChocolate.Authorization;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class NodeResolverBatchTests : BatchScenarioTests
{
    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute),
        SourceGenerated = new Declaration(ConfigureSourceGenerated),
        Fluent = new Declaration(ConfigureFluent)
    };

    [Theory]
    [BatchMatrix]
    public async Task NodeResolver_Should_Dispatch_Separately_When_AliasedFieldsShareType(DeclarationStyle style)
    {
        // arrange
        // aliases never coalesce (hc-0-6cq.15), even when one alias repeats another alias' id
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        var x = Convert.ToBase64String("NodeEntity:x"u8);
        var y = Convert.ToBase64String("NodeEntity:y"u8);

        // act
        var result = await ExecuteAsync(
            executor,
            $$"""
            {
                a: node(id: "{{x}}") { ... on NodeEntity { id name } }
                b: node(id: "{{y}}") { ... on NodeEntity { id name } }
                dup: node(id: "{{x}}") { ... on NodeEntity { id name } }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        var ids = Probe.Invocations.SelectMany(i => i.Keys).Select(k => (string)k!).Order().ToArray();
        Assert.Equal(3, Probe.Invocations.Count);
        Assert.Equal(["x", "x", "y"], ids);
        result.MatchInlineSnapshot(
            $$"""
            {
              "data": {
                "a": {
                  "id": "{{x}}",
                  "name": "x"
                },
                "b": {
                  "id": "{{y}}",
                  "name": "y"
                },
                "dup": {
                  "id": "{{x}}",
                  "name": "x"
                }
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task NodeResolver_Should_Dispatch_PerAlias_When_FieldsMixBatchAndClassicResolvers(
        DeclarationStyle style)
    {
        // arrange
        // two aliases dispatch the batch resolver, the third dispatches a classic resolver
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        var x = Convert.ToBase64String("NodeEntity:x"u8);
        var y = Convert.ToBase64String("NodeEntity:y"u8);
        var foo = Convert.ToBase64String("ClassicEntity:foo"u8);

        // act
        var result = await ExecuteAsync(
            executor,
            $$"""
            {
                a: node(id: "{{x}}") { ... on NodeEntity { id name } }
                b: node(id: "{{y}}") { ... on NodeEntity { id name } }
                c: node(id: "{{foo}}") { ... on ClassicEntity { id name } }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        var ids = Probe.Invocations.SelectMany(i => i.Keys).Select(k => (string)k!).Order().ToArray();
        Assert.Equal(2, Probe.Invocations.Count);
        Assert.Equal(["x", "y"], ids);
        result.MatchInlineSnapshot(
            $$"""
            {
              "data": {
                "a": {
                  "id": "{{x}}",
                  "name": "x"
                },
                "b": {
                  "id": "{{y}}",
                  "name": "y"
                },
                "c": {
                  "id": "{{foo}}",
                  "name": "foo"
                }
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task NodeResolver_Should_Dispatch_Once_When_NodesIdsContainDuplicates(DeclarationStyle style)
    {
        // arrange
        // a repeated id still reaches the batch node resolver once, positionally (hc-0-6cq.15)
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        var x = Convert.ToBase64String("NodeEntity:x"u8);
        var y = Convert.ToBase64String("NodeEntity:y"u8);

        // act
        var result = await ExecuteAsync(
            executor,
            $$"""
            {
                nodes(ids: ["{{x}}", "{{y}}", "{{x}}"]) {
                    ... on NodeEntity { id name }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(Probe.Invocations);
        Assert.Equal(3, Probe.Invocations[0].Keys.Count);
        result.MatchInlineSnapshot(
            $$"""
            {
              "data": {
                "nodes": [
                  {
                    "id": "{{x}}",
                    "name": "x"
                  },
                  {
                    "id": "{{y}}",
                    "name": "y"
                  },
                  {
                    "id": "{{x}}",
                    "name": "x"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task Authorize_Should_Deny_Entry_At_IndexedPath_When_NodesContainProtectedType(
        DeclarationStyle style)
    {
        // arrange
        // a denied entry in a nodes() batch lands at its own indexed path, not the whole field (hc-0-bpl.7)
        AuthHandler.Resolver = (_, directive) => directive.Policy == "READ_PROTECTED_NODE"
            ? AuthorizeResult.NotAllowed
            : AuthorizeResult.Allowed;
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        var x = Convert.ToBase64String("NodeEntity:x"u8);
        var protectedId = Convert.ToBase64String("ProtectedNode:abc"u8);

        // act
        var result = await ExecuteAsync(
            executor,
            $$"""
            {
                nodes(ids: ["{{x}}", "{{protectedId}}"]) {
                    __typename
                    ... on NodeEntity { id name }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(Probe.Invocations);
        result.MatchInlineSnapshot(
            $$"""
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "nodes",
                    1
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": {
                "nodes": [
                  {
                    "__typename": "NodeEntity",
                    "id": "{{x}}",
                    "name": "x"
                  },
                  null
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task NodeResolver_Should_Report_At_FieldPath_When_NodesContainMalformedId(DeclarationStyle style)
    {
        // arrange
        // a malformed id fails before any child is staged: the error path is the whole field
        // ["nodes"], not an indexed entry, and nodes' NonNull type nulls the whole response (bpl.7)
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        var x = Convert.ToBase64String("NodeEntity:x"u8);

        // act
        var result = await ExecuteAsync(
            executor,
            $$"""
            {
                nodes(ids: ["{{x}}", "garbage"]) {
                    ... on NodeEntity { id name }
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
                  "message": "The node ID string has an invalid format.",
                  "path": [
                    "nodes"
                  ],
                  "extensions": {
                    "originalValue": "garbage"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task NodeResolver_Should_Round_Trip_When_CustomIdValueSerializer(DeclarationStyle style)
    {
        // arrange
        // the custom serializer's int key is parsed once and re-encoded on the resolved entity
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        var key = Convert.ToBase64String("CustomKeyEntity:key-42"u8);

        // act
        var result = await ExecuteAsync(
            executor,
            $$"""
            {
                a: node(id: "{{key}}") { ... on CustomKeyEntity { id value } }
                b: node(id: "garbage") { ... on CustomKeyEntity { id value } }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            $$"""
            {
              "errors": [
                {
                  "message": "The node ID string has an invalid format.",
                  "path": [
                    "b"
                  ],
                  "extensions": {
                    "originalValue": "garbage"
                  }
                }
              ],
              "data": {
                "a": {
                  "id": "{{key}}",
                  "value": 42
                },
                "b": null
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task NodeResolver_Should_Deliver_Node_When_InsideDeferFragment(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(
            style, b => b.ModifyOptions(o => o.EnableDefer = true), TestContext.Current.CancellationToken);
        var x = Convert.ToBase64String("NodeEntity:x"u8);

        // act
        await using var result = await ExecuteAsync(
            executor,
            $$"""
            {
                ... @defer {
                    node(id: "{{x}}") { ... on NodeEntity { id name } }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot(style);
    }

    [Theory]
    [BatchMatrix]
    public async Task Authorize_Should_Deny_Node_When_BatchNodeTypeIsProtected(DeclarationStyle style)
    {
        // arrange
        AuthHandler.Resolver = (_, directive) => directive.Policy == "READ_PROTECTED_NODE"
            ? AuthorizeResult.NotAllowed
            : AuthorizeResult.Allowed;
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        var id = Convert.ToBase64String("ProtectedNode:abc"u8);

        // act
        var result = await ExecuteAsync(
            executor,
            $$"""
            {
                node(id: "{{id}}") { __typename }
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
                    "node"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": {
                "node": null
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task Authorize_Should_Resolve_Node_When_BatchNodeTypeAllows(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        var id = Convert.ToBase64String("ProtectedNode:abc"u8);

        // act
        var result = await ExecuteAsync(
            executor,
            $$"""
            {
                node(id: "{{id}}") { __typename }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "node": {
                  "__typename": "ProtectedNode"
                }
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task NodeResolver_Should_Resolve_When_FetchedThroughNodeField(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);
        var id = Convert.ToBase64String("NodeEntity:abc"u8);

        // act
        var result = await ExecuteAsync(
            executor,
            $$"""
            {
                node(id: "{{id}}") { ... on NodeEntity { id name } }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Single(Probe.Invocations);
        result.MatchInlineSnapshot(
            $$"""
            {
              "data": {
                "node": {
                  "id": "{{id}}",
                  "name": "abc"
                }
              }
            }
            """);
    }
}
