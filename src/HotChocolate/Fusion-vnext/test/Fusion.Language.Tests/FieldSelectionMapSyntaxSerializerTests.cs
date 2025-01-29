namespace HotChocolate.Fusion;

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
        var selectedListValueNode = new SelectedListValueNode(
            selectedValue: new SelectedValueNode(
                selectedValueEntry: new SelectedValueEntryNode(
                    path: new PathNode(
                        pathSegment: new PathSegmentNode(fieldName: new NameNode("field1"))))));

        // act
        _serializer.Serialize(selectedListValueNode, _writer);

        // assert
        Assert.Equal("[field1]", _writer.ToString());
    }

    [Fact]
    public void Serialize_SelectedObjectField_ReturnsExpectedString()
    {
        // arrange
        var selectedObjectFieldNode = new SelectedObjectFieldNode(
            new NameNode("field1"),
            new SelectedValueNode(
                selectedValueEntry: new SelectedValueEntryNode(
                    path: new PathNode(
                        pathSegment: new PathSegmentNode(fieldName: new NameNode("field1"))))));

        // act
        _serializer.Serialize(selectedObjectFieldNode, _writer);

        // assert
        Assert.Equal("field1: field1", _writer.ToString());
    }

    [Fact]
    public void Serialize_SelectedObjectFieldNoSelectedValue_ReturnsExpectedString()
    {
        // arrange
        var selectedObjectFieldNode = new SelectedObjectFieldNode(new NameNode("field1"));

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
        var selectedObjectValueNode = new SelectedObjectValueNode(
            fields:
            [
                new SelectedObjectFieldNode(
                    new NameNode("field1"),
                    new SelectedValueNode(
                        selectedValueEntry: new SelectedValueEntryNode(
                            path: new PathNode(
                                pathSegment: new PathSegmentNode(
                                    fieldName: new NameNode("field1"))))))
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
        var selectedObjectValueNode = new SelectedObjectValueNode(
            fields: [new SelectedObjectFieldNode(new NameNode("field1"))]);

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
        var selectedValueNode = new SelectedValueNode(
            selectedValueEntry: new SelectedValueEntryNode(
                path: new PathNode(
                    pathSegment: new PathSegmentNode(
                        fieldName: new NameNode("field1"),
                        typeName: new NameNode("Type1"),
                        pathSegment: new PathSegmentNode(fieldName: new NameNode("field2"))))),
            selectedValue: new SelectedValueNode(
                selectedValueEntry: new SelectedValueEntryNode(
                    path: new PathNode(
                        pathSegment: new PathSegmentNode(
                            fieldName: new NameNode("field1"),
                            typeName: new NameNode("Type2"),
                            pathSegment: new PathSegmentNode(
                                fieldName: new NameNode("field2")))))));

        // act
        _serializer.Serialize(selectedValueNode, _writer);

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
        var selectedValueNode = new SelectedValueNode(
            selectedValueEntry: new SelectedValueEntryNode(
                selectedObjectValue: new SelectedObjectValueNode(
                    fields:
                    [
                        new SelectedObjectFieldNode(
                            new NameNode("field1"),
                            new SelectedValueNode(
                                selectedValueEntry: new SelectedValueEntryNode(
                                    path: new PathNode(
                                        pathSegment: new PathSegmentNode(
                                            fieldName: new NameNode("field1"))))))
                    ])),
            selectedValue: new SelectedValueNode(
                selectedValueEntry: new SelectedValueEntryNode(
                    selectedObjectValue: new SelectedObjectValueNode(
                        fields:
                        [
                            new SelectedObjectFieldNode(
                                new NameNode("field2"),
                                new SelectedValueNode(
                                    selectedValueEntry: new SelectedValueEntryNode(
                                        path: new PathNode(
                                            pathSegment: new PathSegmentNode(
                                                fieldName: new NameNode("field2"))))))
                        ]))));

        // act
        var serializer = indent ? _serializer : _serializerNoIndent;
        serializer.Serialize(selectedValueNode, _writer);

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
        var selectedValueNode = new SelectedValueNode(
            selectedValueEntry: new SelectedValueEntryNode(
                selectedObjectValue: new SelectedObjectValueNode(
                    fields:
                    [
                        new SelectedObjectFieldNode(
                            new NameNode("nested"),
                            new SelectedValueNode(
                                selectedValueEntry: new SelectedValueEntryNode(
                                    selectedObjectValue: new SelectedObjectValueNode(
                                        fields:
                                        [
                                            new SelectedObjectFieldNode(
                                                new NameNode("field1"),
                                                new SelectedValueNode(
                                                    selectedValueEntry: new SelectedValueEntryNode(
                                                        path: new PathNode(
                                                            pathSegment: new PathSegmentNode(
                                                                fieldName:
                                                                    new NameNode("field1"))))))
                                        ])),
                                selectedValue: new SelectedValueNode(
                                    selectedValueEntry: new SelectedValueEntryNode(
                                        selectedObjectValue: new SelectedObjectValueNode(
                                            fields:
                                            [
                                                new SelectedObjectFieldNode(
                                                    new NameNode("field2"),
                                                    new SelectedValueNode(
                                                        selectedValueEntry:
                                                            new SelectedValueEntryNode(
                                                                path: new PathNode(
                                                                    pathSegment:
                                                                        new PathSegmentNode(
                                                                            fieldName:
                                                                                new NameNode(
                                                                                    "field2"))))))
                                            ])))))
                    ])));

        // act
        var serializer = indent ? _serializer : _serializerNoIndent;
        serializer.Serialize(selectedValueNode, _writer);

        // assert
        Assert.Equal(result, _writer.ToString());
    }
}
