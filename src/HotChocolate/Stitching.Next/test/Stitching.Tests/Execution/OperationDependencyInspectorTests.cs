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

public class OperationDependencyInspectorTests
{
    [Fact]
    public async Task Metadata_GetSource_UserId_UserName()
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
            @"{
                reviews {
                    body
                    user {
                        name
                        username
                    }
                    product {
                        upc
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

        // act
        var context = new RemoteQueryPlanerContext();
        var operationInspector = new OperationDependencyInspector(metadataDb);
        operationInspector.Inspect(operation, context);

        // assert
        Assert.Collection(
            context.RequiredFields.First().Value,
            t => Assert.Equal("id", t.Value));

    }
}
