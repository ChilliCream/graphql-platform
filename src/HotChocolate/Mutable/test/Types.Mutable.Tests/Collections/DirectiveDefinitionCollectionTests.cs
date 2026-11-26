namespace HotChocolate.Types.Mutable;

public class DirectiveDefinitionCollectionTests
{
    [Fact]
    public void Clear_Should_RemoveOnlyDirectiveDefinitions_When_TypeHasSameName()
    {
        // arrange
        var schema = new MutableSchemaDefinition();
        var type = new MutableObjectTypeDefinition("Foo");
        schema.Types.Add(type);
        schema.DirectiveDefinitions.Add(new MutableDirectiveDefinition("Foo"));

        // act
        schema.DirectiveDefinitions.Clear();

        // assert
        Assert.Same(type, Assert.Single(schema.GetAllDefinitions()));
    }
}
