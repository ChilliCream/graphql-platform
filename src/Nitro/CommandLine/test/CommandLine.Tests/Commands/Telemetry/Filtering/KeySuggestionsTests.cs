using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Filtering;

public sealed class KeySuggestionsTests
{
    [Fact]
    public void CreateHint_Should_RankLeafPrefixMatches_When_TheRootMatchesButThePathDoesNot()
    {
        // arrange
        var filter = FilterParser.Parse("http.stat:1", TelemetryFilterSignal.Traces);
        AttributeKeyRow[] keys =
        [
            new("Span", "http.response.status_code"),
            new("Span", "http.request.status"),
            new("Span", "http.x.y.status"),
            new("Span", "net.peer.name")
        ];

        // act
        var result = KeySuggestions.CreateHint(filter, keys, OpenTelemetrySignalKind.Traces);

        // assert
        result.MatchInlineSnapshot(
            """
            no results; unknown key 'http.stat', did you mean http.request.status, http.x.y.status, http.response.status_code? Run nitro telemetry attributes keys --signal traces to list keys.
            """);
    }

    [Fact]
    public void CreateHint_Should_SuggestByEditDistance_When_TheUnknownKeyIsLongerThanTheCandidate()
    {
        // arrange
        var filter = FilterParser.Parse("service.namee:x", TelemetryFilterSignal.Traces);
        AttributeKeyRow[] keys = [new("Span", "service.name"), new("Span", "host")];

        // act
        var result = KeySuggestions.CreateHint(filter, keys, OpenTelemetrySignalKind.Traces);

        // assert
        result.MatchInlineSnapshot(
            """
            no results; unknown key 'service.namee', did you mean service.name? Run nitro telemetry attributes keys --signal traces to list keys.
            """);
    }

    [Fact]
    public void CreateHint_Should_SuggestUndottedKeys_When_NeitherSideHasASegment()
    {
        // arrange
        var filter = FilterParser.Parse("enviroment:prod", TelemetryFilterSignal.Traces);
        AttributeKeyRow[] keys = [new("Span", "environment"), new("Span", "env")];

        // act
        var result = KeySuggestions.CreateHint(filter, keys, OpenTelemetrySignalKind.Traces);

        // assert
        result.MatchInlineSnapshot(
            """
            no results; unknown key 'enviroment', did you mean environment? Run nitro telemetry attributes keys --signal traces to list keys.
            """);
    }

