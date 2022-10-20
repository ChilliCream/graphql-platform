using System;
using System.Text;
using Xunit;

namespace HotChocolate.Language;

public class SyntaxExceptionTests
{
    [Fact]
    public void SourceText_Should_BeFullText_When_LessThanRange()
    {
        // arrange
        const string sourceText = "type Error 123 { }";

        // act
        var exception = new SyntaxException(
            new Utf8GraphQLReader(Encoding.UTF8.GetBytes(sourceText)),
            "");

        // assert
        Assert.Equal(sourceText, exception.SourceText);
        Assert.Equal(0, exception.SourceTextOffset);
    }

    [Fact]
    public void SourceText_Should_SliceFromTheBeginning_When_PositionIsAtTheBeginning()
    {
        // arrange
        const string sourceText =
            """
            Error \
            type Query {
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
            }
            """;

        // act
        var exception = ParseString(sourceText);

        // assert
        Assert.Equal(sourceText.Substring(0, 512), exception.SourceText);
        Assert.Equal(0, exception.SourceTextOffset);
        Assert.Equal(
            sourceText.Substring(exception.Position, 10),
            exception.SourceText.Substring(
                exception.Position - exception.SourceTextOffset,
                10));
    }

    [Fact]
    public void SourceText_Should_SliceFromTheEnd_When_PositionIsAtTheEnd()
    {
        // arrange
        const string sourceText =
            """
            type Query {
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
            }
            Error \
            """;

        // act
        var exception = ParseString(sourceText);

        // assert
        Assert.Equal(sourceText.Substring(sourceText.Length - 512), exception.SourceText);
        Assert.Equal(sourceText.Length - 512, exception.SourceTextOffset);
        Assert.Equal(512, exception.SourceText.Length);
        Assert.Equal(
            sourceText.Substring(exception.Position - 10, 10),
            exception.SourceText.Substring(
                exception.Position - exception.SourceTextOffset - 10,
                10));
    }

    [Fact]
    public void SourceText_Should_SliceCenter_When_PositionIsAtTheCenter()
    {
        // arrange
        const string sourceText =
            """
            type Query {
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                Error \
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
                foo: String
                bar: String
                baz: String
                qux: String
                quux: String
            }
            """;

        // act
        var exception = ParseString(sourceText);

        // assert
        Assert.Equal(123, exception.SourceTextOffset);
        Assert.Equal(sourceText.Substring(123, 512), exception.SourceText);
        Assert.Equal(512, exception.SourceText.Length);
        Assert.Equal(
            sourceText.Substring(exception.Position, 10),
            exception.SourceText.Substring(exception.Position - exception.SourceTextOffset, 10));
    }

    private static SyntaxException ParseString(string source)
    {
        try
        {
            var reader = new Utf8GraphQLReader(Encoding.UTF8.GetBytes(source));

            while (reader.Read()) { }
        }
        catch (SyntaxException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("No syntax error detected thrown");
    }
}
