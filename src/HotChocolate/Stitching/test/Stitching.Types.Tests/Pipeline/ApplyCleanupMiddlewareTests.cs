using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Pipeline.ApplyCleanup;
using HotChocolate.Stitching.Types.Pipeline.PrepareDocuments;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Types.Pipeline;

public class ApplyCleanupMiddlewareTests
{
    [Fact]
    public async Task Apply_TypeCleanup()
    {
        // arrange
        MergeSchema pipeline = CreatePipeline();

        var service = new ServiceConfiguration(
            "abc",
            documents: new[]
            {
                Utf8GraphQLParser.Parse(@"
                type Bar {
                    abc: String
                }"),
                Utf8GraphQLParser.Parse(@"
                type Foo {
                }")
            });

        var configurations = new List<ServiceConfiguration> { service };
        var context = new SchemaMergeContext(configurations);

        // act
        await pipeline.Invoke(context);

        // assert
        context.Documents.Single().SyntaxTree.ToString().MatchSnapshot();
    }

    private MergeSchema CreatePipeline()
        => new SchemaMergePipelineBuilder()
            .Use(next =>
            {
                var middleware = new PrepareDocumentsMiddleware(next);
                return context => middleware.InvokeAsync(context);
            })
            .Use(next =>
            {
                var middleware = new ApplyCleanupMiddleware(next);
                return context => middleware.InvokeAsync(context);
            })
            .Compile();
}
