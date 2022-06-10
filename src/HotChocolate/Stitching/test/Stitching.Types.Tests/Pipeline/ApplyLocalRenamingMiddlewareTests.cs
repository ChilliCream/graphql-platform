using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Pipeline.ApplyExtensions;
using HotChocolate.Stitching.Types.Pipeline.ApplyRenaming;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Types.Pipeline;

public class ApplyLocalRenamingMiddlewareTests
{
    [Fact]
    public async Task Apply_Local_Rename()
    {
        // arrange
        MergeSchema pipeline =
            new SchemaMergePipelineBuilder()
                .Use(next =>
                {
                    var middleware = new ApplyExtensionsMiddleware(next);
                    return context => middleware.InvokeAsync(context);
                })
                .Use(next =>
                {
                    var middleware = new ApplyLocalRenamingMiddleware(next);
                    return context => middleware.InvokeAsync(context);
                })
                .Compile();

        var service = new ServiceConfiguration(
            "abc",
            Utf8GraphQLParser.Parse(@"
                type Foo {
                    abc: String
                }

                extend type Foo {
                    abc: String @rename(to: ""def"")
                    def: Int
                    ghi: Int @_hc_bind(to: ""bar"" as: ""baz"")
                }"));

        var configurations = new List<ServiceConfiguration> { service };
        var context = new SchemaMergeContext(configurations);

        // act
        await pipeline.Invoke(context);

        // assert
        context.Documents.Single().SyntaxTree.ToString().MatchSnapshot();
    }
}
