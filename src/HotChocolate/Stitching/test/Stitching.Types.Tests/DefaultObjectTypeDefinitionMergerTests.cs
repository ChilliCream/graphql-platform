using HotChocolate.Stitching.Types.Bindings;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Types;

public class DefaultObjectTypeDefinitionMergerTests
{
    [Fact]
    public void Merge_Simple_Type()
    {
        // arrange
        ObjectTypeDefinition.TryParse(
            @"type Foo {
                a: String
            }",
            "Abc",
            out ObjectTypeDefinition? a);

        ObjectTypeDefinition.TryParse(
            @"type Foo {
                b: String
            }",
            "Def",
            out ObjectTypeDefinition? b);

        // act
        var merger = new DefaultObjectTypeDefinitionMerger();
        merger.MergeInto(a!, b!);

        // assert
        Assert.Collection(
            b!.Bindings,
            t => Assert.Equal("Def", Assert.IsType<SourceBinding>(t).Source),
            t => Assert.Equal("Abc", Assert.IsType<SourceBinding>(t).Source));
        b.Definition.ToString().MatchSnapshot();
    }
}
