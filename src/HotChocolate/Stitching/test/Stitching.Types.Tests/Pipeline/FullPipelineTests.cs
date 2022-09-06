using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Pipeline.ApplyExtensions;
using HotChocolate.Stitching.Types.Pipeline.ApplyMissingBindings;
using HotChocolate.Stitching.Types.Pipeline.ApplyRenaming;
using HotChocolate.Stitching.Types.Pipeline.PrepareDocuments;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Types.Pipeline;

public class FullPipelineTests
{
    [Fact]
    public async Task Apply_Local_Remove()
    {
        // arrange
        MergeSchema pipeline = CreatePipeline();

        var serviceA = new ServiceConfiguration(
            "ServiceA",
            Utf8GraphQLParser.Parse(@"
                type Foo {
                    abc: String @rename(to: ""xyz"")
                    zzz: Float
                }

                extend type Bar {
                    def: String
                }"));

        var serviceB = new ServiceConfiguration(
            "ServiceB",
            Utf8GraphQLParser.Parse(@"
                type Bar {
                    abc: String
                }

                extend type Foo {
                    abc: Int
                    zzz: Float
                }"));

        var configurations = new List<ServiceConfiguration> { serviceA, serviceB };
        var context = new SchemaMergeContext(configurations);

        // act
        await pipeline(context);

        // assert
        context.Documents.Single().SyntaxTree.ToString().MatchSnapshot();
    }

    private MergeSchema CreatePipeline()
        => SchemaMergePipelineBuilder.CreateDefaultPipeline();

}
