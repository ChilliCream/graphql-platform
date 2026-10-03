using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Filtering;

public sealed class FilterParserTests
{
    [Theory]
    [InlineData("a:1 )", "Expected whitespace but \")\" found", 5)]
    [InlineData("a:1 ,", "Expected whitespace but \",\" found", 5)]
    [InlineData("a:1 >", "Expected whitespace but \">\" found", 5)]
    [InlineData("a:1 OR", "Missing right side of OR expression", 5)]
    [InlineData("a:1 OR )", "Missing right side of OR expression", 5)]
    [InlineData("a:1 AND", "Missing right side of AND expression", 5)]
    [InlineData("a:1 AND )", "Missing right side of AND expression", 5)]
    [InlineData(":", "Unexpected ':'", 1)]
    [InlineData("status :error", "Unexpected ':'", 8)]
    [InlineData("AND a:1", "Missing left side of AND expression", 1)]
    [InlineData("OR a:1", "Missing left side of OR expression", 1)]
    [InlineData(")", "Unexpected ')'", 1)]
    [InlineData("(", "Unexpected 'end of input'", 2)]
    [InlineData("IN(1)", "Unexpected 'IN'", 1)]
    [InlineData("RANGE(1,2)", "Unexpected 'RANGE'", 1)]
    [InlineData(">5", "Unexpected '>'", 1)]
    [InlineData(",", "Unexpected ','", 1)]
    [InlineData("a:1 AND AND b:2", "Unexpected operator 'AND'", 9)]
    [InlineData("a:1 AND OR b:2", "Unexpected operator 'OR'", 9)]
    [InlineData("a:1 OR OR b:2", "Unexpected operator 'OR'", 8)]
    [InlineData("a:1 OR AND b:2", "Unexpected operator 'AND'", 8)]
    [InlineData("-", "Missing expression after negation", 1)]
    [InlineData("- AND", "Missing expression after negation", 1)]
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
    [InlineData("a: 1", "Missing value in key:value pair", 2)]
    [InlineData("a:)", "Missing value in key:value pair", 2)]
    [InlineData("a: :b", "Missing value in key:value pair", 2)]
    [InlineData("a::b", "Expected a value but found ':'", 3)]
    [InlineData("a:> 5", "Missing value in range expression", 2)]
    [InlineData("a:>", "Missing value in range expression", 2)]
    [InlineData("a:>)", "Missing value in range expression", 2)]
    [InlineData("a:<=", "Missing value in range expression", 2)]
    [InlineData("a:>\"5\"", "Ordering comparisons need a number", 4)]
    [InlineData("status:-abc :x", "Unexpected ':'", 13)]
    [InlineData("x:IN(1,)", "Expected a value but found ')'", 8)]
    [InlineData("x:IN(1, *)", "Expected a value but found '*'", 9)]
    [InlineData("x:IN((", "Expected a value but found '('", 6)]
    [InlineData("x:(a OR )", "Expected a value but found ')'", 9)]
    [InlineData("x:RANGE(1,)", "Expected a value but found ')'", 11)]
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
    [InlineData("a:1 -b:2", "and(predicate(a,Equal,number:1),not(predicate(b,Equal,number:2)))")]
    [InlineData("--a:1", "not(not(predicate(a,Equal,number:1)))")]
    [InlineData("-(a:1 b:2)", "not(and(predicate(a,Equal,number:1),predicate(b,Equal,number:2)))")]
    [InlineData("-a", "not(term(a))")]
    [InlineData("()", "term()")]
    [InlineData("() ()", "and(term(),term())")]
    [InlineData("() OR ()", "or(term(),term())")]
    [InlineData("-()", "not(term())")]
    [InlineData("\"a b\" c", "and(term(a b),term(c))")]
    [InlineData("a:\"x\\\"y\"", "predicate(a,Equal,string:x\"y)")]
    [InlineData("x:IN()", "predicate(x,In,)")]
    [InlineData("x:()", "predicate(x,In,)")]
    [InlineData("a:1\tAND\nb:2\r", "and(predicate(a,Equal,number:1),predicate(b,Equal,number:2))")]
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
    [InlineData("@span:1", TelemetryFilterSignal.Traces, "error:Expected an attribute name after the @span prefix|1")]
    [InlineData("@span.:1", TelemetryFilterSignal.Traces, "error:Expected an attribute name after the @span prefix|1")]
    [InlineData("@event:1", TelemetryFilterSignal.Traces, "error:Expected an attribute name after the @event prefix|1")]
    [InlineData(
        "@resource.:1",
        TelemetryFilterSignal.Traces,
        "error:Expected an attribute name after the @resource prefix|1")]
    [InlineData("@log:1", TelemetryFilterSignal.Logs, "error:Expected an attribute name after the @log prefix|1")]
    [InlineData("@body.:1", TelemetryFilterSignal.Logs, "error:Expected an attribute name after the @body prefix|1")]
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
        var error = Assert.Throws<FilterParseException>(() => FilterParser.Parse(filter, TelemetryFilterSignal.Traces));

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
    [InlineData(
        "a:1 AND b:2 OR c:3",
        "or(and(predicate(a,Equal,number:1),predicate(b,Equal,number:2)),predicate(c,Equal,number:3))")]
    [InlineData(
        "a:1 and b:2 or c:3",
        "and(predicate(a,Equal,number:1),term(and),predicate(b,Equal,number:2),term(or),predicate(c,Equal,number:3))")]
    [InlineData("   a:1     AND    b:2   ", "and(predicate(a,Equal,number:1),predicate(b,Equal,number:2))")]
    [InlineData("@resource.service.name:api", "predicate(@resource.service.name,Equal,string:api)")]
    [InlineData("@event.exception.type:TimeoutError", "predicate(@event.exception.type,Equal,string:TimeoutError)")]
    public void Parse_Should_PreservePortalAstShape_When_GrammarFormsAreWellFormed(string filter, string expected)
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
        var error = Assert.Throws<FilterParseException>(() => FilterParser.Parse(filter, TelemetryFilterSignal.Traces));

