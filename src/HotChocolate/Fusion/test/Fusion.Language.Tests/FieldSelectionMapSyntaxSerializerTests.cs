using System.Collections.Immutable;

namespace HotChocolate.Fusion.Language;

public sealed class FieldSelectionMapSyntaxSerializerTests
{
    private readonly FieldSelectionMapSyntaxSerializer _serializer
        = new(new SyntaxSerializerOptions { Indented = true });

    private readonly FieldSelectionMapSyntaxSerializer _serializerNoIndent
        = new(new SyntaxSerializerOptions { Indented = false });

    private readonly StringSyntaxWriter _writer
        = new(new StringSyntaxWriterOptions { IndentSize = 4, NewLine = "\n" });

    [Fact]
    public void Serialize_Name_ReturnsExpectedString()
    {
        // arrange
        var nameNode = new NameNode("field1");

        // act
        _serializer.Serialize(nameNode, _writer);

        // assert
        Assert.Equal("field1", _writer.ToString());
    }

    [Fact]
    public void Serialize_PathSegmentSingleFieldName_ReturnsExpectedString()
    {
        // arrange
        var pathSegmentNode = new PathSegmentNode(fieldName: new NameNode("field1"));

        // act
        _serializer.Serialize(pathSegmentNode, _writer);

        // assert
        Assert.Equal("field1", _writer.ToString());
    }

    [Fact]
    public void Serialize_PathSegmentNestedFieldName_ReturnsExpectedString()
    {
        // arrange
        var pathSegmentNode = new PathSegmentNode(
            fieldName: new NameNode("field1"),
            pathSegment: new PathSegmentNode(fieldName: new NameNode("field2")));

        // act
        _serializer.Serialize(pathSegmentNode, _writer);

        // assert
        Assert.Equal("field1.field2", _writer.ToString());
    }

    [Fact]
    public void Serialize_PathSegmentWithTypeName_ReturnsExpectedString()
    {
        // arrange
        var pathSegmentNode = new PathSegmentNode(
            fieldName: new NameNode("field1"),
            typeName: new NameNode("Type1"),
            pathSegment: new PathSegmentNode(fieldName: new NameNode("field2")));

        // act
        _serializer.Serialize(pathSegmentNode, _writer);

        // assert
        Assert.Equal("field1<Type1>.field2", _writer.ToString());
    }

    [Fact]
    public void Serialize_PathSegmentWithTwoTypeNames_ReturnsExpectedString()
    {
        // arrange
        var pathSegmentNode = new PathSegmentNode(
            fieldName: new NameNode("field1"),
            typeName: new NameNode("Type1"),
            pathSegment: new PathSegmentNode(
                fieldName: new NameNode("field2"),
                typeName: new NameNode("Type2"),
                pathSegment: new PathSegmentNode(fieldName: new NameNode("field3"))));

        // act
        _serializer.Serialize(pathSegmentNode, _writer);

        // assert
        Assert.Equal("field1<Type1>.field2<Type2>.field3", _writer.ToString());
    }

    [Fact]
    public void Serialize_PathWithTypeName_ReturnsExpectedString()
    {
        // arrange
        var pathNode = new PathNode(
            pathSegment: new PathSegmentNode(fieldName: new NameNode("field1")),
            typeName: new NameNode("Type1"));

        // act
        _serializer.Serialize(pathNode, _writer);

        // assert
        Assert.Equal("<Type1>.field1", _writer.ToString());
    }

    [Fact]
    public void Serialize_SelectedListValue_ReturnsExpectedString()
    {
        // arrange
        var node = new ListValueSelectionNode(
            new PathNode(new PathSegmentNode(new NameNode("field1"))));

        // act
        _serializer.Serialize(node, _writer);

        // assert
        Assert.Equal("[field1]", _writer.ToString());
    }

    [Fact]
    public void Serialize_SelectedObjectField_ReturnsExpectedString()
    {
        // arrange
        var node = new ObjectFieldSelectionNode(
            new NameNode("field1"),
            new PathNode(new PathSegmentNode(new NameNode("field1"))));

        // act
        _serializer.Serialize(node, _writer);

        // assert
        Assert.Equal("field1: field1", _writer.ToString());
    }

    [Fact]
    public void Serialize_SelectedObjectFieldNoSelectedValue_ReturnsExpectedString()
    {
        // arrange
        var selectedObjectFieldNode = new ObjectFieldSelectionNode(
            new NameNode("field1"));

        // act
        _serializer.Serialize(selectedObjectFieldNode, _writer);

        // assert
        Assert.Equal("field1", _writer.ToString());
    }

