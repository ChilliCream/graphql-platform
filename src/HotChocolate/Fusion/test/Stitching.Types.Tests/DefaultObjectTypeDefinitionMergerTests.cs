using CookieCrumble;
using HotChocolate.Stitching.Types.Bindings;

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
            out var a);

        ObjectTypeDefinition.TryParse(
            @"type Foo {
                b: String
            }",
            "Def",
            out var b);

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

    [Fact]
    public void Same_Field_In_Each_Schema_Version()
    {
        // arrange
        ObjectTypeDefinition.TryParse(
            @"type Foo {
                a: String
            }",
            "Abc",
            out var a);

        ObjectTypeDefinition.TryParse(
            @"type Foo {
                a: String
            }",
            "Def",
            out var b);

        // act
        var merger = new DefaultObjectTypeDefinitionMerger();
        merger.MergeInto(a!, b!);

        // assert
        Assert.Collection(
            b!.Bindings,
            t =>
            {
                Assert.Equal("Def", Assert.IsType<SourceBinding>(t).Source);
                Assert.Equal("Foo.a", Assert.IsType<SourceBinding>(t).Target.ToString());
            },
            t =>
            {
                Assert.Equal("Abc", Assert.IsType<SourceBinding>(t).Source);
                Assert.Equal("Foo.a", Assert.IsType<SourceBinding>(t).Target.ToString());
            });
        b.Definition.ToString().MatchSnapshot();
    }
}
