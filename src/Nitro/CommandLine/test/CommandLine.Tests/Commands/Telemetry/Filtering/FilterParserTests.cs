using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Filtering;

public sealed class FilterParserTests
{
    [Theory]
    [InlineData("a:1 )", "Expected whitespace but \")\" found", 5)]
    [InlineData("status:errorhttp.status_code:500", "Expected whitespace but \":\" found", 29)]
    [InlineData("a:1 OR", "Missing right side of OR expression", 5)]
    [InlineData("a:1 AND", "Missing right side of AND expression", 5)]
    [InlineData(":", "Unexpected ':'", 1)]
    [InlineData("status :error", "Unexpected ':'", 8)]
    [InlineData("AND a:1", "Missing left side of AND expression", 1)]
    [InlineData("OR a:1", "Missing left side of OR expression", 1)]
    [InlineData(")", "Unexpected ')'", 1)]
    [InlineData("(", "Unexpected 'end of input'", 2)]
    [InlineData("IN(1)", "Unexpected 'IN'", 1)]
    [InlineData("a:1 AND AND b:2", "Unexpected operator 'AND'", 9)]
    [InlineData("a:1 AND OR b:2", "Unexpected operator 'OR'", 9)]
    [InlineData("a:1 OR OR b:2", "Unexpected operator 'OR'", 8)]
    [InlineData("a:1 OR AND b:2", "Unexpected operator 'AND'", 8)]
    [InlineData("-", "Missing expression after negation", 1)]
    public void Parse_Should_ReportMessageAndColumn_When_TheExpressionStructureIsInvalid(
        string filter,
        string message,
        int column)
    {
        // act
        var error = Assert.Throws<FilterParseException>(() => FilterParser.Parse(filter, TelemetryFilterSignal.Traces));

        // assert
        Assert.Equal(message, error.Message);
        Assert.Equal(column, error.Column);
    }

    [Theory]
    [InlineData("service.name:", "Missing value in key:value pair", 13)]
    [InlineData("a: 1", "Missing value in key:value pair", 2)]
    [InlineData("a: :b", "Missing value in key:value pair", 2)]
    [InlineData("a::b", "Expected a value but found ':'", 3)]
    [InlineData("a:> 5", "Missing value in range expression", 2)]
    [InlineData("a:>", "Missing value in range expression", 2)]
    [InlineData("a:>\"5\"", "Ordering comparisons need a number", 4)]
    [InlineData("duration:>true", "Boolean values do not support ordering comparisons", 11)]
    [InlineData("status:-abc :x", "Unexpected ':'", 13)]
    [InlineData("x:IN(1,)", "Expected a value but found ')'", 8)]
    [InlineData("x:IN(200,", "Expected a value but found 'end of input'", 10)]
    [InlineData("x:IN(1, *)", "Expected a value but found '*'", 9)]
    [InlineData("x:IN((", "Expected a value but found '('", 6)]
    public void Parse_Should_ReportMessageAndColumn_When_TheMatcherIsInvalid(string filter, string message, int column)
    {
        // act
        var error = Assert.Throws<FilterParseException>(() => FilterParser.Parse(filter, TelemetryFilterSignal.Traces));

        // assert
        Assert.Equal(message, error.Message);
        Assert.Equal(column, error.Column);
    }

