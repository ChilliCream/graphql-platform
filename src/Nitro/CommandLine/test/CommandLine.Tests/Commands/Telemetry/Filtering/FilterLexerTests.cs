using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Filtering;

public sealed class FilterLexerTests
{
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

    private static string Describe(IReadOnlyList<FilterToken> tokens)
        => string.Join(
            '|',
            tokens
                .Where(static token => token.Kind != FilterTokenKind.End)
                .Select(static token =>
                    $"{token.Kind}({token.Value}{(token.HasWildcard ? "*" : string.Empty)})"));
}
