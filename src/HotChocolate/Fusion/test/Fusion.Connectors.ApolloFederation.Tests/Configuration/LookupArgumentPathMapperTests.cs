using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Language;

namespace HotChocolate.Fusion.Configuration;

public sealed class LookupArgumentPathMapperTests
{
    [Fact]
    public void Map_Should_ReturnEmptyString_When_ObjectValueSelectionNode()
    {
        // arrange
        var selection = FieldSelectionMapParser.Parse("{ id name }");

        // act
        var result = LookupArgumentPathMapper.Map(selection);

        // assert
        Assert.IsType<ObjectValueSelectionNode>(selection);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Map_Should_ReturnPathName_When_PathNode()
    {
        // arrange
        var selection = FieldSelectionMapParser.Parse("id");

        // act
        var result = LookupArgumentPathMapper.Map(selection);

        // assert
        Assert.IsType<PathNode>(selection);
        Assert.Equal("id", result);
    }

    [Fact]
    public void Map_Should_ReturnPathName_When_PathObjectValueSelectionNode()
    {
        // arrange
        var selection = FieldSelectionMapParser.Parse("category.{ id name }");

        // act
        var result = LookupArgumentPathMapper.Map(selection);

        // assert
        Assert.IsType<PathObjectValueSelectionNode>(selection);
        Assert.Equal("category", result);
    }

    [Fact]
    public void Map_Should_ReturnPathName_When_PathListValueSelectionNode()
    {
        // arrange
        var selection = FieldSelectionMapParser.Parse("products[{ id pid }]");

        // act
        var result = LookupArgumentPathMapper.Map(selection);

        // assert
        Assert.IsType<PathListValueSelectionNode>(selection);
        Assert.Equal("products", result);
    }

    [Fact]
    public void Map_Should_Throw_When_UnsupportedNode()
    {
        // arrange
        // ListValueSelectionNode is a valid IValueSelectionNode subtype that the
        // Apollo Federation transformer never emits, so it falls through to the
        // unsupported branch.
        var inner = FieldSelectionMapParser.Parse("id");
        var selection = new ListValueSelectionNode(inner);

        // act
        void Act() => LookupArgumentPathMapper.Map(selection);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Contains("ListValueSelectionNode", exception.Message);
    }
}
