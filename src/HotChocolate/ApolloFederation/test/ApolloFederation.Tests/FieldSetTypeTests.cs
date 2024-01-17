using HotChocolate.ApolloFederation.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.ApolloFederation.FederationTypeNames;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.ApolloFederation;

public class FieldSetTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new FieldSetType();

        // assert
        Assert.Equal(FieldSetType_Name, type.Name);
    }

    [Fact]
    public void Deserialize()
    {
        // arrange
        var type = new FieldSetType();
        const string serialized = "a b c d e(d: $b)";

        // act
        var selectionSet = type.Deserialize(serialized);

        // assert
        Assert.IsType<SelectionSetNode>(selectionSet);
    }

    [Fact]
    public void Deserialize_Invalid_Format()
    {
        // arrange
        var type = new FieldSetType();
        const string serialized = "1";

        // act
        void Action() => type.Deserialize(serialized);

        // assert
        Assert.Throws<SerializationException>(Action);
    }

    [Fact]
    public void TryDeserialize()
    {
        // arrange
        var type = new FieldSetType();
        const string serialized = "a b c d e(d: $b)";

        // act
        var success = type.TryDeserialize(serialized, out var selectionSet);

        // assert
        Assert.True(success);
        Assert.IsType<SelectionSetNode>(selectionSet);
    }

    [Fact]
    public void TryDeserialize_Null()
    {
        // arrange
        var type = new FieldSetType();

        // act
        var success = type.TryDeserialize(null, out var selectionSet);

        // assert
        Assert.True(success);
        Assert.Null(selectionSet);
    }

    [Fact]
    public void TryDeserialize_Invalid_Syntax()
    {
        // arrange
        var type = new FieldSetType();
        const string serialized = "1";

        // act
        var success = type.TryDeserialize(serialized, out var selectionSet);

        // assert
        Assert.False(success);
        Assert.Null(selectionSet);
    }

    [Fact]
    public void TryDeserialize_Invalid_Type()
    {
        // arrange
        var type = new FieldSetType();
        const int serialized = 1;

        // act
        var success = type.TryDeserialize(serialized, out var selectionSet);

        // assert
        Assert.False(success);
        Assert.Null(selectionSet);
    }

    [Fact]
    public void Serialize()
    {
        // arrange
        var type = new FieldSetType();
        const string selection = "a b c d e(d: $b)";
        var selectionSet = Syntax.ParseSelectionSet(Braces(selection));

        // act
        var serialized = type.Serialize(selectionSet);

        // assert
        Assert.Equal(selection, serialized);
    }

    [Fact]
    public void Serialize_Invalid_Format()
    {
        // arrange
        var type = new FieldSetType();

        // act
        void Action() => type.Serialize(1);

        // assert
        Assert.Throws<SerializationException>(Action);
    }

    [Fact]
    public void TrySerialize()
    {
        // arrange
        var type = new FieldSetType();
        const string selection = "a b c d e(d: $b)";
        var selectionSet = Syntax.ParseSelectionSet(Braces(selection));

        // act
        var success = type.TrySerialize(selectionSet, out var serialized);

        // assert
        Assert.True(success);
        Assert.Equal(selection, serialized);
    }

    [Fact]
    public void TrySerialize_Invalid_Format()
    {
        // arrange
        var type = new FieldSetType();

        // act
        var success = type.TrySerialize(1, out var serialized);

        // assert
        Assert.False(success);
        Assert.Null(serialized);
    }

    private static string Braces(string s) => $"{{ {s} }}";

    [Fact]
    public void ParseValue()
    {
        // arrange
        var type = new FieldSetType();
        const string selection = "a b c d e(d: $b)";
        var selectionSet = Syntax.ParseSelectionSet(Braces(selection));

        // act
        var valueSyntax = type.ParseValue(selectionSet);

        // assert
        Assert.Equal(
            selection,
            Assert.IsType<StringValueNode>(valueSyntax).Value);
    }

    [Fact]
    public void ParseValue_Null()
    {
        // arrange
        var type = new FieldSetType();

        // act
        var valueSyntax = type.ParseValue(null);

        // assert
        Assert.IsType<NullValueNode>(valueSyntax);
    }

    [Fact]
    public void ParseValue_InvalidValue()
    {
        // arrange
        var type = new FieldSetType();

        // act
        void Action() => type.ParseValue(1);

        // assert
        Assert.Throws<SerializationException>(Action);
    }
}
