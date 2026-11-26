namespace HotChocolate.Types.Mutable;

public class OutputFieldDefinitionCollectionTests
{
    [Fact]
    public void Add_Should_SetDeclaringMember_When_OwnerIsObjectType()
    {
        // arrange
        var type = new MutableObjectTypeDefinition("Foo");
        var field = new MutableOutputFieldDefinition("bar");

        // act
        type.Fields.Add(field);

        // assert
        Assert.Same(type, field.DeclaringMember);
        Assert.Equal("Foo.bar", field.Coordinate.ToString());
    }

    [Fact]
    public void Add_Should_SetDeclaringMember_When_OwnerIsInterfaceType()
    {
        // arrange
        var type = new MutableInterfaceTypeDefinition("Foo");
        var field = new MutableOutputFieldDefinition("bar");

        // act
        type.Fields.Add(field);

        // assert
        Assert.Same(type, field.DeclaringMember);
        Assert.Equal("Foo.bar", field.Coordinate.ToString());
    }
}
