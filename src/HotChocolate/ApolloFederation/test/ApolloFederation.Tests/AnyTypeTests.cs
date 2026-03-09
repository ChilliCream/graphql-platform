using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using AnyType = HotChocolate.ApolloFederation.Types._AnyType;

namespace HotChocolate.ApolloFederation;

public class AnyTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new AnyType();

        // assert
        Assert.Equal(FederationTypeNames.AnyType_Name, type.Name);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new AnyType();
        var serialized = new ObjectValueNode(
            new ObjectFieldNode(AnyType.TypeNameField, "test"),
            new ObjectFieldNode("faa", "foo"),
            new ObjectFieldNode("foo", "bar"));

        // act
        var representationObject = type.CoerceInputLiteral(serialized);

        // assert
        var representation = Assert.IsType<Representation>(representationObject);

        Assert.Equal("test", representation.TypeName);
        Assert.Collection(representation.Data.Fields,
            node =>
            {
                Assert.Equal(
                    AnyType.TypeNameField,
                    node.Name.Value);

                Assert.Equal(
                    "test",
                    node.Value.Value);
            },
            node =>
            {
                Assert.Equal(
                    "faa",
                    node.Name.Value);

                Assert.Equal(
                    "foo",
                    node.Value.Value);
            },
            node =>
            {
                Assert.Equal(
                    "foo",
                    node.Name.Value);

                Assert.Equal(
                    "bar",
                    node.Value.Value);
            }
        );
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new AnyType();
        var serialized = new ObjectValueNode();

        // act
        void Action() => type.CoerceInputLiteral(serialized);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputValue()
    {
        // arrange
        var type = new AnyType();

        var inputValue = JsonDocument.Parse(
            """
            {
              "__typename": "test",
              "faa": "foo",
              "foo": "bar"
            }
            """);

        // act
        var representationObject = type.CoerceInputValue(inputValue.RootElement, null!);

        // assert
        var representation = Assert.IsType<Representation>(representationObject);

        Assert.Equal("test", representation.TypeName);
        Assert.Collection(representation.Data.Fields,
            node =>
            {
                Assert.Equal(
                    AnyType.TypeNameField,
                    node.Name.Value);

                Assert.Equal(
                    "test",
                    node.Value.Value);
            },
            node =>
            {
                Assert.Equal(
                    "faa",
                    node.Name.Value);

                Assert.Equal(
                    "foo",
                    node.Value.Value);
            },
            node =>
            {
                Assert.Equal(
                    "foo",
                    node.Name.Value);

                Assert.Equal(
                    "bar",
                    node.Value.Value);
            }
        );
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new AnyType();
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
        var type = new AnyType();
        var objectValueNode = new ObjectValueNode(
            new ObjectFieldNode(
                AnyType.TypeNameField,
                "test"
            ),
            new ObjectFieldNode(
                "foo",
                "bar"
            )
        );

        var representation = new Representation("test", objectValueNode);

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        void Error() => type.CoerceOutputValue(representation, resultValue);

        // assert
        Assert.Throws<NotSupportedException>(Error);
    }

    [Fact]
    public void ValueToLiteral()
    {
        // arrange
        var type = new AnyType();
        var objectValueNode = new ObjectValueNode(
            new ObjectFieldNode(
                AnyType.TypeNameField,
                "test"
            ),
            new ObjectFieldNode(
                "foo",
                "bar"
            )
        );
        var representation = new Representation("test", objectValueNode);

        // act
        var valueSyntax = type.ValueToLiteral(representation);

        // assert
        Assert.Equal(
            objectValueNode,
            Assert.IsType<ObjectValueNode>(valueSyntax),
            SyntaxComparer.BySyntax);
    }

    [Fact]
    public void ValueToLiteral_Invalid_Format()
    {
        // arrange
        var type = new AnyType();
        var objectValueNode = new ObjectValueNode(
            new ObjectFieldNode(
                AnyType.TypeNameField,
                "test"
            ),
            new ObjectFieldNode(
                "foo",
                "bar"
            )
        );

        // act
        Action action = () => type.ValueToLiteral(1);

        // assert
        Assert.Throws<LeafCoercionException>(action);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new AnyType();
        var objectValueNode = new ObjectValueNode(
            new ObjectFieldNode(
                AnyType.TypeNameField,
                "test"
            ),
            new ObjectFieldNode(
                "foo",
                "bar"
            )
        );

        // act
        var valueSyntax = type.CoerceInputLiteral(objectValueNode);

        // assert
        var parsedRepresentation = Assert.IsType<Representation>(valueSyntax);
        Assert.Equal("test", parsedRepresentation.TypeName);
        Assert.Equal(objectValueNode, parsedRepresentation.Data);
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new AnyType();

        // act
        void Action() => type.CoerceInputLiteral(new ObjectValueNode());

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }
}