    [Fact]
    public void CreateHint_Should_ReturnNull_When_TheFilterIsNull()
    {
        // act
        var result = KeySuggestions.CreateHint(
            null,
            [new AttributeKeyRow("Span", "http.status_code")],
            OpenTelemetrySignalKind.Traces);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void CreateHint_Should_ReturnNull_When_ThereAreNoKnownKeys()
    {
        // arrange
        var filter = FilterParser.Parse("http.statuscode:1", TelemetryFilterSignal.Traces);

        // act
        var result = KeySuggestions.CreateHint(filter, [], OpenTelemetrySignalKind.Traces);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void CreateHint_Should_IgnoreVirtualFieldsAndTerms_When_BuildingSuggestions()
    {
        // arrange
        var filter = FilterParser.Parse(
            "status:error severity:warn duration:>5 span.name:x log.message:y trace.id:abc span.id:1 span.kind:server timeout",
            TelemetryFilterSignal.Traces);
        AttributeKeyRow[] keys = [new("Span", "http.status_code")];

        // act
        var result = KeySuggestions.CreateHint(filter, keys, OpenTelemetrySignalKind.Traces);

        // assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(
        "@span.http.statuscode:1",
        "Span",
        TelemetryFilterSignal.Traces,
        OpenTelemetrySignalKind.Traces,
        "no results; unknown key 'http.statuscode', did you mean http.status_code? Run nitro telemetry attributes keys --signal traces to list keys.")]
    [InlineData(
        "@event.http.statuscode:1",
        "Event",
        TelemetryFilterSignal.Traces,
        OpenTelemetrySignalKind.Traces,
        "no results; unknown key 'http.statuscode', did you mean http.status_code? Run nitro telemetry attributes keys --signal traces to list keys.")]
    [InlineData(
        "@resource.http.statuscode:1",
        "Resource",
        TelemetryFilterSignal.Traces,
        OpenTelemetrySignalKind.Traces,
        "no results; unknown key 'http.statuscode', did you mean http.status_code? Run nitro telemetry attributes keys --signal traces to list keys.")]
    [InlineData(
        "@log.http.statuscode:1",
        "Log",
        TelemetryFilterSignal.Logs,
        OpenTelemetrySignalKind.Logs,
        "no results; unknown key 'http.statuscode', did you mean http.status_code? Run nitro telemetry attributes keys --signal logs to list keys.")]
    [InlineData(
        "@body.http.statuscode:1",
        "Body",
        TelemetryFilterSignal.Logs,
        OpenTelemetrySignalKind.Logs,
        "no results; unknown key 'http.statuscode', did you mean http.status_code? Run nitro telemetry attributes keys --signal logs to list keys.")]
    public void CreateHint_Should_StripEveryScopePrefix_When_FieldsAreScoped(
        string text,
        string kind,
        TelemetryFilterSignal filterSignal,
        OpenTelemetrySignalKind signal,
        string expected)
    {
        // arrange
        var filter = FilterParser.Parse(text, filterSignal);
        AttributeKeyRow[] keys = [new(kind, "http.status_code")];

        // act
        var result = KeySuggestions.CreateHint(filter, keys, signal);

        // assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CreateHint_Should_DeduplicateUnknownKeys_When_TheSameKeyAppearsTwice()
    {
        // arrange
        var filter = FilterParser.Parse("http.statuscode:1 OR -http.statuscode:2", TelemetryFilterSignal.Traces);
        AttributeKeyRow[] keys = [new("Span", "http.status_code")];

        // act
        var result = KeySuggestions.CreateHint(filter, keys, OpenTelemetrySignalKind.Traces);

        // assert
        result.MatchInlineSnapshot(
            """
            no results; unknown key 'http.statuscode', did you mean http.status_code? Run nitro telemetry attributes keys --signal traces to list keys.
            """);
    }

    [Fact]
    public void CreateHint_Should_ReturnNull_When_TheUnknownKeyIsKnownInAnotherCase()
    {
        // arrange
        var filter = FilterParser.Parse("HTTP.STATUS_CODE:1", TelemetryFilterSignal.Traces);
        AttributeKeyRow[] keys = [new("Span", "http.status_code")];

        // act
        var result = KeySuggestions.CreateHint(filter, keys, OpenTelemetrySignalKind.Traces);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void CreateHint_Should_NormalizeScopesAndOrderCandidates_When_FilterHasAnUnknownAttribute()
    {
        // arrange
        var filter = FilterParser.Parse(
            "@resource.http.statuscode:>=500 AND status:error",
            TelemetryFilterSignal.Traces);
        AttributeKeyRow[] keys = [new("Resource", "http.response.status_code"), new("Resource", "http.status_code")];

        // act
        var result = KeySuggestions.CreateHint(filter, keys, OpenTelemetrySignalKind.Traces);

        // assert
        result.MatchInlineSnapshot(
            """
            no results; unknown key 'http.statuscode', did you mean http.status_code, http.response.status_code? Run nitro telemetry attributes keys --signal traces to list keys.
            """);
    }

    [Fact]
    public void CreateHint_Should_TraversePredicatesInSourceOrder_When_FilterHasMultipleUnknownAttributes()
    {
        // arrange
        var filter = FilterParser.Parse(
            "client.addres:127.0.0.1 OR -server.addres:127.0.0.1",
            TelemetryFilterSignal.Traces);
        AttributeKeyRow[] keys = [new("Span", "client.address"), new("Span", "server.address")];

        // act
        var result = KeySuggestions.CreateHint(filter, keys, OpenTelemetrySignalKind.Traces);

        // assert
        result.MatchInlineSnapshot(
            """
            no results; unknown key 'client.addres', did you mean client.address? unknown key 'server.addres', did you mean server.address? Run nitro telemetry attributes keys --signal traces to list keys.
            """);
    }

    [Fact]
    public void CreateHint_Should_UsePrefixOrderingBeforeEditDistance_When_CandidatesQualifyAtDifferentTiers()
    {
        // arrange
        var filter = FilterParser.Parse("service.ver:1", TelemetryFilterSignal.Traces);
        AttributeKeyRow[] keys =
        [
            new("Resource", "service.version.name"),
            new("Resource", "service.verbatim"),
            new("Resource", "service.version"),
            new("Resource", "service.vor")
        ];

        // act
        var result = KeySuggestions.CreateHint(filter, keys, OpenTelemetrySignalKind.Traces);

        // assert
        result.MatchInlineSnapshot(
            """
            no results; unknown key 'service.ver', did you mean service.version, service.verbatim, service.version.name, service.vor? Run nitro telemetry attributes keys --signal traces to list keys.
            """);
    }

    [Fact]
    public void CreateHint_Should_ReturnNull_When_FieldsAreKnownOrHaveNoQualifyingCandidates()
    {
        // arrange
        var knownFilter = FilterParser.Parse("HTTP.STATUS_CODE:500", TelemetryFilterSignal.Traces);
        var unknownFilter = FilterParser.Parse("unrelated.key:value", TelemetryFilterSignal.Traces);
        AttributeKeyRow[] keys = [new("Span", "http.status_code")];

        // act
        var knownResult = KeySuggestions.CreateHint(knownFilter, keys, OpenTelemetrySignalKind.Traces);
        var unknownResult = KeySuggestions.CreateHint(unknownFilter, keys, OpenTelemetrySignalKind.Traces);

        // assert
        Assert.Null(knownResult);
        Assert.Null(unknownResult);
    }
}