    [Theory]
    [InlineData("(a:1)", "predicate(a,Equal,number:1)")]
    [InlineData("((a:1 OR b:2))", "or(predicate(a,Equal,number:1),predicate(b,Equal,number:2))")]
    [InlineData(
        "a:1 OR b:2 OR c:3",
        "or(predicate(a,Equal,number:1),predicate(b,Equal,number:2),predicate(c,Equal,number:3))")]
    [InlineData(
        "a:1 AND b:2 AND c:3",
        "and(predicate(a,Equal,number:1),predicate(b,Equal,number:2),predicate(c,Equal,number:3))")]
    [InlineData(
        "a:1 b:2 OR c:3 d:4",
        "or(and(predicate(a,Equal,number:1),predicate(b,Equal,number:2)),and(predicate(c,Equal,number:3),predicate(d,Equal,number:4)))")]
    [InlineData(
        "a:1 AND b:2 OR c:3",
        "or(and(predicate(a,Equal,number:1),predicate(b,Equal,number:2)),predicate(c,Equal,number:3))")]
    [InlineData(
        "a:1 and b:2 or c:3",
        "and(predicate(a,Equal,number:1),term(and),predicate(b,Equal,number:2),term(or),predicate(c,Equal,number:3))")]
    [InlineData("a:1 -b:2", "and(predicate(a,Equal,number:1),not(predicate(b,Equal,number:2)))")]
    [InlineData("--a:1", "not(not(predicate(a,Equal,number:1)))")]
    [InlineData("-(a:1 b:2)", "not(and(predicate(a,Equal,number:1),predicate(b,Equal,number:2)))")]
    [InlineData("-a", "not(term(a))")]
    [InlineData("()", "term()")]
    [InlineData("\"a b\" c", "and(term(a b),term(c))")]
    [InlineData("service.name:IN(1, 2, 3)", "predicate(service.name,In,number:1,number:2,number:3)")]
    [InlineData("error:false", "predicate(error,Equal,boolean:false)")]
    [InlineData("x:IN()", "predicate(x,In,)")]
    [InlineData("x:()", "predicate(x,In,)")]
    public void Parse_Should_BuildTheExpectedTree_When_OperatorsAndGroupsCombine(string filter, string expected)
    {
        // act
        var result = FilterParser.Parse(filter, TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal(expected, Describe(result));
    }

    [Theory]
    [InlineData("@span.a:1", TelemetryFilterSignal.Traces, "predicate(@span.a,Equal,number:1)")]
    [InlineData("@event.a:1", TelemetryFilterSignal.Traces, "predicate(@event.a,Equal,number:1)")]
    [InlineData("@resource.a:1", TelemetryFilterSignal.Traces, "predicate(@resource.a,Equal,number:1)")]
    [InlineData("@log.a:1", TelemetryFilterSignal.Logs, "predicate(@log.a,Equal,number:1)")]
    [InlineData("@body.a:1", TelemetryFilterSignal.Logs, "predicate(@body.a,Equal,number:1)")]
    [InlineData("@resource.a:1", TelemetryFilterSignal.Logs, "predicate(@resource.a,Equal,number:1)")]
    [InlineData("@log.a:1", TelemetryFilterSignal.Traces, "error:Unknown scope prefix|1")]
    [InlineData("@body.a:1", TelemetryFilterSignal.Traces, "error:Unknown scope prefix|1")]
    [InlineData("@span.a:1", TelemetryFilterSignal.Logs, "error:Unknown scope prefix|1")]
    [InlineData("@event.a:1", TelemetryFilterSignal.Logs, "error:Unknown scope prefix|1")]
    [InlineData("@spanx.name:1", TelemetryFilterSignal.Traces, "error:Unknown scope prefix|1")]
    [InlineData("@span:1", TelemetryFilterSignal.Traces, "error:Expected an attribute name after the @span prefix|1")]
    [InlineData("@span.:1", TelemetryFilterSignal.Traces, "error:Expected an attribute name after the @span prefix|1")]
    [InlineData("@", TelemetryFilterSignal.Traces, "error:Scope prefixes can only be used in filter expressions|1")]
    [InlineData("@ev", TelemetryFilterSignal.Traces, "error:Scope prefixes can only be used in filter expressions|1")]
    [InlineData("@body", TelemetryFilterSignal.Traces, "error:Unknown scope prefix|1")]
    [InlineData("@body", TelemetryFilterSignal.Logs, "error:Scope prefixes can only be used in filter expressions|1")]
    [InlineData("@ev", TelemetryFilterSignal.Logs, "error:Unknown scope prefix|1")]
    [InlineData(
        "@span.x y",
        TelemetryFilterSignal.Traces,
        "error:Scope prefixes can only be used in filter expressions|1")]
    [InlineData("@span_x", TelemetryFilterSignal.Traces, "error:Unknown scope prefix|1")]
    public void Parse_Should_ValidateScopePrefixesPerSignal_When_AKeyIsScoped(
        string filter,
        TelemetryFilterSignal signal,
        string expected)
    {
        // act
        var result = DescribeOrError(filter, signal);

        // assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parse_Should_SurfaceTheLexerError_When_TheInputCannotBeTokenized()
    {
        // act
        var error =
            Assert.Throws<FilterParseException>(() =>
                FilterParser.Parse("field:\"missing", TelemetryFilterSignal.Traces)
            );

        // assert
        Assert.Equal("Missing closing quote", error.Message);
        Assert.Equal(7, error.Column);
    }

    [Theory]
    [InlineData("x:IN(", "Expected a value but found 'end of input'", 6)]
    [InlineData("x:IN(\"a\"", "Missing closing parenthesis", 5)]
    [InlineData("x:RANGE(400", "Missing closing parenthesis", 8)]
    [InlineData("x:(a OR b", "Missing closing parenthesis", 3)]
    [InlineData("x:IN(200 OR 201)", "Expected ',' or ')'", 10)]
    [InlineData("x:(a, b)", "Expected 'OR' or ')'", 5)]
    public void Parse_Should_ReportMessageAndColumn_When_GroupOrSetDelimitersAreInvalid(
        string filter,
        string message,
        int column)
    {
        // act
        var error = Assert.Throws<FilterParseException>(() => FilterParser.Parse(filter, TelemetryFilterSignal.Traces));

        // assert
        Assert.Equal(message, error.Message);
        Assert.Equal(column, error.Column);
    }

    [Theory]
    [InlineData("(status:error)env:prod", "env")]
    [InlineData("((status:error))env:prod", "env")]
    [InlineData("(timeout)failure", "failure")]
    public void Parse_Should_RequireWhitespaceAfterAGroupedPrimary_When_AnotherTermOrClauseFollows(
        string filter,
        string adjacentToken)
    {
        // act
        var error = Assert.Throws<FilterParseException>(() => FilterParser.Parse(filter, TelemetryFilterSignal.Traces));

        // assert
        Assert.Equal($"Expected whitespace but \"{adjacentToken}\" found", error.Message);
        Assert.Equal(filter.IndexOf(adjacentToken, StringComparison.Ordinal) + 1, error.Column);
    }

    [Fact]
    public void Parse_Should_ReturnNull_When_TheInputIsEmptyOrWhitespace()
    {
        // act
        var empty = FilterParser.Parse(string.Empty, TelemetryFilterSignal.Traces);
        var whitespace = FilterParser.Parse("   ", TelemetryFilterSignal.Traces);

        // assert
        Assert.Null(empty);
        Assert.Null(whitespace);
    }

    [Fact]
    public void Parse_Should_CarryWildcardOnlyForBareUnescapedStars_When_ValuesContainStars()
    {
        // act
        var wildcard = FilterParser.Parse("name:foo*", TelemetryFilterSignal.Traces);
        var escaped = FilterParser.Parse("name:foo\\*", TelemetryFilterSignal.Traces);
        var quoted = FilterParser.Parse("service.name:\"*gateway\"", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("predicate(name,Equal,string:foo*[quoted=False,wildcard=True])", DescribeDetailed(wildcard));
        Assert.Equal("predicate(name,Equal,string:foo*[quoted=False,wildcard=False])", DescribeDetailed(escaped));
        Assert.Equal(
            "predicate(service.name,Equal,string:*gateway[quoted=True,wildcard=False])",
            DescribeDetailed(quoted));
    }

    [Fact]
    public void Parse_Should_ProduceTheSameMembershipClause_When_InAndOrSugarAreUsed()
    {
        // act
        var inSet = FilterParser.Parse("service.name:IN(\"a\", \"b\")", TelemetryFilterSignal.Traces);
        var orSet = FilterParser.Parse("service.name:(\"a\" OR \"b\")", TelemetryFilterSignal.Traces);

        // assert
        const string expected =
            "predicate(service.name,In,string:a[quoted=True,wildcard=False],string:b[quoted=True,wildcard=False])";
        Assert.Equal(expected, DescribeDetailed(inSet));
        Assert.Equal(expected, DescribeDetailed(orSet));
    }

    [Fact]
    public void Parse_Should_ReadSignedNumbers_When_EveryMatcherTakesAValue()
    {
        // act
        var equality = FilterParser.Parse("duration:-5", TelemetryFilterSignal.Traces);
        var greater = FilterParser.Parse("duration:>-5", TelemetryFilterSignal.Traces);
        var less = FilterParser.Parse("duration:<=-1.5", TelemetryFilterSignal.Traces);
        var inSet = FilterParser.Parse("duration:IN(-1,2)", TelemetryFilterSignal.Traces);
        var range = FilterParser.Parse("duration:RANGE(-9,-1)", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("predicate(duration,Equal,number:-5[quoted=False,wildcard=False])", DescribeDetailed(equality));
        Assert.Equal(
            "predicate(duration,GreaterThan,number:-5[quoted=False,wildcard=False])",
            DescribeDetailed(greater));
        Assert.Equal(
            "predicate(duration,LessThanOrEqual,number:-1.5[quoted=False,wildcard=False])",
            DescribeDetailed(less));
        Assert.Equal(
            "predicate(duration,In,number:-1[quoted=False,wildcard=False],number:2[quoted=False,wildcard=False])",
            DescribeDetailed(inSet));
        Assert.Equal(
            "predicate(duration,Range,number:-9[quoted=False,wildcard=False],number:-1[quoted=False,wildcard=False])",
            DescribeDetailed(range));
    }

    [Fact]
    public void Parse_Should_ReadMinusAsNegation_When_TheMinusIsNotBoundToAValue()
    {
        // act
        var clause = FilterParser.Parse("-duration:5", TelemetryFilterSignal.Traces);
        var term = FilterParser.Parse("- 5", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("not(predicate(duration,Equal,number:5[quoted=False,wildcard=False]))", DescribeDetailed(clause));
        Assert.Equal("not(term(5))", DescribeDetailed(term));
    }

    [Fact]
    public void Parse_Should_ReadAString_When_AColonIsFollowedByALoneMinus()
    {
        // act
        var status = FilterParser.Parse("status:-", TelemetryFilterSignal.Traces);
        var scoped = FilterParser.Parse("@span.test:-", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("predicate(status,Equal,string:-[quoted=False,wildcard=False])", DescribeDetailed(status));
        Assert.Equal("predicate(@span.test,Equal,string:-[quoted=False,wildcard=False])", DescribeDetailed(scoped));
    }

    [Theory]
    [InlineData("status:-abc", "predicate(status,Equal,string:-abc[quoted=False,wildcard=False])")]
    [InlineData("status:-true", "predicate(status,Equal,string:-true[quoted=False,wildcard=False])")]
    [InlineData("status:--", "predicate(status,Equal,string:--[quoted=False,wildcard=False])")]
    [InlineData("status:-*", "predicate(status,Equal,string:-*[quoted=False,wildcard=True])")]
    [InlineData("status:*-", "predicate(status,Equal,string:*-[quoted=False,wildcard=True])")]
    [InlineData("status:--5", "predicate(status,Equal,string:--5[quoted=False,wildcard=False])")]
    public void Parse_Should_GatherTheValueText_When_AValueIsWrittenAgainstAMinus(string filter, string expected)
    {
        // act
        var result = FilterParser.Parse(filter, TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal(expected, DescribeDetailed(result));
    }

    [Fact]
    public void Parse_Should_NotConsumeTheFollowingClause_When_AMinusValueIsFollowedByWhitespace()
    {
        // act
        var result = FilterParser.Parse("status:- category:x", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal(
            "and(predicate(status,Equal,string:-[quoted=False,wildcard=False]),predicate(category,Equal,string:x[quoted=False,wildcard=False]))",
            DescribeDetailed(result));
    }

    [Fact]
    public void Parse_Should_ReadExistence_When_TheValueIsABareStar()
    {
        // act
        var existence = FilterParser.Parse("status:*", TelemetryFilterSignal.Traces);
        var negated = FilterParser.Parse("-status:*", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("predicate(status,Exists,)", DescribeDetailed(existence));
        Assert.Equal("not(predicate(status,Exists,))", DescribeDetailed(negated));
    }

    [Theory]
    [InlineData("test:>a")]
    [InlineData("test:>-")]
    [InlineData("test:<=-x")]
    public void Parse_Should_RejectTheOrderingComparison_When_TheValueIsNotANumber(string filter)
    {
        // act
        var error = ParseErrorDescription(filter, TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("Ordering comparisons need a number", error);
    }

    [Fact]
    public void Parse_Should_RejectTheSet_When_InHasMoreThanOneHundredValues()
    {
        // arrange
        var allowedValues = "s:IN(" + string.Join(',', Enumerable.Range(1, 100)) + ")";
        var tooManyValues = "s:IN(" + string.Join(',', Enumerable.Range(1, 101)) + ")";
        var tooManyOrValues = "s:(" + string.Join(" OR ", Enumerable.Range(1, 101)) + ")";

        // act
        var allowed =
            Assert.IsType<FilterPredicateNode>(FilterParser.Parse(allowedValues, TelemetryFilterSignal.Traces));
        var error = ParseErrorDescription(tooManyValues, TelemetryFilterSignal.Traces);
        var orError = ParseErrorDescription(tooManyOrValues, TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal(100, allowed.Values.Length);
        Assert.Equal("IN must not contain more than 100 values", error);
        Assert.Equal("IN must not contain more than 100 values", orError);
    }

    [Fact]
    public void Parse_Should_RejectTheRange_When_ABoundIsNotANumber()
    {
        // act
        var error = ParseErrorDescription("http.status_code:RANGE(\"a\", 500)", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("RANGE bounds must be numbers", error);
    }

    [Theory]
    [InlineData("status:@span", "predicate(status,Equal,string:@span[quoted=False,wildcard=False])")]
    [InlineData("s:IN(@span)", "predicate(s,In,string:@span[quoted=False,wildcard=False])")]
    public void Parse_Should_TreatTheAtSignAsText_When_ItAppearsInAValuePosition(string filter, string expected)
    {
        // act
        var result = FilterParser.Parse(filter, TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal(expected, DescribeDetailed(result));
    }

    [Fact]
    public void Parse_Should_TreatEscapedAndQuotedAtSignsAsText_When_ATermStartsWithAnAtSign()
    {
        // act
        var results = new[]
        {
            FilterParser.Parse("\\@span", TelemetryFilterSignal.Traces),
            FilterParser.Parse("\\@", TelemetryFilterSignal.Traces),
            FilterParser.Parse("\"@span\"", TelemetryFilterSignal.Traces)
        };

        // assert
        Assert.Equal(new[] { "term(@span)", "term(@)", "term(@span)" }, results.Select(DescribeDetailed));
    }

    [Fact]
    public void Parse_Should_ReadAString_When_AMinusIsALoneSetValue()
    {
        // act
        var result = FilterParser.Parse("status:IN(-, a)", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal(
            "predicate(status,In,string:-[quoted=False,wildcard=False],string:a[quoted=False,wildcard=False])",
            DescribeDetailed(result));
    }

    [Fact]
    public void Parse_Should_RejectTheRange_When_ItDoesNotHaveExactlyTwoBounds()
    {
        // arrange
        var inputs = new[] { "x:RANGE(400)", "x:RANGE(1, 2, 3)", "x:RANGE()" };

        // act
        var actual = inputs.Select(input => ParseErrorDescription(input, TelemetryFilterSignal.Traces)).ToArray();

        // assert
        Assert.Equal(
            new[]
            {
                "RANGE requires a minimum and maximum value",
                "RANGE requires a minimum and maximum value",
                "RANGE requires a minimum and maximum value"
            },
            actual);
    }

    private static string DescribeDetailed(FilterNode? node)
    {
        return node switch
        {
            null => "null",
            FilterAndNode and => $"and({string.Join(',', and.Children.Select(DescribeDetailed))})",
            FilterOrNode or => $"or({string.Join(',', or.Children.Select(DescribeDetailed))})",
            FilterNotNode not => $"not({DescribeDetailed(not.Child)})",
            FilterPredicateNode predicate =>
                $"predicate({predicate.Field},{predicate.Operator},{string.Join(',', predicate.Values.Select(DescribeDetailed))})",
            FilterTermNode term => $"term({term.Text})",
            _ => throw new ArgumentOutOfRangeException(nameof(node))
        };
    }

    private static string DescribeDetailed(FilterValue value)
        => $"{value.Kind.ToString().ToLowerInvariant()}:{value.Text}[quoted={value.IsQuoted},wildcard={value.HasWildcard}]";

    private static string ParseErrorDescription(string filter, TelemetryFilterSignal signal)
    {
        var error = Assert.Throws<FilterParseException>(() => FilterParser.Parse(filter, signal));
        return error.Message;
    }

    private static string DescribeOrError(string filter, TelemetryFilterSignal signal)
    {
        try
        {
            return Describe(FilterParser.Parse(filter, signal));
        }
        catch (FilterParseException error)
        {
            return $"error:{error.Message}|{error.Column}";
        }
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
