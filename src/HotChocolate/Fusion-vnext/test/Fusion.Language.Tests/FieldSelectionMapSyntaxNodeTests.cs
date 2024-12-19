namespace HotChocolate.Fusion;

public sealed class FieldSelectionMapSyntaxNodeTests
{
    [Test]
    public async Task Create_NameNodeNullValue_ThrowsArgumentNullException()
    {
        // arrange & act
        static void Act() => _ = new NameNode(null!);

        // assert
        await Assert
            .That(Assert.Throws<ArgumentNullException>(Act).Message)
            .IsEqualTo("Value cannot be null. (Parameter 'value')");
    }

    [Test]
    public async Task Create_NameNodeEmptyValue_ThrowsArgumentException()
    {
        // arrange & act
        static void Act() => _ = new NameNode("");

        // assert
        await Assert
            .That(Assert.Throws<ArgumentException>(Act).Message)
            .IsEqualTo("The value cannot be an empty string. (Parameter 'value')");
    }

    [Test]
    public async Task ToString_NameNode_ReturnsExpectedString()
    {
        // arrange
        var node = new NameNode("field1");

        // act
        var result = node.ToString();

        // assert
        await Assert.That(result).IsEqualTo("field1");
    }

    [Test]
    public async Task ToString_PathNode_ReturnsExpectedString()
    {
        // arrange
        var node = new PathNode(
            pathSegment: new PathSegmentNode(fieldName: new NameNode("field1")),
            typeName: new NameNode("Type1"));

        // act
        var result = node.ToString();

        // assert
        await Assert.That(result).IsEqualTo("<Type1>.field1");
    }

    [Test]
    public async Task ToString_PathSegmentNode_ReturnsExpectedString()
    {
        // arrange
        var node = new PathSegmentNode(
            fieldName: new NameNode("field1"),
            typeName: new NameNode("Type1"),
            pathSegment: new PathSegmentNode(
                fieldName: new NameNode("field2"),
                typeName: new NameNode("Type2"),
                pathSegment: new PathSegmentNode(fieldName: new NameNode("field3"))));

        // act
        var result = node.ToString();

        // assert
        await Assert.That(result).IsEqualTo("field1<Type1>.field2<Type2>.field3");
    }

    [Test]
    public async Task ToString_SelectedListValueNode_ReturnsExpectedString()
    {
        // arrange
        var node = new SelectedListValueNode(
            selectedValue: new SelectedValueNode(
                path: new PathNode(
                    pathSegment: new PathSegmentNode(fieldName: new NameNode("field1")))));

        // act
        var result = node.ToString();

        // assert
        await Assert.That(result).IsEqualTo("[field1]");
    }

    [Test]
    public async Task ToString_SelectedObjectFieldNode_ReturnsExpectedString()
    {
        // arrange
        var node = new SelectedObjectFieldNode(
            new NameNode("field1"),
            new SelectedValueNode(
                path: new PathNode(
                    pathSegment: new PathSegmentNode(fieldName: new NameNode("field1")))));

        // act
        var result = node.ToString();

        // assert
        await Assert.That(result).IsEqualTo("field1: field1");
    }

    [Test]
    public async Task ToString_SelectedObjectValueNode_ReturnsExpectedString()
    {
        // arrange
        var node = new SelectedObjectValueNode(fields:
            [
                new SelectedObjectFieldNode(
                    new NameNode("field1"),
                    new SelectedValueNode(
                        path: new PathNode(
                            pathSegment: new PathSegmentNode(fieldName: new NameNode("field1"))))),
                new SelectedObjectFieldNode(
                    new NameNode("field2"),
                    new SelectedValueNode(
                        path: new PathNode(
                            pathSegment: new PathSegmentNode(fieldName: new NameNode("field2")))))
            ]);

        // act
        var result = node.ToString(
            new StringSyntaxWriterOptions
            {
                IndentSize = 4,
                NewLine = "\n"
            });

        // assert
        await Assert.That(result).IsEqualTo(
            """
            {
                field1: field1
                field2: field2
            }
            """);
    }

    [Test]
    public async Task ToString_SelectedValueNode_ReturnsExpectedString()
    {
        // arrange
        var node = new SelectedValueNode(
            path: new PathNode(
                pathSegment: new PathSegmentNode(
                    fieldName: new NameNode("field1"),
                    pathSegment: new PathSegmentNode(fieldName: new NameNode("field2")))),
            selectedValue: new SelectedValueNode(
                path: new PathNode(
                    pathSegment: new PathSegmentNode(
                        fieldName: new NameNode("field3"),
                        pathSegment: new PathSegmentNode(fieldName: new NameNode("field4")))),
                selectedValue: new SelectedValueNode(
                    path: new PathNode(
                        pathSegment: new PathSegmentNode(
                            fieldName: new NameNode("field5"),
                            pathSegment: new PathSegmentNode(
                                fieldName: new NameNode("field6")))))));

        // act
        var result = node.ToString();

        // assert
        await Assert.That(result).IsEqualTo("field1.field2 | field3.field4 | field5.field6");
    }
}
