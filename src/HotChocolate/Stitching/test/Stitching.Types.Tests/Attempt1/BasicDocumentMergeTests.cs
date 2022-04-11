using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Types;

public class BasicDocumentMergeTests
{
    [Fact]
    public void Test()
    {
        DocumentNode source1 = Utf8GraphQLParser.Parse(@"
            interface TestInterface {
              foo: Test!
            }

            extend type Test implements TestInterface {
              foo: Test! @rename(test: 2)
            }
            ",
            ParserOptions.NoLocation);

        DocumentNode source2 = Utf8GraphQLParser.Parse(@"
            type Test {
              id: String! @rename
              foo: String! @rename(test: 1)
            }

            type Test2 {
              test: Test!
            }
            ",
            ParserOptions.NoLocation);

        DocumentDefinition target = new DocumentDefinition();
        DefaultSyntaxNodeVisitor visitor = new DefaultSyntaxNodeVisitor(target, new DefaultOperationProvider());
        source1.Accept(visitor);
        source2.Accept(visitor);

        var schemaNode = new DocumentNode(target.Definition.Select(x => x.Definition).OfType<IDefinitionNode>().ToList());

        schemaNode.Print().MatchSnapshot();

        //var serviceReference = new HttpService("Test", "https://localhost", new[] { new AllowBatchingFeature() });
        //var definition = new ServiceDefinition(serviceReference,
        //    new List<DocumentNode> { new(new List<IDefinitionNode>(0)) });

        //var transformer = new SchemaTransformer();
        //var result = await transformer.Transform(definition, new SchemaTransformationOptions());
        //var subGraph = result.SubGraph;
    }
}
