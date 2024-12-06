namespace HotChocolate.Fusion;

public sealed class FieldSelectionMapSyntaxSerializerTests
{
    private readonly FieldSelectionMapSyntaxSerializer _serializer
        = new(new SyntaxSerializerOptions { Indented = true });

    private readonly FieldSelectionMapSyntaxSerializer _serializerNoIndent
        = new(new SyntaxSerializerOptions { Indented = false });

    private readonly StringSyntaxWriter _writer
        = new(new StringSyntaxWriterOptions { IndentSize = 4, NewLine = "\n" });

    [Test]
    public async Task Serialize_Name_ReturnsExpectedString()
    {
        // arrange
        var nameNode = new NameNode("field1");

        // act
        _serializer.Serialize(nameNode, _writer);

        // assert
        await Assert.That(_writer.ToString()).IsEqualTo("field1");
    }

    [Test]
    public async Task Serialize_PathSingleFieldName_ReturnsExpectedString()
    {
        // arrange
        var pathNode = new PathNode(fieldName: new NameNode("field1"));

        // act
        _serializer.Serialize(pathNode, _writer);

        // assert
        await Assert.That(_writer.ToString()).IsEqualTo("field1");
    }

    [Test]
    public async Task Serialize_PathNestedFieldName_ReturnsExpectedString()
    {
        // arrange
        var pathNode = new PathNode(
            fieldName: new NameNode("field1"),
            path: new PathNode(fieldName: new NameNode("field2")));

        // act
        _serializer.Serialize(pathNode, _writer);

        // assert
        await Assert.That(_writer.ToString()).IsEqualTo("field1.field2");
    }

    [Test]
    public async Task Serialize_PathWithTypeName_ReturnsExpectedString()
    {
        // arrange
        var pathNode = new PathNode(
            fieldName: new NameNode("field1"),
            typeName: new NameNode("Type1"),
            path: new PathNode(fieldName: new NameNode("field2")));

        // act
        _serializer.Serialize(pathNode, _writer);

        // assert
        await Assert.That(_writer.ToString()).IsEqualTo("field1<Type1>.field2");
    }

    [Test]
    public async Task Serialize_PathWithTwoTypeNames_ReturnsExpectedString()
    {
        // arrange
        var pathNode = new PathNode(
            fieldName: new NameNode("field1"),
            typeName: new NameNode("Type1"),
            path: new PathNode(
                fieldName: new NameNode("field2"),
                typeName: new NameNode("Type2"),
                path: new PathNode(fieldName: new NameNode("field3"))));

        // act
        _serializer.Serialize(pathNode, _writer);

        // assert
        await Assert.That(_writer.ToString()).IsEqualTo("field1<Type1>.field2<Type2>.field3");
    }

    [Test]
    public async Task Serialize_SelectedObjectField_ReturnsExpectedString()
    {
        // arrange
        var selectedObjectFieldNode = new SelectedObjectFieldNode(
            new NameNode("field1"),
            new SelectedValueNode(new PathNode(fieldName: new NameNode("field1"))));

        // act
        _serializer.Serialize(selectedObjectFieldNode, _writer);

        // assert
        await Assert.That(_writer.ToString()).IsEqualTo("field1: field1");
    }

    [Test]
    [Arguments(
        """
        {
            field1: field1
        }
        """,
        true)]
    [Arguments("{ field1: field1 }", false)]
    public async Task Serialize_SelectedObjectValue_ReturnsExpectedString(
        string result,
        bool indent)
    {
        // arrange
        var selectedObjectValueNode = new SelectedObjectValueNode(
            fields:
            [
                new SelectedObjectFieldNode(
                    new NameNode("field1"),
                    new SelectedValueNode(new PathNode(fieldName: new NameNode("field1"))))
            ]);

        // act
        var serializer = indent ? _serializer : _serializerNoIndent;
        serializer.Serialize(selectedObjectValueNode, _writer);

        // assert
        await Assert.That(_writer.ToString()).IsEqualTo(result);
    }

    [Test]
    public async Task Serialize_SelectedValueMultiplePaths_ReturnsExpectedString()
    {
        // arrange
        var selectedValueNode = new SelectedValueNode(
            path: new PathNode(
                fieldName: new NameNode("field1"),
                typeName: new NameNode("Type1"),
                path: new PathNode(fieldName: new NameNode("field2"))),
            selectedValue: new SelectedValueNode(
                new PathNode(
                    fieldName: new NameNode("field1"),
                    typeName: new NameNode("Type2"),
                    path: new PathNode(fieldName: new NameNode("field2")))));

        // act
        _serializer.Serialize(selectedValueNode, _writer);

        // assert
        await Assert
            .That(_writer.ToString())
            .IsEqualTo("field1<Type1>.field2 | field1<Type2>.field2");
    }

    [Test]
    [Arguments(
        """
        {
            field1: field1
        } | {
            field2: field2
        }
        """,
        true)]
    [Arguments("{ field1: field1 } | { field2: field2 }", false)]
    public async Task Serialize_SelectedValueMultipleSelectedObjectValues_ReturnsExpectedString(
        string result,
        bool indent)
    {
        // arrange
        var selectedValueNode = new SelectedValueNode(
            selectedObjectValue: new SelectedObjectValueNode(
                fields:
                [
                    new SelectedObjectFieldNode(
                        new NameNode("field1"),
                        new SelectedValueNode(new PathNode(fieldName: new NameNode("field1"))))
                ]),
            selectedValue: new SelectedValueNode(
                selectedObjectValue: new SelectedObjectValueNode(
                    fields:
                    [
                        new SelectedObjectFieldNode(
                            new NameNode("field2"),
                            new SelectedValueNode(new PathNode(fieldName: new NameNode("field2"))))
                    ])));

        // act
        var serializer = indent ? _serializer : _serializerNoIndent;
        serializer.Serialize(selectedValueNode, _writer);

        // assert
        await Assert.That(_writer.ToString()).IsEqualTo(result);
    }

    [Test]
    [Arguments(
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
    [Arguments("{ nested: { field1: field1 } | { field2: field2 } }", false)]
    public async Task Serialize_SelectedValueMultipleSelectedObjectValuesNested_ReturnsExpectedString(
        string result,
        bool indent)
    {
        // arrange
        var selectedValueNode = new SelectedValueNode(
            selectedObjectValue: new SelectedObjectValueNode(
                fields:
                [
                    new SelectedObjectFieldNode(
                        new NameNode("nested"),
                        new SelectedValueNode(
                            selectedObjectValue: new SelectedObjectValueNode(
                                fields:
                                [
                                    new SelectedObjectFieldNode(
                                        new NameNode("field1"),
                                        new SelectedValueNode(
                                            path: new PathNode(fieldName: new NameNode("field1"))))
                                ]),
                            selectedValue: new SelectedValueNode(
                                selectedObjectValue: new SelectedObjectValueNode(
                                    fields:
                                    [
                                        new SelectedObjectFieldNode(
                                            new NameNode("field2"),
                                            new SelectedValueNode(
                                                path: new PathNode(
                                                    fieldName: new NameNode("field2"))))
                                    ]))))
                ]));

        // act
        var serializer = indent ? _serializer : _serializerNoIndent;
        serializer.Serialize(selectedValueNode, _writer);

        // assert
        await Assert.That(_writer.ToString()).IsEqualTo(result);
    }
}
