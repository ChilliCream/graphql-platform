namespace HotChocolate.Types.Mutable;

public class MutableDirectiveDefinitionTests
{
    [Fact]
    public void SetDeprecationReason_Should_MarkAsDeprecated()
    {
        // arrange
        var directiveDefinition = new MutableDirectiveDefinition("foo");

        // act
        directiveDefinition.DeprecationReason = "Use bar.";

        // assert
        Assert.True(directiveDefinition.IsDeprecated);
        Assert.Equal("Use bar.", directiveDefinition.DeprecationReason);
    }

    [Fact]
    public void IsDeprecated_Should_BeFalse_When_DeprecationReasonIsCleared()
    {
        // arrange
        var directiveDefinition = new MutableDirectiveDefinition("foo")
        {
            DeprecationReason = "Use bar."
        };

        // act
        directiveDefinition.DeprecationReason = null;

        // assert
        Assert.False(directiveDefinition.IsDeprecated);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetDeprecationReason_Should_ClearDeprecation_When_ValueIsEmptyOrWhiteSpace(string reason)
    {
        // arrange
        var directiveDefinition = new MutableDirectiveDefinition("foo");

        // act
        directiveDefinition.DeprecationReason = reason;

        // assert
        Assert.False(directiveDefinition.IsDeprecated);
        Assert.Null(directiveDefinition.DeprecationReason);
    }

    [Fact]
    public void Directives_Should_ContainAddedDirective()
    {
        // arrange
        var metaDefinition = new MutableDirectiveDefinition("meta");
        var directiveDefinition = new MutableDirectiveDefinition("foo");

        // act
        directiveDefinition.Directives.Add(new Directive(metaDefinition));

        // assert
        Assert.True(directiveDefinition.Directives.ContainsName("meta"));
    }

    [Fact]
    public void BuiltInDeprecatedDirective_Should_DeclareDirectiveDefinitionLocation()
    {
        // arrange
        var schema = new MutableSchemaDefinition();

        // act
        var deprecated = BuiltIns.Deprecated.Create(schema);

        // assert
        Assert.True(deprecated.Locations.HasFlag(DirectiveLocation.DirectiveDefinition));
    }
}
