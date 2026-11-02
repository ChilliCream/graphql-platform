using HotChocolate.Language;

namespace HotChocolate.Types;

public class DirectiveLocationUtilsTests
{
    [Fact]
    public void Parse_DirectiveDefinitionLocation_MapsToFlagsValue()
    {
        // arrange
        var locations = new[]
        {
            new NameNode(Language.DirectiveLocation.DirectiveDefinition.Value)
        };

        // act
        var parsed = DirectiveLocationUtils.Parse(locations);

        // assert
        Assert.Equal(DirectiveLocation.DirectiveDefinition, parsed);
    }

    [Fact]
    public void Format_DirectiveDefinitionLocation_MapsToSyntaxLocation()
    {
        // act
        var formatted = DirectiveLocation.DirectiveDefinition.Format();

        // assert
        Assert.Equal(Language.DirectiveLocation.DirectiveDefinition, formatted);
    }

    [Fact]
    public void ToNameNodes_DirectiveDefinitionLocation_YieldsLocationName()
    {
        // act
        var names = DirectiveLocation.DirectiveDefinition.ToNameNodes();

        // assert
        var name = Assert.Single(names);
        Assert.Equal("DIRECTIVE_DEFINITION", name.Value);
    }
}
