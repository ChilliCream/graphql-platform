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
    [InlineData("service.name:IN(1, 2, 3)")]
    [InlineData("duration:-5")]
    [InlineData("duration:>=-5.5")]
    [InlineData("status:--")]
    [InlineData("status:*-test")]
    [InlineData("service.name:\"api gateway\"")]
    [InlineData("error:false")]
    [InlineData("a:1 AND b:2 OR c:3")]
    [InlineData("a:1 and b:2 or c:3")]
    [InlineData("   a:1     AND    b:2   ")]
    [InlineData("@resource.service.name:api")]
    [InlineData("@event.exception.type:TimeoutError")]
    public void Parse_Should_AcceptPortalGrammarForms_When_TheyAreWellFormed(string filter)
    {
        // act
        var result = FilterParser.Parse(filter, TelemetryFilterSignal.Traces);

        // assert
        Assert.IsNotType<FilterTermNode>(result);
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
}
