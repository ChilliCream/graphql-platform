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
    [InlineData("a:\"q\"", "Word(a)|Colon(:)|String(q)")]
    [InlineData("a:true", "Word(a)|Colon(:)|Boolean(true)")]
    [InlineData("a:1.5", "Word(a)|Colon(:)|Number(1.5)")]
    [InlineData("a:1.", "Word(a)|Colon(:)|Word(1.)")]
    [InlineData("a:.5", "Word(a)|Colon(:)|Word(.5)")]
    [InlineData("a:1.2.3", "Word(a)|Colon(:)|Word(1.2.3)")]
    [InlineData("a\tAND\nb\rc", "Word(a)|And(AND)|Word(b)|Word(c)")]
    [InlineData("IN(", "In(IN)|LeftParenthesis(()")]
    [InlineData("RANGE(", "Range(RANGE)|LeftParenthesis(()")]
    public void Tokenize_Should_ProduceEveryTokenKind_When_EachGrammarSymbolAppears(
        string input,
        string expected)
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
    [InlineData("s:IN(1, 2)", "Word@0-1|Colon@1-2|In@2-4|LeftParenthesis@4-5|Number@5-6|Comma@6-7|Number@8-9|RightParenthesis@9-10")]
    [InlineData("-a", "Minus@0-1|Word@1-2")]
    [InlineData("a\\ b:1", "Word@0-4|Colon@4-5|Number@5-6")]
    public void Tokenize_Should_TrackStartAndEnd_When_TokensAreSeparatedByWhitespace(
        string input,
        string expected)
    {
        // act
        var tokens = FilterLexer.Tokenize(input);

        // assert
        Assert.Equal(expected, DescribePositions(tokens));
    }

    [Theory]
    [InlineData("http.status_code: 200", "Word(http.status_code)|Colon(:)|Number(200)")]
    [InlineData("-service:web-store", "Minus(-)|Word(service)|Colon(:)|Word(web-store)")]
    [InlineData("x:>=400", "Word(x)|Colon(:)|GreaterThanOrEqual(>=)|Number(400)")]
    [InlineData("x:IN(1,2)", "Word(x)|Colon(:)|In(IN)|LeftParenthesis(()|Number(1)|Comma(,)|Number(2)|RightParenthesis())")]
    [InlineData("service:IN", "Word(service)|Colon(:)|Word(IN)")]
    [InlineData("x:RANGE(1, 3)", "Word(x)|Colon(:)|Range(RANGE)|LeftParenthesis(()|Number(1)|Comma(,)|Number(3)|RightParenthesis())")]
    [InlineData("service:*", "Word(service)|Colon(:)|Star(*)")]
    [InlineData("service:*-test", "Word(service)|Colon(:)|Word(*-test*)")]
    [InlineData("error:true ValidateWatermark", "Word(error)|Colon(:)|Boolean(true)|Word(ValidateWatermark)")]
    [InlineData("field:foo\\ bar", "Word(field)|Colon(:)|Word(foo bar)")]
    [InlineData("field\\:name:value", "Word(field:name)|Colon(:)|Word(value)")]
    [InlineData("name:foo*", "Word(name)|Colon(:)|Word(foo**)")]
    [InlineData("name:foo\\*", "Word(name)|Colon(:)|Word(foo*)")]
    [InlineData("name:\\\\*", "Word(name)|Colon(:)|Word(\\**)")]
    public void Tokenize_Should_PortPortalTokenClassification_When_InputUsesGrammarForms(
        string input,
        string expected)
    {
        // act
        var tokens = FilterLexer.Tokenize(input);

        // assert
        Assert.Equal(expected, Describe(tokens));
    }

    [Theory]
    [InlineData("\"", "Missing closing quote", 1)]
    [InlineData("a:\"", "Missing closing quote", 3)]
    [InlineData("a:\"\\", "Missing closing quote", 3)]
    [InlineData("a:1 !", "Unexpected character '!'", 5)]
    [InlineData("msg:\"oops", "Missing closing quote", 5)]
    [InlineData("test:**", "A single * after the colon already checks the attribute exists", 7)]
    [InlineData("test:a***", "A single * already matches any text", 8)]
    [InlineData("test:!", "Unexpected character '!'", 6)]
    public void Tokenize_Should_ReportPortalLexErrors_When_InputIsMalformed(
        string input,
        string message,
        int column)
    {
        // act
        var error = Assert.Throws<FilterParseException>(() => FilterLexer.Tokenize(input));

        // assert
        Assert.Equal(message, error.Message);
        Assert.Equal(column, error.Column);
    }

    [Fact]
    public void Tokenize_Should_PreservePortalMetadata_When_LexingASimpleEqualityClause()
    {
        // act
        var tokens = FilterLexer.Tokenize("http.status_code: 200");

        // assert
        Assert.Equal(
            new[]
            {
                Expected(FilterTokenKind.Word, "http.status_code", "http.status_code", 0, 16),
                Expected(FilterTokenKind.Colon, ":", ":", 16, 17),
                Expected(FilterTokenKind.Number, "200", "200", 18, 21)
            },
            WithoutEnd(tokens));
    }

    [Fact]
    public void Tokenize_Should_KeepLeadingDashAndInternalDashDistinct_When_LexingAWord()
    {
        // act
        var tokens = FilterLexer.Tokenize("-service:web-store");

        // assert
        Assert.Equal(
            new[]
            {
                Expected(FilterTokenKind.Minus, "-", "-", 0, 1),
                Expected(FilterTokenKind.Word, "service", "service", 1, 8),
                Expected(FilterTokenKind.Colon, ":", ":", 8, 9),
                Expected(FilterTokenKind.Word, "web-store", "web-store", 9, 18)
            },
            WithoutEnd(tokens));
    }

    [Fact]
    public void Tokenize_Should_PreserveComparisonAndSetTokenMetadata_When_OperatorsAreUsed()
    {
        // act
        var comparison = FilterLexer.Tokenize("x: >= 400");
        var set = FilterLexer.Tokenize("x: IN(1,2)");

        // assert
        Assert.Equal(
            new[]
            {
                Expected(FilterTokenKind.Word, "x", "x", 0, 1),
                Expected(FilterTokenKind.Colon, ":", ":", 1, 2),
                Expected(FilterTokenKind.GreaterThanOrEqual, ">=", ">=", 3, 5),
                Expected(FilterTokenKind.Number, "400", "400", 6, 9)
            },
            WithoutEnd(comparison));
        Assert.Equal(
            new[]
            {
                Expected(FilterTokenKind.Word, "x", "x", 0, 1),
                Expected(FilterTokenKind.Colon, ":", ":", 1, 2),
                Expected(FilterTokenKind.In, "IN", "IN", 3, 5),
                Expected(FilterTokenKind.LeftParenthesis, "(", "(", 5, 6),
                Expected(FilterTokenKind.Number, "1", "1", 6, 7),
                Expected(FilterTokenKind.Comma, ",", ",", 7, 8),
                Expected(FilterTokenKind.Number, "2", "2", 8, 9),
                Expected(FilterTokenKind.RightParenthesis, ")", ")", 9, 10)
            },
            WithoutEnd(set));
    }

    [Fact]
    public void Tokenize_Should_NotClassifyInAsAKeyword_When_ItIsNotFollowedByAParenthesis()
    {
        // act
        var tokens = FilterLexer.Tokenize("service: in");

        // assert
        Assert.Equal(
            new[]
            {
                Expected(FilterTokenKind.Word, "service", "service", 0, 7),
                Expected(FilterTokenKind.Colon, ":", ":", 7, 8),
                Expected(FilterTokenKind.Word, "in", "in", 9, 11)
            },
            WithoutEnd(tokens));
    }

    [Fact]
    public void Tokenize_Should_ClassifyRangeOnlyWhenFollowedByAParenthesis()
    {
        // act
        var range = FilterLexer.Tokenize("x:RANGE(1, 3)");
        var word = FilterLexer.Tokenize("service: RANGE");

        // assert
        Assert.Equal(
            new[]
            {
                Expected(FilterTokenKind.Word, "x", "x", 0, 1),
                Expected(FilterTokenKind.Colon, ":", ":", 1, 2),
                Expected(FilterTokenKind.Range, "RANGE", "RANGE", 2, 7),
                Expected(FilterTokenKind.LeftParenthesis, "(", "(", 7, 8),
                Expected(FilterTokenKind.Number, "1", "1", 8, 9),
                Expected(FilterTokenKind.Comma, ",", ",", 9, 10),
                Expected(FilterTokenKind.Number, "3", "3", 11, 12),
                Expected(FilterTokenKind.RightParenthesis, ")", ")", 12, 13)
            },
            WithoutEnd(range));
        Assert.Equal(
            new[]
            {
                Expected(FilterTokenKind.Word, "service", "service", 0, 7),
                Expected(FilterTokenKind.Colon, ":", ":", 7, 8),
                Expected(FilterTokenKind.Word, "RANGE", "RANGE", 9, 14)
            },
            WithoutEnd(word));
    }

    [Fact]
    public void Tokenize_Should_DistinguishExistenceFromGluedWildcards_When_LexingStars()
    {
        // act
        var existence = FilterLexer.Tokenize("service:*");
        var spacedExistence = FilterLexer.Tokenize("service: *");
        var leadingPattern = FilterLexer.Tokenize("service:*-test");
        var middlePattern = FilterLexer.Tokenize("service:*gateway");

        // assert
        Assert.Equal(
            new[]
            {
                Expected(FilterTokenKind.Word, "service", "service", 0, 7),
                Expected(FilterTokenKind.Colon, ":", ":", 7, 8),
                Expected(FilterTokenKind.Star, "*", "*", 8, 9)
            },
            WithoutEnd(existence));
        Assert.Equal(
            new[]
            {
                Expected(FilterTokenKind.Word, "service", "service", 0, 7),
                Expected(FilterTokenKind.Colon, ":", ":", 7, 8),
                Expected(FilterTokenKind.Star, "*", "*", 9, 10)
            },
            WithoutEnd(spacedExistence));
        Assert.Equal(
            Expected(FilterTokenKind.Word, "*-test", "*-test", 8, 14, wildcard: true),
            WithoutEnd(leadingPattern)[2]);
        Assert.Equal(
            Expected(FilterTokenKind.Word, "*gateway", "*gateway", 8, 16, wildcard: true),
            WithoutEnd(middlePattern)[2]);
    }

    [Fact]
    public void Tokenize_Should_UnquoteStringsAndReportAnUnterminatedString()
    {
        // act
        var tokens = FilterLexer.Tokenize("msg: \"hello world\"");
        var error = LexError("msg: \"oops");

        // assert
        Assert.Equal(
            Expected(FilterTokenKind.String, "\"hello world\"", "hello world", 5, 18),
            WithoutEnd(tokens)[2]);
        Assert.Equal("Missing closing quote|6", error);
    }

    [Fact]
    public void Tokenize_Should_ClassifyBooleansAndFreeTextWords()
    {
        // act
        var tokens = FilterLexer.Tokenize("error: true ValidateWatermark");

        // assert
        Assert.Equal(
            new[]
            {
                Expected(FilterTokenKind.Word, "error", "error", 0, 5),
                Expected(FilterTokenKind.Colon, ":", ":", 5, 6),
                Expected(FilterTokenKind.Boolean, "true", "true", 7, 11),
                Expected(FilterTokenKind.Word, "ValidateWatermark", "ValidateWatermark", 12, 29)
            },
            WithoutEnd(tokens));
    }

    [Fact]
    public void Tokenize_Should_PreserveEscapedBreakCharactersAsOneWordToken()
    {
        // arrange
        const string backslash = "\\";
        var inputs = new[]
        {
            "graphql.name:" + backslash + "\"",
            "a:b" + backslash + "(c",
            "a:b" + backslash + ")c",
            "a:b" + backslash + ":c"
        };
        var expected = new[]
        {
            Expected(FilterTokenKind.Word, backslash + "\"", "\"", 13, 15),
            Expected(FilterTokenKind.Word, "b" + backslash + "(c", "b(c", 2, 6),
            Expected(FilterTokenKind.Word, "b" + backslash + ")c", "b)c", 2, 6),
            Expected(FilterTokenKind.Word, "b" + backslash + ":c", "b:c", 2, 6)
        };

        // act
        var actual = inputs
            .Select(static input => WithoutEnd(FilterLexer.Tokenize(input))[2])
            .ToArray();

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Tokenize_Should_UnescapeABackslashAndKeepATrailingBackslashLiteral()
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
        Assert.Equal(
            Expected(FilterTokenKind.Word, "b" + backslash, "b" + backslash, 2, 4),
            WithoutEnd(trailing)[2]);
    }

    [Fact]
    public void Tokenize_Should_AllowAnEscapeToStartAValueToken()
    {
        // arrange
        const string backslash = "\\";

        // act
        var tokens = FilterLexer.Tokenize("graphql.name:" + backslash + "\"");

        // assert
        Assert.Equal(
            Expected(FilterTokenKind.Word, backslash + "\"", "\"", 13, 15),
            WithoutEnd(tokens)[2]);
    }

    [Fact]
    public void Tokenize_Should_OptEscapedRunsOutOfKeywordNumberAndBooleanClassification()
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
    public void Tokenize_Should_KeepAnEscapedSpaceInsideOneWordToken()
    {
        // act
        var tokens = FilterLexer.Tokenize("field:foo\\ bar");

        // assert
        Assert.Equal(
            Expected(FilterTokenKind.Word, "foo\\ bar", "foo bar", 6, 14),
            WithoutEnd(tokens)[2]);
    }

    [Fact]
    public void Tokenize_Should_BindAnEscapedColonIntoTheWord()
    {
        // act
        var tokens = FilterLexer.Tokenize("field\\:x");

        // assert
        Assert.Equal(
            new[] { Expected(FilterTokenKind.Word, "field\\:x", "field:x", 0, 8) },
            WithoutEnd(tokens));
    }

    [Fact]
    public void Tokenize_Should_FlagOnlyUnescapedWildcards()
    {
        // act
        var wildcard = FilterLexer.Tokenize("name:foo*");
        var escaped = FilterLexer.Tokenize("name:foo\\*");

        // assert
        Assert.Equal(
            Expected(FilterTokenKind.Word, "foo*", "foo*", 5, 9, wildcard: true),
            WithoutEnd(wildcard)[2]);
        Assert.Equal(
            Expected(FilterTokenKind.Word, "foo\\*", "foo*", 5, 10),
            WithoutEnd(escaped)[2]);
    }

    [Fact]
    public void Tokenize_Should_FlagAWildcardAfterAnEscapedBackslash()
    {
        // arrange
        const string backslash = "\\";

        // act
        var tokens = FilterLexer.Tokenize("name:" + backslash + backslash + "*");

        // assert
        Assert.Equal(
            Expected(FilterTokenKind.Word, backslash + backslash + "*", backslash + "*", 5, 8, wildcard: true),
            WithoutEnd(tokens)[2]);
    }

    [Fact]
    public void Tokenize_Should_ReportTheFirstRepeatedWildcardWithPortalMessage()
    {
        // arrange
        var inputs = new[]
        {
            "***",
            "**",
            "test:a****",
            "test:***meow***",
            "test:***",
            "test:**",
            "test:***meow",
            "test:a***",
            "test:-***"
        };
        var expected = new[]
        {
            "A single * already matches any text|2",
            "A single * already matches any text|2",
            "A single * already matches any text|8",
            "A single * already matches any text|7",
            "A single * after the colon already checks the attribute exists|7",
            "A single * after the colon already checks the attribute exists|7",
            "A single * already matches any text|7",
            "A single * already matches any text|8",
            "A single * already matches any text|8"
        };

        // act
        var actual = inputs.Select(LexError).ToArray();

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Tokenize_Should_LeaveLoneWildcardsAndEscapedRunsWithoutErrors()
    {
        // arrange
        var inputs = new[]
        {
            "test:*",
            "test:*meow*",
            "test:*-*",
            "test:\\*\\*",
            "test:\"***\"",
            "test:*\\**"
        };
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
        var actual = inputs
            .Select(static input => Describe(FilterLexer.Tokenize(input)))
            .ToArray();

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Tokenize_Should_UnescapeQuotedStringEscapes()
    {
        // act
        var tokens = FilterLexer.Tokenize("msg:\"a\\\"b\"");

        // assert
        Assert.Equal(
            Expected(FilterTokenKind.String, "\"a\\\"b\"", "a\"b", 4, 10),
            WithoutEnd(tokens)[2]);
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

    private static string LexError(string input)
    {
        var error = Assert.Throws<FilterParseException>(() => FilterLexer.Tokenize(input));
        return $"{error.Message}|{error.Column}";
    }

    private static string Describe(IReadOnlyList<FilterToken> tokens)
        => string.Join(
            '|',
            tokens
                .Where(static token => token.Kind != FilterTokenKind.End)
                .Select(static token =>
                    $"{token.Kind}({token.Value}{(token.HasWildcard ? "*" : string.Empty)})"));

    private static string DescribePositions(IReadOnlyList<FilterToken> tokens)
        => string.Join(
            '|',
            tokens
                .Where(static token => token.Kind != FilterTokenKind.End)
                .Select(static token => $"{token.Kind}@{token.Start}-{token.End}"));
}
