using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class ResolverCompositionTests(ITestOutputHelper output) : CompositionTestBase(output)
{
    [Fact]
    public async Task Ensure_Node_Resolver_Only_Picked_If_Needed()
        => await Succeed(FileResource.Open("test1.graphql"));

    [Fact]
    public async Task Merge_Meta_Data_Correctly()
        => await Succeed(
            FileResource.Open("test1.graphql"),
            [FileResource.Open("test1.extensions.graphql"),]);
}
