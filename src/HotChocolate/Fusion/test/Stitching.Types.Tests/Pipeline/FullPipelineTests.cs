using CookieCrumble;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline;

public class FullPipelineTests
{
    [Fact]
    public async Task Apply_Local_Remove()
    {
        // arrange
        var pipeline = CreatePipeline();

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
        context.Documents.Single().SyntaxTree.MatchSnapshot();
    }

    private MergeSchema CreatePipeline()
        => SchemaMergePipelineBuilder.CreateDefaultPipeline();

}
