using ChilliCream.Testing;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class ResolverCompositionTests(ITestOutputHelper output) : CompositionTestBase(output)
{
    [Fact]
    public async Task Ensure_Node_Resolver_Only_Picked_If_Needed()
        => await Succeed(FileResource.Open("test1.graphql"));
}
