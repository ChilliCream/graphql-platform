using IOPath = System.IO.Path;

namespace HotChocolate;

public class SchemaBuilderExtensionsDocumentTests
{
    [Fact]
    public void AddDocumentFromFile_Builder_Is_Null()
    {
        // arrange
        // act
        Action action = () =>
            SchemaBuilderExtensions.AddDocumentFromFile(null, "abc");

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void AddDocumentFromFile_File_Is_Null()
    {
        // arrange
        var builder = SchemaBuilder.New();

        // act
        Action action = () =>
            SchemaBuilderExtensions.AddDocumentFromFile(builder, null);

        // assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void AddDocumentFromFile_File_Is_Empty()
    {
        // arrange
        var builder = SchemaBuilder.New();

        // act
        Action action = () =>
            SchemaBuilderExtensions.AddDocumentFromFile(
                builder, string.Empty);

        // assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public async Task AddDocumentFromFile()
    {
        // arrange
        var builder = SchemaBuilder.New();
        var file = IOPath.GetTempFileName();
        await File.WriteAllTextAsync(file, "type Query { a: String }");

        // act
        builder.AddDocumentFromFile(file);

        // assert
        var schema = builder.Use(next => next).Create();

        schema.ToString().MatchSnapshot();
    }
}
