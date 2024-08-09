using HotChocolate.Language;
using HotChocolate.Types;
using AnyType = HotChocolate.ApolloFederation.Types._AnyType;

namespace HotChocolate.ApolloFederation;

public class _AnyTypeTests
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
    public void Deserialize()
    {
        // arrange
        var type = new AnyType();
        var serialized = new ObjectValueNode(
            new ObjectFieldNode(AnyType.TypeNameField, "test"),
            new ObjectFieldNode("faa", "foo"),
            new ObjectFieldNode("foo", "bar")
        );

        // act
        var representationObject = type.Deserialize(serialized);

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
    public void Deserialize_Invalid_Format()
    {
        // arrange
        var type = new AnyType();
        var serialized = new ObjectValueNode();

        // act
        void Action() => type.Deserialize(serialized);

        // assert
        Assert.Throws<SerializationException>(Action);
    }

    [Fact]
    public void TryDeserialize()
    {
        // arrange
        var type = new AnyType();
        var serialized = new ObjectValueNode(
            new ObjectFieldNode(AnyType.TypeNameField, "test"),
            new ObjectFieldNode("foo", "bar"));

        // act
        var success = type.TryDeserialize(serialized, out var representation);

        // assert
        Assert.True(success);
        Assert.IsType<Representation>(representation);
    }

    [Fact]
    public void TryDeserialize_Null()
    {
        // arrange
        var type = new AnyType();

        // act
        var success = type.TryDeserialize(null, out var representation);

        // assert
        Assert.True(success);
        Assert.Null(representation);
    }

    [Fact]
    public void TryDeserialize_Invalid_Type()
    {
        // arrange
        var type = new AnyType();
        const int serialized = 1;

        // act
        var success = type.TryDeserialize(serialized, out var representation);

        // assert
        Assert.False(success);
        Assert.Null(representation);
    }

    [Fact]
    public void Serialize()
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
        var serialized = (ISyntaxNode)type.Serialize(representation)!;

        // assert
        Assert.Equal(
            objectValueNode,
            serialized,
            SyntaxComparer.BySyntax);
    }

    [Fact]
    public void Serialize_Invalid_Format()
    {
        // arrange
        var type = new AnyType();

        // act
        void Action() => type.Serialize(1);

        // assert
        Assert.Throws<SerializationException>(Action);
    }

    [Fact]
    public void TrySerialize()
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
        var success = type.TrySerialize(representation, out var serialized);

        // assert
        Assert.True(success);
        Assert.Equal(
            objectValueNode,
            (ISyntaxNode)serialized!,
            SyntaxComparer.BySyntax);
    }

    [Fact]
    public void TrySerialize_Invalid_Type()
    {
        // arrange
        var type = new AnyType();

        // act
        var success = type.TrySerialize(1, out var serialized);

        // assert
        Assert.False(success);
        Assert.Null(serialized);
    }

    [Fact]
    public void TrySerialize_Invalid_Null()
    {
        // arrange
        var type = new AnyType();

        // act
        var success = type.TrySerialize(null, out var serialized);

        // assert
        Assert.True(success);
        Assert.Null(serialized);
    }

    [Fact]
    public void ParseValue()
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
        var valueSyntax = type.ParseValue(representation);

        // assert
        Assert.Equal(
            objectValueNode,
            Assert.IsType<ObjectValueNode>(valueSyntax),
            SyntaxComparer.BySyntax);
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
        var valueSyntax = type.ParseLiteral(objectValueNode);

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
        void Action() => type.ParseLiteral(new ObjectValueNode());

        // assert
        Assert.Throws<SerializationException>(Action);
    }

    [Fact]
    public void ParseResult()
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
        var parsedResult = type.ParseResult(representation);

        // assert
        Assert.Equal(
            objectValueNode,
            Assert.IsType<ObjectValueNode>(parsedResult),
            SyntaxComparer.BySyntax);
    }

    [Fact]
    public void ParseResult_Null()
    {
        // arrange
        var type = new AnyType();

        // act
        var parsedResult = type.ParseResult(null);

        // assert
        Assert.Equal(NullValueNode.Default, parsedResult);
    }

    [Fact]
    public void ParseResult_InvalidValue()
    {
        // arrange
        var type = new AnyType();

        // act
        void Action() => type.ParseResult(new ObjectValueNode());

        // assert
        Assert.Throws<SerializationException>(Action);
    }

    [Fact]
    public void ParseValue_Null()
    {
        // arrange
        var type = new AnyType();

        // act
        var valueSyntax = type.ParseValue(null);

        // assert
        Assert.IsType<NullValueNode>(valueSyntax);
    }

    [Fact]
    public void ParseValue_InvalidValue()
    {
        // arrange
        var type = new AnyType();

        // act
        void Action() => type.ParseValue(1);

        // assert
        Assert.Throws<SerializationException>(Action);
    }
}
