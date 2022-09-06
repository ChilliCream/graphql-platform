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

public class ApplyMissingBindingsMiddlewareTests
{
    [Fact]
    public async Task Apply_Missing_Bindings()
    {
        // arrange
        MergeSchema pipeline = CreatePipeline();

        var service = new ServiceConfiguration(
            "SchemaName",
            Utf8GraphQLParser.Parse(@"
                type Foo implements IFoo & IFooExt {
                    abc: String
                    def: String
                }

                interface IFoo {
                    abc: String
                }

                interface IFooExt implements IFoo {
                    abc: String
                    def: String
                }

                extend interface IFoo {
                    abc: String @rename(to: ""newName"")
                }"));

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
                var middleware = new ApplyExtensionsMiddleware(next);
                return context => middleware.InvokeAsync(context);
            })
            .Use(next =>
            {
                var middleware = new ApplyRenamingMiddleware(next);
                return context => middleware.InvokeAsync(context);
            })
            .Use(next =>
            {
                var middleware = new ApplyMissingBindingsMiddleware(next);
                return context => middleware.InvokeAsync(context);
            })
            .Compile();
}
