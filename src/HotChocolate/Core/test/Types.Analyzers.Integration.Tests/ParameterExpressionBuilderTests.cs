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
}
