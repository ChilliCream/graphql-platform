using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Filtering;

public sealed class FilterLexerTests
{
    [Theory]
    [InlineData("word", "Word(word)")]
    [InlineData("#", "Word(#)")]
    [InlineData("\"q\"", "String(q)")]
    [InlineData("1.5", "Number(1.5)")]
    [InlineData("true", "Boolean(true)")]
    [InlineData(":", "Colon(:)")]
    [InlineData("(", "LeftParenthesis(()")]
    [InlineData(")", "RightParenthesis())")]
    [InlineData(",", "Comma(,)")]
    [InlineData("-", "Minus(-)")]
    [InlineData("*", "Star(*)")]
    [InlineData("x:>", "Word(x)|Colon(:)|GreaterThan(>)")]
    [InlineData("x:>=5", "Word(x)|Colon(:)|GreaterThanOrEqual(>=)|Number(5)")]
    [InlineData("x:<", "Word(x)|Colon(:)|LessThan(<)")]
    [InlineData("x:<5", "Word(x)|Colon(:)|LessThan(<)|Number(5)")]
    [InlineData("x:<=5", "Word(x)|Colon(:)|LessThanOrEqual(<=)|Number(5)")]
    [InlineData("a AND b", "Word(a)|And(AND)|Word(b)")]
    [InlineData("a OR b", "Word(a)|Or(OR)|Word(b)")]
    [InlineData("a:1.", "Word(a)|Colon(:)|Word(1.)")]
    [InlineData("a:.5", "Word(a)|Colon(:)|Word(.5)")]
    [InlineData("a:1.2.3", "Word(a)|Colon(:)|Word(1.2.3)")]
    [InlineData("a\tAND\nb\rc", "Word(a)|And(AND)|Word(b)|Word(c)")]
    [InlineData("IN(", "In(IN)|LeftParenthesis(()")]
    [InlineData("RANGE(", "Range(RANGE)|LeftParenthesis(()")]
    public void Tokenize_Should_ProduceEveryTokenKind_When_EachGrammarSymbolAppears(string input, string expected)
    {
        // act
        var tokens = FilterLexer.Tokenize(input);

        // assert
        Assert.Equal(expected, Describe(tokens));
    }

    [Theory]
    [InlineData("a:1", "Word@0-1|Colon@1-2|Number@2-3")]
    [InlineData(" a : 1 ", "Word@1-2|Colon@3-4|Number@5-6")]
    [InlineData("x:>=400", "Word@0-1|Colon@1-2|GreaterThanOrEqual@2-4|Number@4-7")]
    [InlineData("msg:\"a b\"", "Word@0-3|Colon@3-4|String@4-9")]
    [InlineData(
        "s:IN(1, 2)",
        "Word@0-1|Colon@1-2|In@2-4|LeftParenthesis@4-5|Number@5-6|Comma@6-7|Number@8-9|RightParenthesis@9-10")]
    [InlineData("-a", "Minus@0-1|Word@1-2")]
    [InlineData("a\\ b:1", "Word@0-4|Colon@4-5|Number@5-6")]
    public void Tokenize_Should_TrackStartAndEnd_When_TokensAreLexed(string input, string expected)
    {
        // act
        var tokens = FilterLexer.Tokenize(input);

        // assert
        Assert.Equal(expected, DescribePositions(tokens));
    }

    [Theory]
    [InlineData("http.status_code: 200", "Word(http.status_code)|Colon(:)|Number(200)")]
    [InlineData("-service:web-store", "Minus(-)|Word(service)|Colon(:)|Word(web-store)")]
    [InlineData("service:IN", "Word(service)|Colon(:)|Word(IN)")]
    [InlineData("service:RANGE", "Word(service)|Colon(:)|Word(RANGE)")]
    [InlineData("service:*", "Word(service)|Colon(:)|Star(*)")]
    [InlineData("service:*-test", "Word(service)|Colon(:)|Word(*-test*)")]
    [InlineData("field:foo\\ bar", "Word(field)|Colon(:)|Word(foo bar)")]
    [InlineData("field\\:name:value", "Word(field:name)|Colon(:)|Word(value)")]
    [InlineData("graphql.name:\\\"", "Word(graphql.name)|Colon(:)|Word(\")")]
    [InlineData("a:b\\(c", "Word(a)|Colon(:)|Word(b(c)")]
    [InlineData("name:foo*", "Word(name)|Colon(:)|Word(foo**)")]
    [InlineData("name:foo\\*", "Word(name)|Colon(:)|Word(foo*)")]
    [InlineData("name:\\\\*", "Word(name)|Colon(:)|Word(\\**)")]
    public void Tokenize_Should_ClassifyTokens_When_InputUsesGrammarForms(string input, string expected)
    {
        // act
        var tokens = FilterLexer.Tokenize(input);

        // assert
        Assert.Equal(expected, Describe(tokens));
    }