    [Theory]
    [InlineData(
        """
        {
            field1: field1
        }
        """,
        true)]
    [InlineData("{ field1: field1 }", false)]
    public void Serialize_SelectedObjectValue_ReturnsExpectedString(string result, bool indent)
    {
        // arrange
        var selectedObjectValueNode = new ObjectValueSelectionNode(
            fields:
            [
                new ObjectFieldSelectionNode(
                    new NameNode("field1"),
                    new PathNode(new PathSegmentNode( fieldName: new NameNode("field1"))))
            ]);

        // act
        var serializer = indent ? _serializer : _serializerNoIndent;
        serializer.Serialize(selectedObjectValueNode, _writer);

        // assert
        Assert.Equal(result, _writer.ToString());
    }

    [Theory]
    [InlineData(
        """
        {
            field1
        }
        """,
        true)]
    [InlineData("{ field1 }", false)]
    public void Serialize_SelectedObjectValueNoSelectedValue_ReturnsExpectedString(
        string result,
        bool indent)
    {
        // arrange
        var selectedObjectValueNode = new ObjectValueSelectionNode(
            [new ObjectFieldSelectionNode(new NameNode("field1"))]);

        // act
        var serializer = indent ? _serializer : _serializerNoIndent;
        serializer.Serialize(selectedObjectValueNode, _writer);

        // assert
        Assert.Equal(result, _writer.ToString());
    }

    [Fact]
    public void Serialize_SelectedValueMultiplePaths_ReturnsExpectedString()
    {
        // arrange
        var node = new ChoiceValueSelectionNode(
        [
            new PathNode(
                new PathSegmentNode(
                    new NameNode("field1"),
                    new NameNode("Type1"),
                    new PathSegmentNode(new NameNode("field2")))),
            new PathNode(
                new PathSegmentNode(
                    new NameNode("field1"),
                    new NameNode("Type2"),
                    new PathSegmentNode(new NameNode("field2"))))
        ]);

        // act
        _serializer.Serialize(node, _writer);

        // assert
        Assert.Equal("field1<Type1>.field2 | field1<Type2>.field2", _writer.ToString());
    }

    [Theory]
    [InlineData(
        """
        {
            field1: field1
        } | {
            field2: field2
        }
        """,
        true)]
    [InlineData("{ field1: field1 } | { field2: field2 }", false)]
    public void Serialize_SelectedValueMultipleSelectedObjectValues_ReturnsExpectedString(
        string result,
        bool indent)
    {
        // arrange
        var node = new ChoiceValueSelectionNode(
        [
            new ObjectValueSelectionNode(
            [
                new ObjectFieldSelectionNode(
                    new NameNode("field1"),
                    new PathNode( new PathSegmentNode(new NameNode("field1"))))
            ]),
            new ObjectValueSelectionNode(
            [
                new ObjectFieldSelectionNode(
                    new NameNode("field2"),
                    new PathNode( new PathSegmentNode( new NameNode("field2"))))
            ])
        ]);

        // act
        var serializer = indent ? _serializer : _serializerNoIndent;
        serializer.Serialize(node, _writer);

        // assert
        Assert.Equal(result, _writer.ToString());
    }

    [Theory]
    [InlineData(
        """
        {
            nested: {
                field1: field1
            } | {
                field2: field2
            }
        }
        """,
        true)]
    [InlineData("{ nested: { field1: field1 } | { field2: field2 } }", false)]
    public void Serialize_SelectedValueMultipleSelectedObjectValuesNested_ReturnsExpectedString(
        string result,
        bool indent)
    {
        // arrange
        var selectedValueNode = new ObjectValueSelectionNode(
        [
            new ObjectFieldSelectionNode(
                new NameNode("nested"),
                new ChoiceValueSelectionNode(
                [
                    new ObjectValueSelectionNode(
                    [
                        new ObjectFieldSelectionNode(
                            new NameNode("field1"),
                            new PathNode(new PathSegmentNode(new NameNode("field1"))))
                    ]),
                    new ObjectValueSelectionNode(
                    [
                        new ObjectFieldSelectionNode(
                            new NameNode("field2"),
                            new PathNode(new PathSegmentNode(new NameNode("field2"))))
                    ])
                ]))
        ]);

        // act
        var serializer = indent ? _serializer : _serializerNoIndent;
        serializer.Serialize(selectedValueNode, _writer);

        // assert
        Assert.Equal(result, _writer.ToString());
    }

    [Fact]
    public void Serialize_PathSegmentWithArgument_ReturnsExpectedString()
    {
        // arrange
        var node = new PathSegmentNode(
            location: null,
            fieldName: new NameNode("width"),
            arguments: [new ArgumentNode(new NameNode("unit"), new EnumValueNode("IMPERIAL"))],
            typeName: null,
            pathSegment: null);

        // act
        _serializerNoIndent.Serialize(node, _writer);

        // assert
        Assert.Equal("width(unit: IMPERIAL)", _writer.ToString());
    }