        // assert
        Assert.Equal("IN must not contain more than 100 values", error.Message);
    }

    [Theory]
    [InlineData("field:\"missing", "Missing closing quote", 7)]
    [InlineData("field:foo**", "A single * already matches any text", 11)]
    [InlineData("field:!", "Unexpected character '!'", 7)]
    public void Parse_Should_SurfaceLexerErrors_When_TheInputCannotBeTokenized(
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
        var error = Assert.Throws<FilterParseException>(() => FilterParser.Parse(filter, TelemetryFilterSignal.Traces));

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
        var error = Assert.Throws<FilterParseException>(() => FilterParser.Parse(filter, TelemetryFilterSignal.Traces));

        // assert
        Assert.Equal($"Expected whitespace but \"{adjacentToken}\" found", error.Message);
        Assert.Equal(filter.IndexOf(adjacentToken, StringComparison.Ordinal) + 1, error.Column);
    }

    [Fact]
    public void Portal_Parse_Should_ReturnNullForEmptyInput()
    {
        // act
        var empty = FilterParser.Parse(string.Empty, TelemetryFilterSignal.Traces);
        var whitespace = FilterParser.Parse("   ", TelemetryFilterSignal.Traces);

        // assert
        Assert.Null(empty);
        Assert.Null(whitespace);
    }

    [Fact]
    public void Portal_Parse_Should_PreserveEqualityClauseAst()
    {
        // act
        var result = FilterParser.Parse("http.status_code:200", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal(
            "predicate(http.status_code,Equal,number:200[quoted=False,wildcard=False])",
            DescribeDetailed(result));
    }

    [Fact]
    public void Portal_Parse_Should_PreserveComparisonExistenceAndPatternValues()
    {
        // act
        var comparison = FilterParser.Parse("http.status_code:>=400", TelemetryFilterSignal.Traces);
        var existence = FilterParser.Parse("http.status_code:*", TelemetryFilterSignal.Traces);
        var pattern = FilterParser.Parse("http.url:*/api/*", TelemetryFilterSignal.Traces);
        var quoted = FilterParser.Parse("service.name:\"*gateway\"", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal(
            "predicate(http.status_code,GreaterThanOrEqual,number:400[quoted=False,wildcard=False])",
            DescribeDetailed(comparison));
        Assert.Equal("predicate(http.status_code,Exists,)", DescribeDetailed(existence));
        Assert.Equal("predicate(http.url,Equal,string:*/api/*[quoted=False,wildcard=True])", DescribeDetailed(pattern));
        Assert.Equal(
            "predicate(service.name,Equal,string:*gateway[quoted=True,wildcard=False])",
            DescribeDetailed(quoted));
    }

    [Fact]
    public void Portal_Parse_Should_CarryWildcardOnlyForUnescapedValues()
    {
        // act
        var wildcard = FilterParser.Parse("name:foo*", TelemetryFilterSignal.Traces);
        var escaped = FilterParser.Parse("name:foo\\*", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("predicate(name,Equal,string:foo*[quoted=False,wildcard=True])", DescribeDetailed(wildcard));
        Assert.Equal("predicate(name,Equal,string:foo*[quoted=False,wildcard=False])", DescribeDetailed(escaped));
    }

    [Fact]
    public void Portal_Parse_Should_TreatInAndOrSetSugarAsTheSameMembershipClause()
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
    public void Portal_Parse_Should_PreserveRangeBoundsAsNumbers()
    {
        // act
        var result = FilterParser.Parse("http.status_code:RANGE(400, 500)", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal(
            "predicate(http.status_code,Range,number:400[quoted=False,wildcard=False],number:500[quoted=False,wildcard=False])",
            DescribeDetailed(result));
    }

    [Fact]
    public void Portal_Parse_Should_ReadSignedNumbersInEveryMatcherPosition()
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
    public void Portal_Parse_Should_KeepUnboundMinusAsNegation()
    {
        // act
        var clause = FilterParser.Parse("-duration:5", TelemetryFilterSignal.Traces);
        var term = FilterParser.Parse("- 5", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("not(predicate(duration,Equal,number:5[quoted=False,wildcard=False]))", DescribeDetailed(clause));
        Assert.Equal("not(term(5))", DescribeDetailed(term));
    }

    [Fact]
    public void Portal_Parse_Should_ReadALoneMinusAfterAColonAsAString()
    {
        // act
        var status = FilterParser.Parse("status:-", TelemetryFilterSignal.Traces);
        var scoped = FilterParser.Parse("@span.test:-", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("predicate(status,Equal,string:-[quoted=False,wildcard=False])", DescribeDetailed(status));
        Assert.Equal("predicate(@span.test,Equal,string:-[quoted=False,wildcard=False])", DescribeDetailed(scoped));
    }

    [Fact]
    public void Portal_Parse_Should_GatherValuesWrittenAgainstAMinus()
    {
        // act
        var word = FilterParser.Parse("status:-abc", TelemetryFilterSignal.Traces);
        var boolean = FilterParser.Parse("status:-true", TelemetryFilterSignal.Traces);
        var dashes = FilterParser.Parse("status:--", TelemetryFilterSignal.Traces);
        var star = FilterParser.Parse("status:-*", TelemetryFilterSignal.Traces);
        var trailing = FilterParser.Parse("status:*-", TelemetryFilterSignal.Traces);
        var number = FilterParser.Parse("status:--5", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("predicate(status,Equal,string:-abc[quoted=False,wildcard=False])", DescribeDetailed(word));
        Assert.Equal("predicate(status,Equal,string:-true[quoted=False,wildcard=False])", DescribeDetailed(boolean));
        Assert.Equal("predicate(status,Equal,string:--[quoted=False,wildcard=False])", DescribeDetailed(dashes));
        Assert.Equal("predicate(status,Equal,string:-*[quoted=False,wildcard=True])", DescribeDetailed(star));
        Assert.Equal("predicate(status,Equal,string:*-[quoted=False,wildcard=True])", DescribeDetailed(trailing));
        Assert.Equal("predicate(status,Equal,string:--5[quoted=False,wildcard=False])", DescribeDetailed(number));
    }

    [Fact]
    public void Portal_Parse_Should_NotConsumeAFollowingClauseAsAMinusValue()
    {
        // act
        var result = FilterParser.Parse("status:- category:x", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal(
            "and(predicate(status,Equal,string:-[quoted=False,wildcard=False]),predicate(category,Equal,string:x[quoted=False,wildcard=False]))",
            DescribeDetailed(result));
    }

    [Fact]
    public void Portal_Parse_Should_ReportAMinusWithNoExpressionAfterIt()
    {
        // act
        var error = ParseErrorDescription("status:-- -", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("Missing expression after negation", error);
    }

    [Fact]
    public void Portal_Parse_Should_ReadBareAndNegatedStarsAsExistence()
    {
        // act
        var existence = FilterParser.Parse("status:*", TelemetryFilterSignal.Traces);
        var negated = FilterParser.Parse("-status:*", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("predicate(status,Exists,)", DescribeDetailed(existence));
        Assert.Equal("not(predicate(status,Exists,))", DescribeDetailed(negated));
    }

    [Fact]
    public void Portal_Parse_Should_ReportOrderingComparisonsAgainstNonNumbers()
    {
        // arrange
        var inputs = new[] { "test:>a", "test:>asdfasdf", "test:>-", "test:<=-x" };

        // act
        var actual = inputs.Select(input => ParseErrorDescription(input, TelemetryFilterSignal.Traces)).ToArray();

        // assert
        Assert.Equal(
            new[]
            {
                "Ordering comparisons need a number",
                "Ordering comparisons need a number",
                "Ordering comparisons need a number",
                "Ordering comparisons need a number"
            },
            actual);
    }

    [Fact]
    public void Portal_Parse_Should_AcceptSignedNumbersForOrderingComparisons()
    {
        // act
        var greater = FilterParser.Parse("test:>-5", TelemetryFilterSignal.Traces);
        var less = FilterParser.Parse("test:<=-9.5", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("predicate(test,GreaterThan,number:-5[quoted=False,wildcard=False])", DescribeDetailed(greater));
        Assert.Equal(
            "predicate(test,LessThanOrEqual,number:-9.5[quoted=False,wildcard=False])",
            DescribeDetailed(less));
    }

    [Fact]
    public void Portal_Parse_Should_RejectBooleanOrderingButAllowBooleanEqualityAndSets()
    {
        // act
        var greater = ParseErrorDescription("flag:>true", TelemetryFilterSignal.Traces);
        var less = ParseErrorDescription("flag:<=false", TelemetryFilterSignal.Traces);
        var equality = FilterParser.Parse("flag:true", TelemetryFilterSignal.Traces);
        var set = FilterParser.Parse("flag:IN(true,false)", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("Boolean values do not support ordering comparisons", greater);
        Assert.Equal("Boolean values do not support ordering comparisons", less);
        Assert.Equal("predicate(flag,Equal,boolean:true[quoted=False,wildcard=False])", DescribeDetailed(equality));
        Assert.Equal(
            "predicate(flag,In,boolean:true[quoted=False,wildcard=False],boolean:false[quoted=False,wildcard=False])",
            DescribeDetailed(set));
    }

    [Fact]
    public void Portal_Parse_Should_RejectAnInListLongerThanOneHundredValues()
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
    public void Portal_Parse_Should_RejectNonNumericRangeBounds()
    {
        // arrange
        var inputs = new[] { "http.status_code:RANGE(\"a\", 500)", "http.status_code:RANGE(low, high)" };

        // act
        var actual = inputs.Select(input => ParseErrorDescription(input, TelemetryFilterSignal.Traces)).ToArray();

        // assert
        Assert.Equal(new[] { "RANGE bounds must be numbers", "RANGE bounds must be numbers" }, actual);
    }

    [Fact]
    public void Portal_Parse_Should_ReportMissingValuesInOpenParenthesizedMatchers()
    {
        // arrange
        var inputs = new[] { "x:IN(", "x:RANGE(", "x:(" };

        // act
        var actual = inputs.Select(input => ParseErrorDescription(input, TelemetryFilterSignal.Traces)).ToArray();

        // assert
        Assert.Equal(
            new[]
            {
                "Expected a value but found 'end of input'",
                "Expected a value but found 'end of input'",
                "Expected a value but found 'end of input'"
            },
            actual);
    }

    [Fact]
    public void Portal_Parse_Should_ReportMissingValuesAfterDanglingSetSeparators()
    {
        // arrange
        var inputs = new[] { "x:IN(200,", "x:RANGE(400,", "x:(a OR" };

        // act
        var actual = inputs.Select(input => ParseErrorDescription(input, TelemetryFilterSignal.Traces)).ToArray();

        // assert
        Assert.Equal(
            new[]
            {
                "Expected a value but found 'end of input'",
                "Expected a value but found 'end of input'",
                "Expected a value but found 'end of input'"
            },
            actual);
    }

    [Fact]
    public void Portal_Parse_Should_ReportOnlyMissingParenthesisWhenValuesWereRead()
    {
        // arrange
        var inputs = new[] { "x:IN(\"a\"", "x:RANGE(400, 600", "x:(a OR b" };

        // act
        var actual = inputs.Select(input => ParseErrorDescription(input, TelemetryFilterSignal.Traces)).ToArray();

        // assert
        Assert.Equal(
            new[] { "Missing closing parenthesis", "Missing closing parenthesis", "Missing closing parenthesis" },
            actual);
    }

    [Fact]
    public void Portal_Parse_Should_KeepRangeArityErrorsForClosedAndOpenOneValueRanges()
    {
        // act
        var closed = ParseErrorDescription("x:RANGE(400)", TelemetryFilterSignal.Traces);
        var open = ParseErrorDescription("x:RANGE(400", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("RANGE requires a minimum and maximum value", closed);
        Assert.Equal("Missing closing parenthesis", open);
    }

    [Fact]
    public void Portal_Parse_Should_RejectWrongSeparatorsInEachSetForm()
    {
        // act
        var commaSet = ParseErrorDescription("http.status_code:IN(200 OR 201)", TelemetryFilterSignal.Traces);
        var orSet = ParseErrorDescription("service.name:(\"a\", \"b\")", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("Expected ',' or ')'", commaSet);
        Assert.Equal("Expected 'OR' or ')'", orSet);
    }

    [Fact]
    public void Portal_Parse_Should_WrapNegationInANode()
    {
        // act
        var result = FilterParser.Parse("-http.status_code:200", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal(
            "not(predicate(http.status_code,Equal,number:200[quoted=False,wildcard=False]))",
            DescribeDetailed(result));
    }

    [Fact]
    public void Portal_Parse_Should_AndAdjacentClausesImplicitly()
    {
        // act
        var result = FilterParser.Parse("service.name:\"a\" service.version:\"1\"", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal(
            "and(predicate(service.name,Equal,string:a[quoted=True,wildcard=False]),predicate(service.version,Equal,string:1[quoted=True,wildcard=False]))",
            DescribeDetailed(result));
    }

    [Fact]
    public void Portal_Parse_Should_TreatOnlyUppercaseLogicalKeywordsAsCombinators()
    {
        // act
        var upper = FilterParser.Parse("a:1 AND b:2 OR c:3", TelemetryFilterSignal.Traces);
        var lower = FilterParser.Parse("a:1 and b:2 or c:3", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal(
            "or(and(predicate(a,Equal,number:1[quoted=False,wildcard=False]),predicate(b,Equal,number:2[quoted=False,wildcard=False])),predicate(c,Equal,number:3[quoted=False,wildcard=False]))",
            DescribeDetailed(upper));
        Assert.Equal(
            "and(predicate(a,Equal,number:1[quoted=False,wildcard=False]),term(and),predicate(b,Equal,number:2[quoted=False,wildcard=False]),term(or),predicate(c,Equal,number:3[quoted=False,wildcard=False]))",
            DescribeDetailed(lower));
    }

    [Fact]
    public void Portal_Parse_Should_BindImplicitAndTighterThanOr()
    {
        // act
        var result = FilterParser.Parse("a b OR c", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("or(and(term(a),term(b)),term(c))", DescribeDetailed(result));
    }

    [Fact]
    public void Portal_Parse_Should_PreserveBareFreeTextTerms()
    {
        // act
        var result = FilterParser.Parse("ValidateWatermark", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("term(ValidateWatermark)", DescribeDetailed(result));
    }

    [Fact]
    public void Portal_Parse_Should_AcceptUnquotedValuesContainingDashes()
    {
        // act
        var result = FilterParser.Parse("service:web-store", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal(
            "predicate(service,Equal,string:web-store[quoted=False,wildcard=False])",
            DescribeDetailed(result));
    }

    [Fact]
    public void Portal_Parse_Should_ReportMissingClauseValues()
    {
        // act
        var error = ParseErrorDescription("service.name:", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal("Missing value in key:value pair", error);
    }

    [Fact]
    public void Portal_Parse_Should_RejectScopePrefixesWithoutAttributeNames()
    {
        // arrange
        var inputs = new[] { "@span:x", "@resource.:x" };

        // act
        var actual = inputs.Select(input => ParseErrorDescription(input, TelemetryFilterSignal.Traces)).ToArray();

        // assert
        Assert.Equal(
            new[]
            {
                "Expected an attribute name after the @span prefix",
                "Expected an attribute name after the @resource prefix"
            },
            actual);
    }

    [Fact]
    public void Portal_Parse_Should_GuideRecognizableScopeTermsIntoFilterExpressions()
    {
        // arrange
        var inputs = new[] { "@", "@span", "@span.", "@span.test", "@resou" };

        // act
        var actual = inputs.Select(input => ParseErrorDescription(input, TelemetryFilterSignal.Traces)).ToArray();

        // assert
        Assert.Equal(
            new[]
            {
                "Scope prefixes can only be used in filter expressions",
                "Scope prefixes can only be used in filter expressions",
                "Scope prefixes can only be used in filter expressions",
                "Scope prefixes can only be used in filter expressions",
                "Scope prefixes can only be used in filter expressions"
            },
            actual);
    }

    [Fact]
    public void Portal_Parse_Should_RejectUnknownScopeTermsAndKeys()
    {
        // arrange
        var terms = new[] { "@spanx", "@foo.bar", "@resourcex" };
        var keys = new[] { "@foo.bar:1", "@spanx.name:1", "@resourcex.a:1" };

        // act
        var termErrors = terms.Select(input => ParseErrorDescription(input, TelemetryFilterSignal.Traces)).ToArray();
        var keyErrors = keys.Select(input => ParseErrorDescription(input, TelemetryFilterSignal.Traces)).ToArray();

        // assert
        Assert.Equal(new[] { "Unknown scope prefix", "Unknown scope prefix", "Unknown scope prefix" }, termErrors);
        Assert.Equal(new[] { "Unknown scope prefix", "Unknown scope prefix", "Unknown scope prefix" }, keyErrors);
    }

    [Fact]
    public void Portal_Parse_Should_TreatAtSignsInValuePositionsAsText()
    {
        // act
        var results = new[]
        {
            FilterParser.Parse("service.name:@gateway", TelemetryFilterSignal.Traces),
            FilterParser.Parse("status:@span", TelemetryFilterSignal.Traces),
            FilterParser.Parse("status:@span.name", TelemetryFilterSignal.Traces),
            FilterParser.Parse("@span.name:@log", TelemetryFilterSignal.Traces),
            FilterParser.Parse("s:IN(@span)", TelemetryFilterSignal.Traces)
        };

        // assert
        Assert.Equal(
            new[]
            {
                "predicate(service.name,Equal,string:@gateway[quoted=False,wildcard=False])",
                "predicate(status,Equal,string:@span[quoted=False,wildcard=False])",
                "predicate(status,Equal,string:@span.name[quoted=False,wildcard=False])",
                "predicate(@span.name,Equal,string:@log[quoted=False,wildcard=False])",
                "predicate(s,In,string:@span[quoted=False,wildcard=False])"
            },
            results.Select(DescribeDetailed));
    }

    [Fact]
    public void Portal_Parse_Should_TreatEscapedAndQuotedAtSignsAsText()
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
    public void Portal_Parse_Should_AcceptSignalSpecificScopedKeys()
    {
        // act
        var traces = new[]
        {
            FilterParser.Parse("@span.name:GET", TelemetryFilterSignal.Traces),
            FilterParser.Parse("@resource.service.name:x", TelemetryFilterSignal.Traces),
            FilterParser.Parse("@span.test:1", TelemetryFilterSignal.Traces),
            FilterParser.Parse("@event.exception.type:TimeoutError", TelemetryFilterSignal.Traces)
        };
        var logs = new[]
        {
            FilterParser.Parse("@log.message:x", TelemetryFilterSignal.Logs),
            FilterParser.Parse("@resource.service.name:1", TelemetryFilterSignal.Logs),
            FilterParser.Parse("@body.message:x", TelemetryFilterSignal.Logs)
        };

        // assert
        Assert.Equal(
            new[]
            {
                "predicate(@span.name,Equal,string:GET[quoted=False,wildcard=False])",
                "predicate(@resource.service.name,Equal,string:x[quoted=False,wildcard=False])",
                "predicate(@span.test,Equal,number:1[quoted=False,wildcard=False])",
                "predicate(@event.exception.type,Equal,string:TimeoutError[quoted=False,wildcard=False])"
            },
            traces.Select(DescribeDetailed));
        Assert.Equal(
            new[]
            {
                "predicate(@log.message,Equal,string:x[quoted=False,wildcard=False])",
                "predicate(@resource.service.name,Equal,number:1[quoted=False,wildcard=False])",
                "predicate(@body.message,Equal,string:x[quoted=False,wildcard=False])"
            },
            logs.Select(DescribeDetailed));
    }

    [Fact]
    public void Portal_Parse_Should_RejectScopedKeysNotAllowedForLogs()
    {
        // act
        var span = ParseErrorDescription("@span.name:x", TelemetryFilterSignal.Logs);
        var bareSpan = ParseErrorDescription("@span:x", TelemetryFilterSignal.Logs);
        var partial = ParseErrorDescription("@spa", TelemetryFilterSignal.Logs);

        // assert
        Assert.Equal("Unknown scope prefix", span);
        Assert.Equal("Unknown scope prefix", bareSpan);
        Assert.Equal("Unknown scope prefix", partial);
    }

    [Fact]
    public void Portal_Parse_Should_GuideValidLogScopeTerms()
    {
        // arrange
        var inputs = new[] { "@", "@lo", "@res", "@log.x" };

        // act
        var actual = inputs.Select(input => ParseErrorDescription(input, TelemetryFilterSignal.Logs)).ToArray();

        // assert
        Assert.Equal(
            new[]
            {
                "Scope prefixes can only be used in filter expressions",
                "Scope prefixes can only be used in filter expressions",
                "Scope prefixes can only be used in filter expressions",
                "Scope prefixes can only be used in filter expressions"
            },
            actual);
    }

    [Fact]
    public void Portal_Parse_Should_AcceptAndValidateLogScopeKeys()
    {
        // act
        var log = FilterParser.Parse("@log.message:x", TelemetryFilterSignal.Logs);
        var resource = FilterParser.Parse("@resource.service.name:1", TelemetryFilterSignal.Logs);
        var missing = ParseErrorDescription("@log:info", TelemetryFilterSignal.Logs);

        // assert
        Assert.Equal("predicate(@log.message,Equal,string:x[quoted=False,wildcard=False])", DescribeDetailed(log));
        Assert.Equal(
            "predicate(@resource.service.name,Equal,number:1[quoted=False,wildcard=False])",
            DescribeDetailed(resource));
        Assert.Equal("Expected an attribute name after the @log prefix", missing);
    }

    [Fact]
    public void Portal_Parse_Should_RejectAdjacentTermsAfterGroupedPrimaries()
    {
        // arrange
        var cases = new[]
        {
            "(status:error)env:prod",
            "((status:error))env:prod",
            "(status:error OR status:ok)env:prod",
            "(timeout)failure",
            "(timeout OR failure)service.name:checkout"
        };

        // act
        var actual = cases.Select(input => ParseErrorDescription(input, TelemetryFilterSignal.Traces)).ToArray();

        // assert
        Assert.Equal(
            new[]
            {
                "Expected whitespace but \"env\" found",
                "Expected whitespace but \"env\" found",
                "Expected whitespace but \"env\" found",
                "Expected whitespace but \"failure\" found",
                "Expected whitespace but \"service.name\" found"
            },
            actual);
    }

    [Fact]
    public void Portal_Parse_Should_SignALoneMinusInsideASet()
    {
        // act
        var result = FilterParser.Parse("status:IN(-, a)", TelemetryFilterSignal.Traces);

        // assert
        Assert.Equal(
            "predicate(status,In,string:-[quoted=False,wildcard=False],string:a[quoted=False,wildcard=False])",
            DescribeDetailed(result));
    }

    [Fact]
    public void Portal_Parse_Should_RejectEveryInvalidRangeArity()
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

    [Fact]
    public void Portal_Parse_Should_ValidateTheLogScopePrefixWhenTheAttributeNameIsMissing()
    {
        // act
        var error = ParseErrorDescription("@log:info", TelemetryFilterSignal.Logs);

        // assert
        Assert.Equal("Expected an attribute name after the @log prefix", error);
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
