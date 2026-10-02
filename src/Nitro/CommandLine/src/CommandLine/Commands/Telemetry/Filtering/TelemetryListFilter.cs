using System.Collections.Frozen;
using System.Collections.Immutable;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal static class TelemetryListFilter
{
    private static readonly ImmutableArray<string> s_severityLevels =
    [
        "trace",
        "debug",
        "info",
        "warn",
        "error",
        "fatal"
    ];

    private static readonly FrozenDictionary<string, int> s_severityRanks = s_severityLevels
        .Select(static (level, index) => new KeyValuePair<string, int>(level, index))
        .ToFrozenDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    public static bool TryCompile(
        INitroConsole console,
        TelemetryFilterSignal signal,
        string? filterText,
        bool hasError,
        int? minDurationMs,
        string? severity,
        string? traceId,
        string? search,
        string? service,
        out OpenTelemetryFilterInput? filter,
        out FilterNode? parsedFilter)
    {
        try
        {
            filter = Compile(
                filterText,
                signal,
                hasError,
                minDurationMs,
                severity,
                traceId,
                search,
                service,
                out parsedFilter);
            return true;
        }
        catch (FilterParseException exception)
        {
            RenderParseError(console, signal, filterText!, exception);
            filter = null;
            parsedFilter = null;
            return false;
        }
    }

    public static OpenTelemetryFilterInput? Compile(
        string? filterText,
        TelemetryFilterSignal signal,
        bool hasError,
        int? minDurationMs,
        string? severity,
        string? traceId,
        string? search,
        string? service)
        => Compile(filterText, signal, hasError, minDurationMs, severity, traceId, search, service, out _);

    public static OpenTelemetryFilterInput? Compile(
        string? filterText,
        TelemetryFilterSignal signal,
        bool hasError,
        int? minDurationMs,
        string? severity,
        string? traceId,
        string? search,
        string? service,
        out FilterNode? parsedFilter)
    {
        var freeTextKey = signal == TelemetryFilterSignal.Traces ? "span.name" : "log.message";
        var clauses = new List<OpenTelemetryFilterInput>();
        parsedFilter = string.IsNullOrWhiteSpace(filterText) ? null : FilterParser.Parse(filterText, signal);
        var compiledFilter = FilterCompiler.Compile(parsedFilter, freeTextKey);
        if (compiledFilter is not null)
        {
            clauses.Add(compiledFilter);
        }

        if (hasError)
        {
            clauses.Add(
                FilterCompiler.CreateAttributeFilter(
                    "status",
                    new OpenTelemetryAttributeConditionInput
                    {
                        Eq = new OpenTelemetryAttributeValueInput { String = "error" }
                    }));
        }

        if (minDurationMs is not null)
        {
            clauses.Add(
                FilterCompiler.CreateAttributeFilter(
                    "duration",
                    new OpenTelemetryAttributeConditionInput
                    {
                        Gte = new OpenTelemetryAttributeValueInput { Int = minDurationMs }
                    }));
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            clauses.Add(CreateSeverityFilter(severity));
        }

        if (!string.IsNullOrWhiteSpace(traceId))
        {
            clauses.Add(
                FilterCompiler.CreateAttributeFilter(
                    "trace.id",
                    new OpenTelemetryAttributeConditionInput
                    {
                        Eq = new OpenTelemetryAttributeValueInput { String = traceId }
                    }));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            clauses.Add(FilterCompiler.CreateFreeTextFilter(search, freeTextKey));
        }

        if (!string.IsNullOrWhiteSpace(service))
        {
            clauses.Add(
                FilterCompiler.CreateAttributeFilter(
                    "@resource.service.name",
                    new OpenTelemetryAttributeConditionInput
                    {
                        Eq = new OpenTelemetryAttributeValueInput { String = service }
                    }));
        }

        return clauses.Count switch
        {
            0 => null,
            1 => clauses[0],
            _ => new OpenTelemetryFilterInput { And = clauses }
        };
    }

    public static async Task<string?> CreateEmptyResultHintAsync(
        ITelemetryClient client,
        string workspaceId,
        TelemetryFilterSignal signal,
        int itemCount,
        string? filterText,
        string? search,
        FilterNode? parsedFilter,
        DateTimeOffset? since,
        DateTimeOffset? until,
        CancellationToken cancellationToken)
    {
        if (itemCount != 0
            || (string.IsNullOrWhiteSpace(filterText) && string.IsNullOrWhiteSpace(search)))
        {
            return null;
        }

        var signalKind =
            signal == TelemetryFilterSignal.Traces ? OpenTelemetrySignalKind.Traces : OpenTelemetrySignalKind.Logs;

        try
        {
            var attributeKeys = await client.ListAttributeKeysAsync(
                workspaceId,
                signalKind,
                kinds: null,
                search: null,
                since,
                until,
                first: 50,
                after: null,
                cancellationToken);
            return KeySuggestions.CreateHint(parsedFilter, attributeKeys.Items, signalKind);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static OpenTelemetryFilterInput CreateSeverityFilter(string severity)
    {
        if (!s_severityRanks.TryGetValue(severity, out var start))
        {
            throw ThrowHelper.UnsupportedSeverityLevel(severity);
        }

        return FilterCompiler.CreateAttributeFilter(
            "severity",
            new OpenTelemetryAttributeConditionInput
            {
                In = s_severityLevels
                    .Skip(start)
                    .Select(level => new OpenTelemetryAttributeValueInput { String = level })
                    .ToArray()
            });
    }

    private static void RenderParseError(
        INitroConsole console,
        TelemetryFilterSignal signal,
        string filterText,
        FilterParseException exception)
    {
        var examples =
            signal == TelemetryFilterSignal.Traces
                ? "hint: examples: `status:error`, `duration:>=100`, or `@resource.service.name:checkout`"
                : "hint: examples: `severity:error`, `@resource.service.name:checkout`, "
                    + "or `exception.type:TimeoutException`";

        console.Error.Write(new Text($"filter: {exception.Message} at column {exception.Column}"));
        console.Error.WriteLine();
        console.Error.Write(new Text(filterText));
        console.Error.WriteLine();
        console.Error.Write(new Text($"{new string(' ', exception.Column - 1)}^"));
        console.Error.WriteLine();
        console.Error.Write(new Text(examples));
        console.Error.WriteLine();
    }
}