    [Theory]
    [InlineData("a:\"", "Missing closing quote", 3)]
    [InlineData("a:\"\\", "Missing closing quote", 3)]
    [InlineData("msg:\"oops", "Missing closing quote", 5)]
    [InlineData("a:1 !", "Unexpected character '!'", 5)]
    [InlineData("**", "A single * already matches any text", 2)]
    [InlineData("***", "A single * already matches any text", 2)]
    [InlineData("test:***", "A single * after the colon already checks the attribute exists", 7)]
    [InlineData("test:a***", "A single * already matches any text", 8)]
    [InlineData("test:***meow", "A single * already matches any text", 7)]
    [InlineData("test:***meow***", "A single * already matches any text", 7)]
    [InlineData("test:-***", "A single * already matches any text", 8)]
    public void Tokenize_Should_ReportMessageAndColumn_When_InputIsMalformed(string input, string message, int column)
    {
        // act
        var error = Assert.Throws<FilterParseException>(() => FilterLexer.Tokenize(input));

        // assert
        Assert.Equal(message, error.Message);
        Assert.Equal(column, error.Column);
    }

    [Fact]
    public void Tokenize_Should_UnescapeBackslashAndKeepTrailingBackslash_When_WordContainsBackslashes()
    {
        // arrange
        const string backslash = "\\";

        // act
        var escaped = FilterLexer.Tokenize("a:b" + backslash + backslash + "c");
        var trailing = FilterLexer.Tokenize("a:b" + backslash);

        // assert
        Assert.Equal(
            Expected(FilterTokenKind.Word, "b" + backslash + backslash + "c", "b" + backslash + "c", 2, 6),
            WithoutEnd(escaped)[2]);
        Assert.Equal(Expected(FilterTokenKind.Word, "b" + backslash, "b" + backslash, 2, 4), WithoutEnd(trailing)[2]);
    }

    [Fact]
    public void Tokenize_Should_ClassifyAsWord_When_EscapedRunLooksLikeKeywordNumberOrBoolean()
    {
        // act
        var actual = new[] { "\\AND", "\\true", "12\\3" }
            .Select(static input => WithoutEnd(FilterLexer.Tokenize(input))[0])
            .ToArray();

        // assert
        Assert.Equal(
            new[]
            {
                Expected(FilterTokenKind.Word, "\\AND", "AND", 0, 4),
                Expected(FilterTokenKind.Word, "\\true", "true", 0, 5),
                Expected(FilterTokenKind.Word, "12\\3", "123", 0, 4)
            },
            actual);
    }

    [Fact]
    public void Tokenize_Should_NotReportErrors_When_WildcardsAreLoneOrEscaped()
    {
        // arrange
        var inputs = new[] { "test:*", "test:*meow*", "test:*-*", "test:\\*\\*", "test:\"***\"", "test:*\\**" };
        var expected = new[]
        {
            "Word(test)|Colon(:)|Star(*)",
            "Word(test)|Colon(:)|Word(*meow**)",
            "Word(test)|Colon(:)|Word(*-**)",
            "Word(test)|Colon(:)|Word(**)",
            "Word(test)|Colon(:)|String(***)",
            "Word(test)|Colon(:)|Word(****)"
        };

        // act
        var actual = inputs.Select(static input => Describe(FilterLexer.Tokenize(input))).ToArray();

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Tokenize_Should_UnescapeQuotedString_When_StringContainsAnEscapedQuote()
    {
        // act
        var tokens = FilterLexer.Tokenize("msg:\"a\\\"b\"");

        // assert
        Assert.Equal(Expected(FilterTokenKind.String, "\"a\\\"b\"", "a\"b", 4, 10), WithoutEnd(tokens)[2]);
    }

    private static FilterToken Expected(
        FilterTokenKind kind,
        string text,
        string value,
        int start,
        int end,
        bool wildcard = false)
        => new(kind, text, value, start, end, wildcard);

    private static FilterToken[] WithoutEnd(IReadOnlyList<FilterToken> tokens)
        => tokens.Where(static token => token.Kind != FilterTokenKind.End).ToArray();

    private static string Describe(IReadOnlyList<FilterToken> tokens)
        => string.Join(
            '|',
            tokens
                .Where(static token => token.Kind != FilterTokenKind.End)
                .Select(static token => $"{token.Kind}({token.Value}{(token.HasWildcard ? "*" : string.Empty)})"));

    private static string DescribePositions(IReadOnlyList<FilterToken> tokens)
        => string.Join(
            '|',
            tokens
                .Where(static token => token.Kind != FilterTokenKind.End)
                .Select(static token => $"{token.Kind}@{token.Start}-{token.End}"));
}
