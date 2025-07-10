using System.Text;

namespace HotChocolate.Language;

public class SchemaCoordinateParserTests
{
    [Fact]
    public void ParseName()
    {
        // arrange
        const string sourceText = "MyType";
        var source = Encoding.UTF8.GetBytes(sourceText);

        // act
        var result = Utf8GraphQLParser.Syntax.ParseSchemaCoordinate(source);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void ParseNameAndMemberName()
    {
        // arrange
        const string sourceText = "MyType.MemberName";
        var source = Encoding.UTF8.GetBytes(sourceText);

        // act
        var result = Utf8GraphQLParser.Syntax.ParseSchemaCoordinate(source);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void ParseNameNameName()
    {
        // arrange
        const string sourceText = "Name.Name.Name";
        var source = Encoding.UTF8.GetBytes(sourceText);

        // act
        void Fail() => Utf8GraphQLParser.Syntax.ParseSchemaCoordinate(source);

        // assert
        Assert.Throws<SyntaxException>(Fail);
    }

    [Fact]
    public void ParseNameAndMemberNameAndArg()
    {
        // arrange
        const string sourceText = "MyType.MemberName(arg:)";
        var source = Encoding.UTF8.GetBytes(sourceText);

        // act
        var result = Utf8GraphQLParser.Syntax.ParseSchemaCoordinate(source);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void ParseDirectiveName()
    {
        // arrange
        const string sourceText = "@foo";
        var source = Encoding.UTF8.GetBytes(sourceText);

        // act
        var result = Utf8GraphQLParser.Syntax.ParseSchemaCoordinate(source);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void ParseDirectiveNameAndArg()
    {
        // arrange
        const string sourceText = "@foo(arg:)";
        var source = Encoding.UTF8.GetBytes(sourceText);

        // act
        var result = Utf8GraphQLParser.Syntax.ParseSchemaCoordinate(source);

        // assert
        result.MatchSnapshot();
    }

    [Theory]
    [InlineData("MyType.field(arg: value)")]
    [InlineData("@myDirective.field")]
    public void RejectsInvalidPatterns(string sourceText)
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(sourceText);

        // act
        var ex = Record.Exception(() =>
        {
            Utf8GraphQLParser.Syntax.ParseSchemaCoordinate(source);
        });

        // assert
        Assert.IsType<SyntaxException>(ex);
    }

    [InlineData(null)]
    [InlineData("")]
    [Theory]
    public void ParseSourceTextIsEmptyOrNull(string? s)
    {
        // arrange;
        // act
        void Fail() => Utf8GraphQLParser.Syntax.ParseSchemaCoordinate(s!);

        // assert
        Assert.Equal("sourceText", Assert.Throws<ArgumentException>(Fail).ParamName);
    }
}
