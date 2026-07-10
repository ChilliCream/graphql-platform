using System.Collections.Immutable;

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

    [Fact]
    public void Create_IntValueNodeEmptyValue_ThrowsArgumentException()
    {
        // arrange & act
        static void Act() => _ = new IntValueNode("");

        // assert
        Assert.Equal(
            "The value cannot be an empty string. (Parameter 'value')",
            Assert.Throws<ArgumentException>(Act).Message);
    }

    [Fact]
    public void Create_EnumValueNodeNullValue_ThrowsArgumentNullException()
    {
        // arrange & act
        static void Act() => _ = new EnumValueNode(null!);

        // assert
        Assert.Equal(
            "Value cannot be null. (Parameter 'value')",
            Assert.Throws<ArgumentNullException>(Act).Message);
    }

    [Fact]
    public void Create_StringValueNodeNullValue_ThrowsArgumentNullException()
    {
        // arrange & act
        static void Act() => _ = new StringValueNode(null!);

        // assert
        Assert.Equal(
            "Value cannot be null. (Parameter 'value')",
            Assert.Throws<ArgumentNullException>(Act).Message);
    }

    [Fact]
    public void Create_ArgumentNodeNullValue_ThrowsArgumentNullException()
    {
        // arrange & act
        static void Act() => _ = new ArgumentNode(new NameNode("unit"), null!);

        // assert
        Assert.Equal(
            "Value cannot be null. (Parameter 'value')",
            Assert.Throws<ArgumentNullException>(Act).Message);
    }

    [Fact]
    public void Create_ObjectFieldNodeNullName_ThrowsArgumentNullException()
    {
        // arrange & act
        static void Act() => _ = new ObjectFieldNode(null!, new IntValueNode("1"));

        // assert
        Assert.Equal(
            "Value cannot be null. (Parameter 'name')",
            Assert.Throws<ArgumentNullException>(Act).Message);
    }

    [Fact]
    public void ToString_StringValueNodeBlock_ReturnsHotChocolateMultiLineLayout()
    {
        // arrange
        // a block string is printed in HotChocolate's multi-line triple-quote layout.
        var node = new StringValueNode("block", block: true);

        // act
        var result = node.ToString().Replace("\r\n", "\n");

        // assert
        Assert.Equal("\"\"\"\nblock\n\"\"\"", result);
    }

    [Fact]
    public void ToString_ArgumentNode_ReturnsExpectedString()
    {
        // arrange
        var node = new ArgumentNode(new NameNode("unit"), new EnumValueNode("IMPERIAL"));

        // act
        var result = node.ToString();

        // assert
        Assert.Equal("unit: IMPERIAL", result);
    }

    [Fact]
    public void ToString_PathSegmentNodeWithArguments_ReturnsExpectedString()
    {
        // arrange
        var node = new PathSegmentNode(
            location: null,
            fieldName: new NameNode("width"),
            arguments: [new ArgumentNode(new NameNode("unit"), new EnumValueNode("IMPERIAL"))],
            typeName: null,
            pathSegment: null);

        // act
        var result = node.ToString();

        // assert
        Assert.Equal("width(unit: IMPERIAL)", result);
    }

    [Fact]
    public void ToString_ObjectFieldSelectionNodeWithArguments_ReturnsExpectedString()
    {
        // arrange
        var node = new ObjectFieldSelectionNode(
            location: null,
            name: new NameNode("width"),
            arguments: [new ArgumentNode(new NameNode("unit"), new EnumValueNode("IMPERIAL"))],
            valueSelection: null);

        // act
        var result = node.ToString();

        // assert
        Assert.Equal("width(unit: IMPERIAL)", result);
    }

    [Fact]
    public void ToString_ListValueNode_ReturnsExpectedString()
    {
        // arrange
        var node = new ListValueNode(
            ImmutableArray.Create<IValueNode>(
                new IntValueNode("1"),
                new IntValueNode("2"),
                new IntValueNode("3")));

        // act
        var result = node.ToString();

        // assert
        Assert.Equal("[1, 2, 3]", result);
    }

    [Fact]
    public void ToString_ObjectValueNode_ReturnsExpectedString()
    {
        // arrange
        var node = new ObjectValueNode(
            ImmutableArray.Create(
                new ObjectFieldNode(new NameNode("a"), new IntValueNode("1")),
                new ObjectFieldNode(new NameNode("b"), new EnumValueNode("B"))));

        // act
        var result = node.ToString();

        // assert
        Assert.Equal("{ a: 1, b: B }", result);
    }

    [Fact]
    public void ToString_ListValueNodeDefaultItems_ReturnsEmptyList()
    {
        // arrange
        var node = new ListValueNode(default);

        // act
        var result = node.ToString();

        // assert
        Assert.Equal("[]", result);
    }

    [Fact]
    public void ToString_ObjectValueNodeDefaultFields_ReturnsEmptyObject()
    {
        // arrange
        var node = new ObjectValueNode(default);

        // act
        var result = node.ToString();

        // assert
        Assert.Equal("{}", result);
    }

    [Fact]
    public void ToString_EnumValueNodeBooleanKeyword_ReturnsBareKeyword()
    {
        // arrange
        // an enum value is not validated against the boolean/null keywords (matching
        // HotChocolate.Language), so "true" prints bare and would re-parse as a boolean value.
        var node = new EnumValueNode("true");

        // act
        var result = node.ToString();

        // assert
        Assert.Equal("true", result);
    }
}
