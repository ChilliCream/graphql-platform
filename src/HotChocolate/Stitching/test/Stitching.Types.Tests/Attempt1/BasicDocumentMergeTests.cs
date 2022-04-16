using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Stitching.Types.Attempt1.Coordinates;
using HotChocolate.Stitching.Types.Attempt1.Operations;
using HotChocolate.Stitching.Types.Attempt1.Traversal;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Types.Attempt1;

public class BasicDocumentMergeTests
{
    [Fact]
    public void Test()
    {
        DocumentNode source = Utf8GraphQLParser.Parse(@"
            interface TestInterface {
              foo: Test2!
            }
        ");

        DocumentNode source1 = Utf8GraphQLParser.Parse(@"
            interface TestInterface @rename(name: ""TestInterface_renamed"") {
              foo: Test2! @rename(name: ""foo_renamed"")
            }

            extend type Test implements TestInterface {
              foo: Test2!
            }
            ",
            ParserOptions.NoLocation);

        DocumentNode source2 = Utf8GraphQLParser.Parse(@"
            type Test @rename(name: ""test_renamed"") {
              id: String! @rename(name: ""id_renamed"")
            }

            type Test2 @rename(name: ""test2_renamed"") {
              test: Test!
            }
            ",
            ParserOptions.NoLocation);

        DefaultOperationProvider operationProvider = new DefaultOperationProvider();
        SchemaNodeFactory schemaNodeFactory = new SchemaNodeFactory();
        SchemaDatabase schemaDatabase = new SchemaDatabase(schemaNodeFactory);
        DefaultSyntaxNodeVisitor visitor = new DefaultSyntaxNodeVisitor(schemaDatabase, operationProvider);

        var documentNode = new DocumentNode(new List<IDefinitionNode>(0));
        var documentDefinition = new DocumentDefinition(schemaDatabase.Add, documentNode);
        schemaDatabase.Reindex(documentDefinition);

        //visitor.Accept(source);
        visitor.Accept(source1);
        visitor.Accept(source2);

        var operations =
            new List<ISchemaNodeRewriteOperation> { new RenameTypeOperation(), new RenameFieldOperation() };

        var schemaOperations = new SchemaOperations(operations, schemaDatabase);
        schemaOperations.Apply(documentDefinition);

        ISchemaNode renderedSchema = schemaDatabase.Root;
        var schema = renderedSchema.Definition.Print();

        schema.MatchSnapshot();

        //var serviceReference = new HttpService("Test", "https://localhost", new[] { new AllowBatchingFeature() });
        //var definition = new ServiceDefinition(serviceReference,
        //    new List<DocumentNode> { new(new List<IDefinitionNode>(0)) });

        //var transformer = new SchemaTransformer();
        //var result = await transformer.Transform(definition, new SchemaTransformationOptions());
        //var subGraph = result.SubGraph;
    }
}
