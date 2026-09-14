using HotChocolate.Authorization;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class AuthorizationBatchTests : BatchScenarioTests
{
    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute),
        SourceGenerated = new Declaration(ConfigureSourceGenerated),
        Fluent = new Declaration(ConfigureFluent)
    };

    [Theory]
    [BatchMatrix]
    public async Task Authorize_Should_Deny_When_BeforeResolverPolicyFails(DeclarationStyle style)
    {
        // arrange
        AuthHandler.Resolver = (_, directive) => directive.Policy == "READ_SECRET_BEFORE"
            ? AuthorizeResult.NotAllowed
            : AuthorizeResult.Allowed;
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteAsync(
            executor, "{ a: beforeSecretById(id: 1) { value } }", TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(Probe.Invocations);
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "a"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": {
                "a": null
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task Authorize_Should_Deny_When_AfterResolverPolicyFails(DeclarationStyle style)
    {
        // arrange
        AuthHandler.Resolver = (_, directive) => directive.Policy == "READ_SECRET_AFTER"
            ? AuthorizeResult.NotAllowed
            : AuthorizeResult.Allowed;
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteAsync(
            executor, "{ a: afterSecretById(id: 1) { value } }", TestContext.Current.CancellationToken);

        // assert
        Assert.Single(Probe.Invocations);
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "a"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": {
                "a": null
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task Authorize_Should_Deny_When_ReturnedObjectTypeIsProtected(DeclarationStyle style)
    {
        // arrange
        AuthHandler.Resolver = (_, directive) => directive.Policy == "READ_FRIEND"
            ? AuthorizeResult.NotAllowed
            : AuthorizeResult.Allowed;
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteAsync(
            executor, "{ users { friend { id } } }", TestContext.Current.CancellationToken);

        // assert
        // The type-level policy defaults to BeforeResolver, so the batch resolver never runs.
        Assert.Empty(Probe.Invocations);
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "path": [
                    "users",
                    0,
                    "friend"
                  ],
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ],
              "data": {
                "users": [
                  {
                    "friend": null
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task Authorize_Should_Deny_When_ValidationPolicyFails(DeclarationStyle style)
    {
        // arrange
        AuthHandler.Validation = (_, directive) => directive.Policy == "READ_THING"
            ? AuthorizeResult.NotAllowed
            : AuthorizeResult.Allowed;
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteAsync(
            executor, "{ a: thingById(id: 1) { id } }", TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(Probe.Invocations);
        Assert.Equal(401, result.ContextData![ExecutionContextData.HttpStatusCode]);
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ]
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task Authorize_Should_Resolve_When_PolicyAllows(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteAsync(
            executor, "{ a: allowedById(id: 1) { id } }", TestContext.Current.CancellationToken);

        // assert
        Assert.Single(Probe.Invocations);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "a": {
                  "id": 1
                }
              }
            }
            """);
    }
}
