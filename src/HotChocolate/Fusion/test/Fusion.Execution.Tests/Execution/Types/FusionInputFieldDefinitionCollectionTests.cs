using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Execution.Types;

public class FusionInputFieldDefinitionCollectionTests
{
    [Fact]
    public void TryGetField_Should_ReturnMatchingField_When_SmallCollectionContainsName()
    {
        // arrange
        var collection = new FusionInputFieldDefinitionCollection(
            [Field(0, "id"), Field(1, "name"), Field(2, "price")]);

        // act
        var found = collection.TryGetField("name"u8, out var field);

        // assert
        Assert.True(found);
        Assert.Equal("name", field!.Name);
    }

    [Fact]
    public void TryGetField_Should_ReturnMatchingField_When_LargeCollectionContainsName()
    {
        // arrange
        var collection = new FusionInputFieldDefinitionCollection(
            [.. Enumerable.Range(0, 20).Select(i => Field(i, "f" + new string('x', 19 - i)))]);

        // act
        var found = collection.TryGetField("fxxxxx"u8, out var field);

        // assert
        Assert.True(found);
        Assert.Equal(14, field!.Index);
    }

    [Fact]
    public void TryGetField_Should_ReturnFalse_When_FieldIsInaccessible()
    {
        // arrange
        var collection = new FusionInputFieldDefinitionCollection(
            [Field(0, "visible"), Field(1, "hidden", isInaccessible: true)]);

        // act
        var found = collection.TryGetField("hidden"u8, out var field);

        // assert
        Assert.False(found);
        Assert.Null(field);
    }

    [Fact]
    public void TryGetField_Should_ReturnFalse_When_NameIsJsonEscaped()
    {
        // arrange
        var collection = new FusionInputFieldDefinitionCollection([Field(0, "id")]);

        // act
        var found = collection.TryGetField("\\u0069d"u8, out var field);

        // assert
        Assert.False(found);
        Assert.Null(field);
    }

    private static FusionInputFieldDefinition Field(int index, string name, bool isInaccessible = false)
        => new(index, name, description: null, defaultValue: null, deprecationReason: null, isInaccessible);
}
