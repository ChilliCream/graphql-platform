using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Stitching.Types.Attempt1.Coordinates;
using HotChocolate.Stitching.Types.Attempt1.Operations;
using HotChocolate.Stitching.Types.Attempt1.Traversal;
using JetBrains.dotMemoryUnit;
using Snapshooter.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace HotChocolate.Stitching.Types.Attempt1;

public class BasicDocumentMergeTests
{
    private readonly ITestOutputHelper _helper;

    public BasicDocumentMergeTests(ITestOutputHelper helper)
    {
        _helper = helper;
        //DotMemoryUnitTestOutput.SetOutputMethod(str => _helper.WriteLine(str));
    }

    //[DotMemoryUnit(SavingStrategy = SavingStrategy.OnCheckFail, Directory = @"C:\Temp\HotChocolate")]
    [Fact]
    public void Test()
    {
        var totalAllocatedBytes = GC.GetTotalAllocatedBytes();
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

        SchemaDatabase schemaDatabase = new SchemaDatabase();
        DefaultMergeOperationsProvider operationsProvider = new DefaultMergeOperationsProvider();
        DefaultSyntaxNodeVisitor visitor = new DefaultSyntaxNodeVisitor(schemaDatabase, operationsProvider);

        var documentNode = new DocumentNode(new List<IDefinitionNode>(0));
        DocumentDefinition documentDefinition = SchemaNodeFactory.CreateDocument(
            schemaDatabase,
            documentNode);
        
        schemaDatabase.Reindex(documentDefinition);

        //visitor.Accept(source);
        visitor.Accept(source1);
        visitor.Accept(source2);

        var schemaOperations = new DefaultRewriteOperationsProvider(schemaDatabase);
        schemaOperations.Apply(documentDefinition);

        ISchemaNode renderedSchema = schemaDatabase.Root;
        var schema = renderedSchema.Definition.Print();

        //var totalAllocatedBytes2 = GC.GetTotalAllocatedBytes();
        //_helper.WriteLine($"{totalAllocatedBytes2 - totalAllocatedBytes}");
        //dotMemory.Check(_ =>
        //{
        //    Assert.True(false);
        //});

        schema.MatchSnapshot();

        //var serviceReference = new HttpService("Test", "https://localhost", new[] { new AllowBatchingFeature() });
        //var definition = new ServiceDefinition(serviceReference,
        //    new List<DocumentNode> { new(new List<IDefinitionNode>(0)) });

        //var transformer = new SchemaTransformer();
        //var result = await transformer.Transform(definition, new SchemaTransformationOptions());
        //var subGraph = result.SubGraph;
    }
}
