using HotChocolate.Stitching.Types.Bindings;
using Xunit;

namespace HotChocolate.Stitching.Types;

public class DefaultObjectTypeDefinitionTests
{
    [Fact]
    public void TryParse()
    {
        var success =
            ObjectTypeDefinition.TryParse(
                @"type Foo {
                    a: String
                }",
                "Abc",
                out ObjectTypeDefinition? definition);

        Assert.True(success);
        Assert.NotNull(definition);
        Assert.Collection(
            definition!.Bindings,
            t => Assert.Equal("Abc", Assert.IsType<SourceBinding>(t).Source));
    }
}
