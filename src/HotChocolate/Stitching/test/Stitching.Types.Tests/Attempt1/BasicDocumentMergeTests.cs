using System;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Stitching.Types.Attempt1.Coordinates;
using HotChocolate.Stitching.Types.Attempt1.Operations;
using HotChocolate.Stitching.Types.Attempt1.Traversal;
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

        DocumentDefinition document1 = SchemaNodeFactory.CreateDocument(
            new SchemaDatabase(),
            source1);

        DocumentDefinition document2 = SchemaNodeFactory.CreateDocument(
            new SchemaDatabase(),
            source2);

        SchemaDatabase schemaDatabase = new SchemaDatabase();
        DocumentDefinition documentDefinition = SchemaNodeFactory.CreateNewDocument(schemaDatabase);

        DefaultMergeOperationsProvider operationsProvider = new DefaultMergeOperationsProvider();
        operationsProvider.Apply(document1, documentDefinition);
        operationsProvider.Apply(document2, documentDefinition);

        var schemaOperations = new DefaultRewriteOperationsProvider();
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

    [Fact]
    public async Task MergeSubdocuments()
    {
        var subGraphDocument1 = Utf8GraphQLParser.Parse(@"
interface TestInterface_renamed @_hc_source(coordinate: ""TestInterface"") {
  foo_renamed: test2_renamed! @_hc_source(coordinate: ""TestInterface.foo"")
}

type test_renamed implements TestInterface_renamed @_hc_source(coordinate: ""Test"") {
  foo_renamed: test2_renamed! @_hc_source(coordinate: ""Test.foo"")
  id_renamed: String! @_hc_source(coordinate: ""Test.id"")
}

type test2_renamed @_hc_source(coordinate: ""Test2"") {
  test: test_renamed!
}
");

        var subGraphDocument2 = Utf8GraphQLParser.Parse(@"
interface TestInterface_renamed {
  foo_renamed: test2_renamed!
}

interface TestInterface {
  foo_renamed: test2!
}

type test_renamed implements TestInterface_renamed {
  foo_renamed: test2_renamed!
  id_renamed: String!
}

type test2_renamed {
  test: test_renamed!
}

type test2 {
  test: test_renamed!
}

");

        SchemaDatabase schemaDatabase1 = new SchemaDatabase("SubGraph1");
        DocumentDefinition subGraph1 = SchemaNodeFactory.CreateDocument(
            schemaDatabase1,
            subGraphDocument1);

        SchemaDatabase schemaDatabase2 = new SchemaDatabase("SubGraph2");
        DocumentDefinition subGraph2 = SchemaNodeFactory.CreateDocument(
            schemaDatabase2,
            subGraphDocument2);

        SchemaDatabase destinationDatabase = new SchemaDatabase();
        DocumentDefinition documentDefinition = SchemaNodeFactory.CreateNewDocument(destinationDatabase);
        destinationDatabase.Reindex(documentDefinition);

        DefaultMergeOperationsProvider mergeOperations = new DefaultMergeOperationsProvider();
        mergeOperations.Apply(subGraph1, documentDefinition);
        mergeOperations.Apply(subGraph2, documentDefinition);

        ISchemaNode renderedSchema = destinationDatabase.Root;
        var schema = renderedSchema.Definition.Print();
        schema.MatchSnapshot();
    }
}
