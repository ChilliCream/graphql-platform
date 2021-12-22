using System;
using System.Text;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language;

public class SchemaCoordinateParserTests
{
    [Fact]
    public void ParseName()
    {
        // arrange
        string sourceText = "MyType";
        byte[] source = Encoding.UTF8.GetBytes(sourceText);

        // act
        SchemaCoordinateNode result = Utf8GraphQLParser.Syntax.ParseSchemaCoordinate(source);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void ParseNameAndMemberName()
    {
        // arrange
        string sourceText = "MyType.MemberName";
        byte[] source = Encoding.UTF8.GetBytes(sourceText);

        // act
        SchemaCoordinateNode result = Utf8GraphQLParser.Syntax.ParseSchemaCoordinate(source);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void ParseNameAndMemberNameAndArg()
    {
        // arrange
        string sourceText = "MyType.MemberName(arg:)";
        byte[] source = Encoding.UTF8.GetBytes(sourceText);

        // act
        SchemaCoordinateNode result = Utf8GraphQLParser.Syntax.ParseSchemaCoordinate(source);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void ParseDirectiveName()
    {
        // arrange
        string sourceText = "@foo";
        byte[] source = Encoding.UTF8.GetBytes(sourceText);

        // act
        SchemaCoordinateNode result = Utf8GraphQLParser.Syntax.ParseSchemaCoordinate(source);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void ParseDirectiveNameAndArg()
    {
        // arrange
        string sourceText = "@foo(arg:)";
        byte[] source = Encoding.UTF8.GetBytes(sourceText);

        // act
        SchemaCoordinateNode result = Utf8GraphQLParser.Syntax.ParseSchemaCoordinate(source);

        // assert
        result.MatchSnapshot();
    }

    [Fact(Skip = "Do we need to reject this, if so how?")]
    public void RejectsNameNameName()
    {
        // arrange
        string sourceText = "Name.Name.Name";
        byte[] source = Encoding.UTF8.GetBytes(sourceText);

        // act
        Exception ex = Record.Exception(() =>
        {
            Utf8GraphQLParser.Syntax.ParseSchemaCoordinate(source);
        });

        // assert
        SyntaxException syntaxException = Assert.IsType<SyntaxException>(ex);
        syntaxException.Message.MatchSnapshot();
    }

    [Theory]
    [InlineData("MyType.field(arg: value)")]
    [InlineData("@myDirective.field")]
    public void RejectsInvalidPatterns(string sourceText)
    {
        // arrange
        byte[] source = Encoding.UTF8.GetBytes(sourceText);

        // act
        Exception ex = Record.Exception(() =>
        {
            Utf8GraphQLParser.Syntax.ParseSchemaCoordinate(source);
        });

        // assert
        Assert.IsType<SyntaxException>(ex);
    }
}
