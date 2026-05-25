namespace HotChocolate.Fusion.Language;

public sealed class FieldSelectionMapSyntaxNodeTests
{
    [Fact]
    public void Create_NameNodeNullValue_ThrowsArgumentNullException()
    {
        // arrange & act
        static void Act() => _ = new NameNode(null!);

        // assert
        Assert.Equal(
            "Value cannot be null. (Parameter 'value')",
            Assert.Throws<ArgumentNullException>(Act).Message);
    }

    [Fact]
    public void Create_NameNodeEmptyValue_ThrowsArgumentException()
    {
        // arrange & act
        static void Act() => _ = new NameNode("");

        // assert
        Assert.Equal(
            "The value cannot be an empty string. (Parameter 'value')",
            Assert.Throws<ArgumentException>(Act).Message);
    }

    [Fact]
    public void ToString_NameNode_ReturnsExpectedString()
    {
        // arrange
        var node = new NameNode("field1");

        // act
        var result = node.ToString();

        // assert
        Assert.Equal("field1", result);
    }

    [Fact]
    public void ToString_PathNode_ReturnsExpectedString()
    {
        // arrange
        var node = new PathNode(
            pathSegment: new PathSegmentNode(fieldName: new NameNode("field1")),
            typeName: new NameNode("Type1"));

        // act
        var result = node.ToString();

        // assert
        Assert.Equal("<Type1>.field1", result);
    }

    [Fact]
    public void ToString_PathSegmentNode_ReturnsExpectedString()
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
        Assert.Equal("field1<Type1>.field2<Type2>.field3", result);
    }

    [Fact]
    public void ToString_SelectedListValueNode_ReturnsExpectedString()
    {
        // arrange
        var node = new ListValueSelectionNode(new PathNode(new PathSegmentNode(new NameNode("field1"))));

        // act
        var result = node.ToString();

        // assert
        Assert.Equal("[field1]", result);
    }

    [Fact]
    public void ToString_SelectedObjectFieldNode_ReturnsExpectedString()
    {
        // arrange
        var node = new ObjectFieldSelectionNode(
            new NameNode("field1"),
            new PathNode(new PathSegmentNode(new NameNode("field1"))));

        // act
        var result = node.ToString();

        // assert
        Assert.Equal("field1: field1", result);
    }

    [Fact]
    public void ToString_SelectedObjectValueNode_ReturnsExpectedString()
    {
        // arrange
        var node = new ObjectValueSelectionNode(
        [
            new ObjectFieldSelectionNode(
                new NameNode("field1"),
                new PathNode(new PathSegmentNode(new NameNode("field1")))),
            new ObjectFieldSelectionNode(
                new NameNode("field2"),
                new PathNode(new PathSegmentNode(new NameNode("field2"))))
        ]);

        // act
        var result = node.ToString(
            new StringSyntaxWriterOptions
            {
                IndentSize = 4,
                NewLine = "\n"
            });

        // assert
        Assert.Equal(
            """
            {
                field1: field1
                field2: field2
            }
            """,
            result);
    }

    [Fact]
    public void ToString_SelectedValueNode_ReturnsExpectedString()
    {
        // arrange
        var node = new ChoiceValueSelectionNode(
        [
            new PathNode(new PathSegmentNode(new NameNode("field1"), new PathSegmentNode(new NameNode("field2")))),
            new PathNode(new PathSegmentNode(new NameNode("field3"), new PathSegmentNode(new NameNode("field4")))),
            new PathNode(new PathSegmentNode(new NameNode("field5"), new PathSegmentNode( new NameNode("field6"))))
        ]);

        // act
        var result = node.ToString();

        // assert
        Assert.Equal("field1.field2 | field3.field4 | field5.field6", result);
    }
}
