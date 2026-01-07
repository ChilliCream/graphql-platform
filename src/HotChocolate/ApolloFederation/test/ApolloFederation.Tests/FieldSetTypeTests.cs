using System.Text.Json;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Language;
using HotChocolate.Text.Json;
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
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new FieldSetType();
        const string selection = "a b c d e(d: $b)";
        var serialized = new StringValueNode(selection);

        // act
        var selectionSetObject = type.CoerceInputLiteral(serialized);

        // assert
        var selectionSet = Assert.IsType<SelectionSetNode>(selectionSetObject);
        Assert.Equal(5, selectionSet.Selections.Count);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new FieldSetType();
        var serialized = new StringValueNode("1");

        // act
        void Action() => type.CoerceInputLiteral(serialized);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputValue()
    {
        // arrange
        var type = new FieldSetType();

        var inputValue = JsonDocument.Parse(
            """
            "a b c d e(d: $b)"
            """);

        // act
        var selectionSetObject = type.CoerceInputValue(inputValue.RootElement, null!);

        // assert
        var selectionSet = Assert.IsType<SelectionSetNode>(selectionSetObject);
        Assert.Equal(5, selectionSet.Selections.Count);
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new FieldSetType();
        var inputValue = JsonDocument.Parse("1").RootElement;

        // act
        void Action() => type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        // arrange
        var type = new FieldSetType();
        const string selection = "a b c d e(d: $b)";
        var selectionSet = Syntax.ParseSelectionSet(Braces(selection));

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(selectionSet, resultValue);

        // assert
        resultValue.MatchSnapshot();
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new FieldSetType();

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        void Action() => type.CoerceOutputValue(1, resultValue);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void ValueToLiteral()
    {
        // arrange
        var type = new FieldSetType();
        const string selection = "a b c d e(d: $b)";
        var selectionSet = Syntax.ParseSelectionSet(Braces(selection));

        // act
        var valueSyntax = type.ValueToLiteral(selectionSet);

        // assert
        Assert.Equal(
            selection,
            Assert.IsType<StringValueNode>(valueSyntax).Value);
    }

    [Fact]
    public void ValueToLiteral_Invalid_Format()
    {
        // arrange
        var type = new FieldSetType();

        // act
        Action action = () => type.ValueToLiteral(1);

        // assert
        Assert.Throws<LeafCoercionException>(action);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new FieldSetType();
        const string selection = "a b c d e(d: $b)";
        var stringValueNode = new StringValueNode(selection);

        // act
        var valueSyntax = type.CoerceInputLiteral(stringValueNode);

        // assert
        var parsedSelectionSet = Assert.IsType<SelectionSetNode>(valueSyntax);
        Assert.Equal(5, parsedSelectionSet.Selections.Count);
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new FieldSetType();

        // act
        void Action() => type.CoerceInputLiteral(new StringValueNode("1"));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    private static string Braces(string s) => $"{{ {s} }}";
}
