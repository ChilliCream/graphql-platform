using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion.Planning;

public class RequestPlannerTests
{
    [Fact]
    public async Task Accounts_And_Reviews_Query_Plan_1()
    {
        // arrange
        var serviceDefinition = FileResource.Open("AccountsAndReviews.graphql");
        var document = Parse(serviceDefinition);

        var context = FusionTypeNames.From(document);
        var rewriter = new FusionGraphConfigurationToSchemaRewriter();
        var rewritten = rewriter.Rewrite(document, new(context))!;

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(rewritten.ToString())
            .UseField(n => n)
            .BuildSchemaAsync();

        var serviceConfig = FusionGraphConfiguration.Load(serviceDefinition);

        var request = Parse(
            """
            query {
              users {
                name
                reviews {
                  body
                  author {
                    name
                  }
                }
              }
            }
            """);

        var operationCompiler = new OperationCompiler(new());
        var operation = operationCompiler.Compile(
            "abc",
            (OperationDefinitionNode)request.Definitions.First(),
            schema.QueryType,
            request,
            schema);

        // act
        var queryPlanContext = new QueryPlanContext(operation);
        var requestPlaner = new RequestPlanner(serviceConfig);
        var requirementsPlaner = new RequirementsPlanner();
        var executionPlanBuilder = new ExecutionPlanBuilder(serviceConfig, schema);

        requestPlaner.Plan(queryPlanContext);
        requirementsPlaner.Plan(queryPlanContext);
        var queryPlan = executionPlanBuilder.Build(queryPlanContext);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(request, "User Request");
        snapshot.Add(queryPlan, "Query Plan");
        await snapshot.MatchAsync();
    }
}
