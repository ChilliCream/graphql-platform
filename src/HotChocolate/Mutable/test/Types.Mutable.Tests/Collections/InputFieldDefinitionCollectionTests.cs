namespace HotChocolate.Types.Mutable;

public class InputFieldDefinitionCollectionTests
{
    [Fact]
    public void Add_Should_SetDeclaringMember_When_OwnerIsInputObjectType()
    {
        // arrange
        var type = new MutableInputObjectTypeDefinition("FooInput");
        var field = new MutableInputFieldDefinition("bar");

        // act
        type.Fields.Add(field);

        // assert
        Assert.Same(type, field.DeclaringMember);
        Assert.Equal("FooInput.bar", field.Coordinate.ToString());
    }

    [Fact]
    public void Add_Should_SetDeclaringMember_When_OwnerIsDirective()
    {
        // arrange
        var directive = new MutableDirectiveDefinition("foo");
        var argument = new MutableInputFieldDefinition("bar");

        // act
        directive.Arguments.Add(argument);

        // assert
        Assert.Same(directive, argument.DeclaringMember);
        Assert.Equal("@foo(bar:)", argument.Coordinate.ToString());
    }

    [Fact]
    public void Add_Should_SetDeclaringMember_When_OwnerIsOutputField()
    {
        // arrange
        var type = new MutableObjectTypeDefinition("Foo");
        var field = new MutableOutputFieldDefinition("bar");
        type.Fields.Add(field);
        var argument = new MutableInputFieldDefinition("baz");

        // act
        field.Arguments.Add(argument);

        // assert
        Assert.Same(field, argument.DeclaringMember);
        Assert.Equal("Foo.bar(baz:)", argument.Coordinate.ToString());
    }

    [Fact]
    public void Insert_Should_SetDeclaringMember_When_InsertedBeforeExistingField()
    {
        // arrange
        var type = new MutableInputObjectTypeDefinition("FooInput");
        type.Fields.Add(new MutableInputFieldDefinition("bar"));
        var field = new MutableInputFieldDefinition("baz");

        // act
        type.Fields.Insert(0, field);

        // assert
        Assert.Same(type, field.DeclaringMember);
        Assert.Equal("FooInput.baz", field.Coordinate.ToString());
    }

    [Fact]
    public void Indexer_Should_SetDeclaringMember_When_FieldIsReplaced()
    {
        // arrange
        var type = new MutableInputObjectTypeDefinition("FooInput");
        type.Fields.Add(new MutableInputFieldDefinition("bar"));
        var field = new MutableInputFieldDefinition("baz");

        // act
        type.Fields[0] = field;

        // assert
        Assert.Same(type, field.DeclaringMember);
        Assert.Equal("FooInput.baz", field.Coordinate.ToString());
    }
}