    [Fact]
    public void Serialize_PathSegmentWithMultipleArguments_ReturnsExpectedString()
    {
        // arrange
        var node = new PathSegmentNode(
            location: null,
            fieldName: new NameNode("box"),
            arguments:
            [
                new ArgumentNode(new NameNode("width"), new IntValueNode("1")),
                new ArgumentNode(new NameNode("height"), new IntValueNode("2"))
            ],
            typeName: null,
            pathSegment: null);

        // act
        _serializerNoIndent.Serialize(node, _writer);

        // assert
        Assert.Equal("box(width: 1, height: 2)", _writer.ToString());
    }

    [Theory]
    [InlineData(
        """
        {
            width: width(unit: IMPERIAL)
        }
        """,
        true)]
    [InlineData("{ width: width(unit: IMPERIAL) }", false)]
    public void Serialize_ObjectFieldSelectionWithArgument_ReturnsExpectedString(
        string result,
        bool indent)
    {
        // arrange
        var node = new ObjectValueSelectionNode(
        [
            new ObjectFieldSelectionNode(
                new NameNode("width"),
                new PathNode(
                    new PathSegmentNode(
                        location: null,
                        fieldName: new NameNode("width"),
                        arguments:
                        [
                            new ArgumentNode(new NameNode("unit"), new EnumValueNode("IMPERIAL"))
                        ],
                        typeName: null,
                        pathSegment: null)))
        ]);

        // act
        var serializer = indent ? _serializer : _serializerNoIndent;
        serializer.Serialize(node, _writer);

        // assert
        Assert.Equal(result, _writer.ToString());
    }

    [Fact]
    public void Serialize_ArgumentWithListValue_ReturnsExpectedString()
    {
        // arrange
        var node = new ArgumentNode(
            new NameNode("l"),
            new ListValueNode(
                ImmutableArray.Create<IValueNode>(
                    new IntValueNode("1"),
                    new IntValueNode("2"))));

        // act
        _serializerNoIndent.Serialize(node, _writer);

        // assert
        Assert.Equal("l: [1, 2]", _writer.ToString());
    }

    [Fact]
    public void Serialize_ArgumentWithObjectValue_ReturnsExpectedString()
    {
        // arrange
        var node = new ArgumentNode(
            new NameNode("o"),
            new ObjectValueNode(
                ImmutableArray.Create(
                    new ObjectFieldNode(new NameNode("a"), new IntValueNode("1")),
                    new ObjectFieldNode(
                        new NameNode("b"),
                        new ListValueNode(
                            ImmutableArray.Create<IValueNode>(new IntValueNode("2")))))));

        // act
        _serializerNoIndent.Serialize(node, _writer);

        // assert
        Assert.Equal("o: { a: 1, b: [2] }", _writer.ToString());
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void Serialize_BooleanValue_ReturnsExpectedString(bool value, string result)
    {
        // arrange
        var node = new BooleanValueNode(value);

        // act
        _serializerNoIndent.Serialize(node, _writer);

        // assert
        Assert.Equal(result, _writer.ToString());
    }

    [Fact]
    public void Serialize_NullValue_ReturnsExpectedString()
    {
        // arrange
        var node = new NullValueNode();

        // act
        _serializerNoIndent.Serialize(node, _writer);

        // assert
        Assert.Equal("null", _writer.ToString());
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("6.0221413e23")]
    public void Serialize_FloatValue_ReturnsExpectedString(string value)
    {
        // arrange
        var node = new FloatValueNode(value);

        // act
        _serializerNoIndent.Serialize(node, _writer);

        // assert
        Assert.Equal(value, _writer.ToString());
    }

    [Fact]
    public void Serialize_StringValue_ReturnsExpectedString()
    {
        // arrange
        var node = new StringValueNode("hello");

        // act
        _serializerNoIndent.Serialize(node, _writer);

        // assert
        Assert.Equal("\"hello\"", _writer.ToString());
    }

    [Fact]
    public void Serialize_StringValueWithSpecialCharacters_ReturnsEscapedString()
    {
        // arrange
        var node = new StringValueNode("a\"b\\c\nd\te");

        // act
        _serializerNoIndent.Serialize(node, _writer);

        // assert
        Assert.Equal("\"a\\\"b\\\\c\\nd\\te\"", _writer.ToString());
    }

    [Fact]
    public void Serialize_BlockStringValue_ReturnsTripleQuotedString()
    {
        // arrange
        var node = new StringValueNode(value: "line1\nline2", block: true);

        // act
        _serializerNoIndent.Serialize(node, _writer);

        // assert
        Assert.Equal("\"\"\"\nline1\nline2\n\"\"\"", _writer.ToString());
    }
}
