using System.Text;

namespace HotChocolate.Types;

public class FieldCollectionTests
{
    [Fact]
    public void TryGetField_Should_ReturnField_When_SmallCollectionContainsUtf8Name()
    {
        // arrange
        var fields = CreateFields("a", "b", "c");

        // act
        var found = fields.TryGetField("b"u8, out var field);

        // assert
        Assert.True(found);
        Assert.Equal("b", field!.Name);
    }

    [Fact]
    public void TryGetField_Should_ReturnField_When_LargeCollectionContainsUtf8Name()
    {
        // arrange
        var fields = CreateFields(CreateNames(20));

        // act
        var found = fields.TryGetField("field17"u8, out var field);

        // assert
        Assert.True(found);
        Assert.Equal(17, field!.Index);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(20)]
    public void TryGetField_Should_ReturnFalse_When_Utf8NameIsUnknown(int count)
    {
        // arrange
        var fields = CreateFields(CreateNames(count));

        // act
        var found = fields.TryGetField("unknown"u8, out var field);

        // assert
        Assert.False(found);
        Assert.Null(field);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(20)]
    public void TryGetField_Should_ReturnFalse_When_Utf8NameContainsNonAsciiBytes(int count)
    {
        // arrange
        var fields = CreateFields(CreateNames(count));

        // act
        var found = fields.TryGetField("fiëld1"u8, out var field);

        // assert
        Assert.False(found);
        Assert.Null(field);
    }

    [Fact]
    public void TryGetField_Should_ReturnField_When_Utf8NameExceedsStackallocThreshold()
    {
        // arrange
        var longName = "f" + new string('x', 199);
        var fields = CreateFields([.. CreateNames(20), longName]);

        // act
        var found = fields.TryGetField(Encoding.UTF8.GetBytes(longName), out var field);

        // assert
        Assert.True(found);
        Assert.Equal(longName, field!.Name);
    }

    private static string[] CreateNames(int count)
        => [.. Enumerable.Range(0, count).Select(i => "field" + i)];

    private static InputFieldCollection CreateFields(params string[] names)
    {
        var sdl = new StringBuilder("type Query { a: String } input Test {");

        foreach (var name in names)
        {
            sdl.Append(' ').Append(name).Append(": String");
        }

        sdl.Append(" }");

        var schema = SchemaBuilder.New()
            .AddDocumentFromString(sdl.ToString())
            .Use(next => context => default)
            .Create();

        return schema.Types.GetType<InputObjectType>("Test").Fields;
    }
}
