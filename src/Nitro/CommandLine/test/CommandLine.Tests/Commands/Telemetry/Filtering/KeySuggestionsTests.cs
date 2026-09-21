using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Filtering;

public sealed class KeySuggestionsTests
{
    [Fact]
    public void CreateHint_Should_NormalizeScopesAndOrderCandidates_When_FilterHasAnUnknownAttribute()
    {
        // arrange
        var filter = FilterParser.Parse(
            "@resource.http.statuscode:>=500 AND status:error",
            TelemetryFilterSignal.Traces);
        AttributeKeyRow[] keys =
        [
            new("Resource", "http.response.status_code"),
            new("Resource", "http.status_code")
        ];

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
        AttributeKeyRow[] keys =
        [
            new("Span", "client.address"),
            new("Span", "server.address")
        ];

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
