using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;
using static HotChocolate.Stitching.Execution.TestHelper;

namespace HotChocolate.Stitching.Execution;

public class VariableRewriterTests
{
    [Fact]
    public async Task Rewrite_Variables()
    {
        // arrange
        MergedSchema mergedSchema = CreateSchemaInfo();

        ISchema schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocument(mergedSchema.SchemaInfo.ToSchemaDocument())
            .UseField(_ => _)
            .BuildSchemaAsync();

        var metadataDb = new StitchingMetadataDb(
            mergedSchema.Sources,
            schema,
            mergedSchema.SchemaInfo);

        var query = Utf8GraphQLParser.Parse(
            @"query GetUserWithReviews($id: ID!) {
                userById(id: $id) {
                    name
                    username
                    reviews {
                        body
                    }
                }
            }");

        var operation = OperationCompiler.Compile(
            "abc",
            query,
            query.Definitions.OfType<OperationDefinitionNode>().First(),
            schema,
            schema.QueryType,
            new InputParser());

        NameString source = metadataDb.GetSource(operation.GetRootSelectionSet().Selections);

        var context = new OperationPlanerContext();
        var operationInspector = new OperationInspector(metadataDb);
        var operationPlaner = new OperationPlaner(metadataDb);

        operationInspector.Inspect(operation, context);
        QueryNode plan = operationPlaner.Build(operation, context);

        // act
        var variableRewriter = new VariableRewriter();
        foreach (QueryNode node in plan)
        {
            plan.Document = (DocumentNode)variableRewriter.Rewrite(plan.Document, context);
        }

        // assert
        MatchSnapshot(query, plan);
    }

    [Fact]
    public async Task Rewrite_Variables_With_Fragments()
    {
        // arrange
        MergedSchema mergedSchema = CreateSchemaInfo();

        ISchema schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocument(mergedSchema.SchemaInfo.ToSchemaDocument())
            .UseField(_ => _)
            .BuildSchemaAsync();

        var metadataDb = new StitchingMetadataDb(
            mergedSchema.Sources,
            schema,
            mergedSchema.SchemaInfo);

        var query = Utf8GraphQLParser.Parse(
            @"query GetUserWithReviews($id: ID!) {
                userById(id: $id) {
                    ... UserInfo
                }
            }
            
            fragment UserInfo on User {
                name
                username
                reviews {
                    ... on Review {
                        body
                    }
                }
            }");

        var operation = OperationCompiler.Compile(
            "abc",
            query,
            query.Definitions.OfType<OperationDefinitionNode>().First(),
            schema,
            schema.QueryType,
            new InputParser());

        NameString source = metadataDb.GetSource(operation.GetRootSelectionSet().Selections);

        var context = new OperationPlanerContext();
        var operationInspector = new OperationInspector(metadataDb);
        var operationPlaner = new OperationPlaner(metadataDb);

        operationInspector.Inspect(operation, context);
        QueryNode plan = operationPlaner.Build(operation, context);

        // act
        var variableRewriter = new VariableRewriter();
        foreach (QueryNode node in plan)
        {
            plan.Document = (DocumentNode)variableRewriter.Rewrite(plan.Document, context);
        }

        // assert
        MatchSnapshot(query, plan);
    }
}
