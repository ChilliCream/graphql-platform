using HotChocolate.Utilities;

namespace HotChocolate.Types.Directives;

public class SpecedDirectiveTypeTests : TypeTestBase
{
    [Fact]
    public void EnsureSkipDirectiveIsAvailable()
    {
        // arrange
        var schema = CreateSchema(b => { });

        // act
        var directive =
            schema.DirectiveTypes.FirstOrDefault(
                t => t.Name.EqualsOrdinal("skip"));

        // assert
        Assert.NotNull(directive);
        Assert.IsType<SkipDirectiveType>(directive);
        Assert.Equal("skip", directive.Name);
        Assert.Collection(directive.Arguments,
            t =>
            {
                Assert.Equal("if", t.Name);
                Assert.IsType<NonNullType>(t.Type);
                Assert.IsType<BooleanType>(((NonNullType)t.Type).Type);
            });
        Assert.Collection(directive.Locations.AsEnumerable(),
            t => Assert.Equal(DirectiveLocation.Field, t),
            t => Assert.Equal(DirectiveLocation.FragmentSpread, t),
            t => Assert.Equal(DirectiveLocation.InlineFragment, t));
    }

    [Fact]
    public void EnsureIncludeDirectiveIsAvailable()
    {
        // arrange
        var schema = CreateSchema(b => { });

        // act
        var directive =
            schema.DirectiveTypes.FirstOrDefault(
                t => t.Name.EqualsOrdinal("include"));

        // assert
        Assert.NotNull(directive);
        Assert.IsType<IncludeDirectiveType>(directive);
        Assert.Equal("include", directive.Name);
        Assert.Collection(directive.Arguments,
            t =>
            {
                Assert.Equal("if", t.Name);
                Assert.IsType<NonNullType>(t.Type);
                Assert.IsType<BooleanType>(((NonNullType)t.Type).Type);
            });
        Assert.Collection(directive.Locations.AsEnumerable(),
            t => Assert.Equal(DirectiveLocation.Field, t),
            t => Assert.Equal(DirectiveLocation.FragmentSpread, t),
            t => Assert.Equal(DirectiveLocation.InlineFragment, t));
    }
}
