using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class ParameterExpressionBuilderTests
{
    [Fact]
    public async Task AddParameterExpressionBuilder_Should_NotExposeParameterAsArgument_When_ResolverIsSourceGenerated()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .AddParameterExpressionBuilder(
                static (IResolverContext ctx) => ctx.GetGlobalStateOrDefault<CurrentUser>("currentUser")!)
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var field = schema.Types.GetType<ObjectType>("Mutation").Fields["createExport"];

        // assert
        // The custom expression builder handles the currentUser parameter, so only the
        // name parameter must remain as a GraphQL argument on the field.
        field.ToString().MatchInlineSnapshot("createExport(name: String!): String!");
    }

    [Fact]
    public async Task AddParameterExpressionBuilder_Should_InjectValueFromExpression_When_ResolverIsSourceGenerated()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .AddParameterExpressionBuilder(
                static (IResolverContext ctx) => ctx.GetGlobalStateOrDefault<CurrentUser>("currentUser")!)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        // The resolver combines the injected currentUser with the name argument, so the
        // result proves the source-generated binding executed the custom expression.
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("""mutation { createExport(name: "sales") }""")
                .SetGlobalState("currentUser", new CurrentUser("alice"))
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "createExport": "alice:sales"
              }
            }
            """);
    }

    [Fact]
    public async Task AddParameterExpressionBuilder_Should_RejectParameterInfoPredicate_When_ResolverIsSourceGenerated()
    {
        // arrange
        var services = new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .AddParameterExpressionBuilder(
                static (IResolverContext ctx) => ctx.GetGlobalStateOrDefault<CurrentUser>("currentUser")!,
                static parameter => parameter.Name == "currentUser");

        // act
        var exception = await Assert.ThrowsAsync<SchemaException>(
            () => services
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken)
                .AsTask());

        // assert
        var predicateException = Assert.IsType<SchemaException>(exception.Errors.Single().Exception);
        Assert.Equal(
            "Custom parameter expression builders that use a ParameterInfo predicate cannot be "
            + "used with source-generated resolvers. Omit the canHandle predicate to match "
            + "parameters by type.",
            predicateException.Errors.Single().Message);
    }

    [Fact]
    public async Task AddParameterExpressionBuilder_Should_OverrideBuiltInBindings_When_ResolverIsSourceGenerated()
    {
        // arrange
        using var source = new CancellationTokenSource();
        source.Cancel();

        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .AddParameterExpressionBuilder(
                static (IResolverContext ctx) =>
                    ctx.GetGlobalState<CancellationToken>("customCancellationToken"))
            .AddParameterExpressionBuilder(static _ => (IResolverContext)null!)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ areCustomParametersInjected }")
                .SetGlobalState("customCancellationToken", source.Token)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "areCustomParametersInjected": true
              }
            }
            """);
    }
}
