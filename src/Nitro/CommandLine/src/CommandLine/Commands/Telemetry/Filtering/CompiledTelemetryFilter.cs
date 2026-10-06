using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal sealed class CompiledTelemetryFilter
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

    private CompiledTelemetryFilter(OpenTelemetryFilterInput? input)
    {
        Input = input;
    }

    /// <summary>
    /// The server filter combining the <c>--filter</c> expression and the shortcut options,
    /// or <c>null</c> when nothing restricts the results.
    /// </summary>
    public OpenTelemetryFilterInput? Input { get; }

    public static CompiledTelemetryFilter Create(TelemetryFilterSignal signal, string? filterText)
        => Create(
            signal,
            filterText,
            hasError: false,
            minDurationMs: null,
            severity: null,
            traceId: null,
            search: null,
            service: null);

    public static CompiledTelemetryFilter Create(
        string? filterText,
        bool hasError,
        int? minDurationMs,
        string? search,
        string? service)
        => Create(
            TelemetryFilterSignal.Traces,
            filterText,
            hasError,
            minDurationMs,
            severity: null,
            traceId: null,
            search,
            service);

    public static CompiledTelemetryFilter Create(
        string? filterText,
        string? severity,
        string? traceId,
        string? search,
        string? service)
        => Create(
            TelemetryFilterSignal.Logs,
            filterText,
            hasError: false,
            minDurationMs: null,
            severity,
            traceId,
            search,
            service);

    public static bool TryCreate(
        INitroConsole console,
        TelemetryFilterSignal signal,
        string? filterText,
        [NotNullWhen(true)] out CompiledTelemetryFilter? filter)
        => TryCreate(
            console,
            signal,
            filterText,
            hasError: false,
            minDurationMs: null,
            severity: null,
            traceId: null,
            search: null,
            service: null,
            out filter);

    public static bool TryCreate(
        INitroConsole console,
        string? filterText,
        bool hasError,
        int? minDurationMs,
        string? search,
        string? service,
        [NotNullWhen(true)] out CompiledTelemetryFilter? filter)
        => TryCreate(
            console,
            TelemetryFilterSignal.Traces,
            filterText,
            hasError,
            minDurationMs,
            severity: null,
            traceId: null,
            search,
            service,
            out filter);

    public static bool TryCreate(
        INitroConsole console,
        string? filterText,
        string? severity,
        string? traceId,
        string? search,
        string? service,
        [NotNullWhen(true)] out CompiledTelemetryFilter? filter)
        => TryCreate(
            console,
            TelemetryFilterSignal.Logs,
            filterText,
            hasError: false,
            minDurationMs: null,
            severity,
            traceId,
            search,
            service,
            out filter);

    private static bool TryCreate(
        INitroConsole console,
        TelemetryFilterSignal signal,
        string? filterText,
        bool hasError,
        int? minDurationMs,
        string? severity,
        string? traceId,
        string? search,
        string? service,
        [NotNullWhen(true)] out CompiledTelemetryFilter? filter)
    {
        try
        {
            filter = Create(signal, filterText, hasError, minDurationMs, severity, traceId, search, service);
            return true;
        }
        catch (FilterParseException exception)
        {
            RenderParseError(console, filterText!, exception);
            filter = null;
            return false;
        }
    }

    private static CompiledTelemetryFilter Create(
        TelemetryFilterSignal signal,
        string? filterText,
        bool hasError,
        int? minDurationMs,
        string? severity,
        string? traceId,
        string? search,
        string? service)
    {
        var freeTextKey = signal == TelemetryFilterSignal.Traces ? "span.name" : "log.message";
        var clauses = new List<OpenTelemetryFilterInput>();
        var parsedFilter = string.IsNullOrWhiteSpace(filterText) ? null : FilterParser.Parse(filterText, signal);
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

        var input = clauses.Count switch
        {
            0 => null,
            1 => clauses[0],
            _ => new OpenTelemetryFilterInput { And = clauses }
        };

        return new CompiledTelemetryFilter(input);
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
        string filterText,
        FilterParseException exception)
    {
        console.Error.Write(new Text($"filter: {exception.Message} at column {exception.Column}"));
        console.Error.WriteLine();
        console.Error.Write(new Text(filterText));
        console.Error.WriteLine();
        console.Error.Write(new Text($"{new string(' ', exception.Column - 1)}^"));
        console.Error.WriteLine();
    }
}
