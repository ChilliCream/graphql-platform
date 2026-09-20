using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Filtering;

public sealed class FilterParserTests
{
    [Theory]
    [InlineData("service.name:", "Missing value in key:value pair", 13)]
    [InlineData("status:errorhttp.status_code:500", "Expected whitespace but \":\" found", 29)]
    [InlineData("duration:>true", "Boolean values do not support ordering comparisons", 11)]
    [InlineData("duration:RANGE(1)", "RANGE requires a minimum and maximum value", 15)]
    [InlineData("service:IN(one OR two)", "Expected ',' or ')'", 16)]
    [InlineData("@span:x", "Expected an attribute name after the @span prefix", 1)]
    [InlineData("@log.message:x", "Unknown scope prefix", 1)]
    [InlineData("@span", "Scope prefixes can only be used in filter expressions", 1)]
    [InlineData("@spanx", "Unknown scope prefix", 1)]
    public void Parse_Should_ReportPortalDiagnosticAndColumn_When_TheFilterIsInvalid(
        string filter,
        string message,
        int column)
    {
        // act
        var error = Assert.Throws<FilterParseException>(
            () => FilterParser.Parse(filter, TelemetryFilterSignal.Traces));

        // assert
        Assert.Equal(message, error.Message);
        Assert.Equal(column, error.Column);
    }

    [Theory]
    [InlineData("service.name:IN(1, 2, 3)", "predicate(service.name,In,number:1,number:2,number:3)")]
    [InlineData("duration:-5", "predicate(duration,Equal,number:-5)")]
    [InlineData("duration:>=-5.5", "predicate(duration,GreaterThanOrEqual,number:-5.5)")]
    [InlineData("status:--", "predicate(status,Equal,string:--)")]
    [InlineData("status:*-test", "predicate(status,Equal,string:*-test*)")]
    [InlineData("service.name:\"api gateway\"", "predicate(service.name,Equal,string:api gateway)")]
    [InlineData("error:false", "predicate(error,Equal,boolean:false)")]
    [InlineData("a:1 AND b:2 OR c:3", "or(and(predicate(a,Equal,number:1),predicate(b,Equal,number:2)),predicate(c,Equal,number:3))")]
    [InlineData("a:1 and b:2 or c:3", "and(predicate(a,Equal,number:1),term(and),predicate(b,Equal,number:2),term(or),predicate(c,Equal,number:3))")]
    [InlineData("   a:1     AND    b:2   ", "and(predicate(a,Equal,number:1),predicate(b,Equal,number:2))")]
    [InlineData("@resource.service.name:api", "predicate(@resource.service.name,Equal,string:api)")]
    [InlineData("@event.exception.type:TimeoutError", "predicate(@event.exception.type,Equal,string:TimeoutError)")]
    public void Parse_Should_PreservePortalAstShape_When_GrammarFormsAreWellFormed(
        string filter,
        string expected)
    {
        // act
        var result = FilterParser.Parse(filter, TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal(expected, Describe(result));
    }

    [Fact]
    public void Parse_Should_RejectMoreThanOneHundredSetValues_When_InHasTooManyEntries()
    {
        // arrange
        var filter = "s:IN(" + string.Join(',', Enumerable.Range(1, 101)) + ")";

        // act
        var error = Assert.Throws<FilterParseException>(
            () => FilterParser.Parse(filter, TelemetryFilterSignal.Traces));

        // assert
        Assert.Equal("IN must not contain more than 100 values", error.Message);
    }

    [Theory]
    [InlineData("field:\"missing")]
    [InlineData("field:foo**")]
    [InlineData("field:!")]
    public void Parse_Should_ThrowAColumnAwareException_When_LexingFails(string filter)
    {
        // act
        var error = Assert.Throws<FilterParseException>(
            () => FilterParser.Parse(filter, TelemetryFilterSignal.Traces));

        // assert
        Assert.True(error.Column > 0);
        Assert.NotEmpty(error.Message);
    }

    [Theory]
    [InlineData("x:IN(", "Expected a value but found 'end of input'", 6)]
    [InlineData("x:RANGE(400", "Missing closing parenthesis", 8)]
    [InlineData("x:(a OR b", "Missing closing parenthesis", 3)]
    [InlineData("x:IN(200 OR 201)", "Expected ',' or ')'", 10)]
    [InlineData("x:(a, b)", "Expected 'OR' or ')'", 5)]
    public void Parse_Should_RejectMalformedPortalGroupsAndSets_When_TheirDelimitersAreInvalid(
        string filter,
        string message,
        int column)
    {
        // act
        var error = Assert.Throws<FilterParseException>(
            () => FilterParser.Parse(filter, TelemetryFilterSignal.Traces));

        // assert
        Assert.Equal(message, error.Message);
        Assert.Equal(column, error.Column);
    }

    [Theory]
    [InlineData("(status:error)env:prod", "env")]
    [InlineData("((status:error))env:prod", "env")]
    [InlineData("(status:error OR status:ok)env:prod", "env")]
    [InlineData("(timeout)failure", "failure")]
    [InlineData("(timeout OR failure)service.name:checkout", "service.name")]
    public void Parse_Should_RequireWhitespaceAfterAGroupedPrimary_When_AnotherTermOrClauseFollows(
        string filter,
        string adjacentToken)
    {
        // act
        var error = Assert.Throws<FilterParseException>(
            () => FilterParser.Parse(filter, TelemetryFilterSignal.Traces));

        // assert
        Assert.Equal($"Expected whitespace but \"{adjacentToken}\" found", error.Message);
        Assert.Equal(filter.IndexOf(adjacentToken, StringComparison.Ordinal) + 1, error.Column);
    }

    private static string Describe(FilterNode? node)
    {
        return node switch
        {
            null => "null",
            FilterAndNode and => $"and({string.Join(',', and.Children.Select(Describe))})",
            FilterOrNode or => $"or({string.Join(',', or.Children.Select(Describe))})",
            FilterNotNode not => $"not({Describe(not.Child)})",
            FilterPredicateNode predicate =>
                $"predicate({predicate.Field},{predicate.Operator},{string.Join(',', predicate.Values.Select(Describe))})",
            FilterTermNode term => $"term({term.Text})",
            _ => throw new ArgumentOutOfRangeException(nameof(node))
        };
    }

    private static string Describe(FilterValue value)
    {
        var wildcard = value.HasWildcard ? "*" : string.Empty;
        return $"{value.Kind.ToString().ToLowerInvariant()}:{value.Text}{wildcard}";
    }
}
