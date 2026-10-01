using System.Collections.Frozen;
using System.Collections.Immutable;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal static class FilterFlags
{
    public static OpenTelemetryFilterInput? Compile(
        string? filter,
        TelemetryFilterSignal signal,
        bool hasError,
        int? minDurationMs,
        string? severity,
        string? traceId,
        string? search,
        string? service)
        => Compile(
            filter,
            signal,
            hasError,
            minDurationMs,
            severity,
            traceId,
            search,
            service,
            out _);

    public static OpenTelemetryFilterInput? Compile(
        string? filter,
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
        parsedFilter = string.IsNullOrWhiteSpace(filter) ? null : FilterParser.Parse(filter, signal);
        var parsed = FilterCompiler.Compile(parsedFilter, freeTextKey);
        if (parsed is not null)
        {
            clauses.Add(parsed);
        }

        if (hasError)
        {
            clauses.Add(OpenTelemetryFilterInput.FromAttribute("status", new OpenTelemetryAttributeConditionInput
            {
                Eq = new OpenTelemetryAttributeValueInput { String = "error" }
            }));
        }

        if (minDurationMs is not null)
        {
            clauses.Add(OpenTelemetryFilterInput.FromAttribute("duration", new OpenTelemetryAttributeConditionInput
            {
                Gte = new OpenTelemetryAttributeValueInput { Int = minDurationMs }
            }));
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            clauses.Add(severity.ToSeverityFilter());
        }

        if (!string.IsNullOrWhiteSpace(traceId))
        {
            clauses.Add(OpenTelemetryFilterInput.FromAttribute("trace.id", new OpenTelemetryAttributeConditionInput
            {
                Eq = new OpenTelemetryAttributeValueInput { String = traceId }
            }));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            clauses.Add(FilterCompiler.FreeText(search, freeTextKey));
        }

        if (!string.IsNullOrWhiteSpace(service))
        {
            clauses.Add(OpenTelemetryFilterInput.FromAttribute("@resource.service.name", new OpenTelemetryAttributeConditionInput
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
}

file static class Extensions
{
    private static readonly ImmutableArray<string> s_severityLevels = ["trace", "debug", "info", "warn", "error", "fatal"];

    private static readonly FrozenDictionary<string, int> s_severityRanks = s_severityLevels
        .Select(static (level, index) => new KeyValuePair<string, int>(level, index))
        .ToFrozenDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    extension(OpenTelemetryFilterInput)
    {
        public static OpenTelemetryFilterInput FromAttribute(
            string field,
            OpenTelemetryAttributeConditionInput condition)
        {
            var (key, kind) = field.StartsWith("@resource.", StringComparison.Ordinal)
                ? (field[10..], (OpenTelemetryAttributeKind?)OpenTelemetryAttributeKind.Resource)
                : (field, null);
            var predicate = new OpenTelemetryAttributePredicateInput
            {
                Key = key,
                Condition = condition
            };
            if (kind is not null)
            {
                predicate = predicate with { Kind = kind };
            }

            return new OpenTelemetryFilterInput
            {
                Attribute = predicate
            };
        }
    }

    extension(string severity)
    {
        public OpenTelemetryFilterInput ToSeverityFilter()
        {
            if (!s_severityRanks.TryGetValue(severity, out var start))
            {
                throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unsupported severity level.");
            }

            return OpenTelemetryFilterInput.FromAttribute("severity", new OpenTelemetryAttributeConditionInput
            {
                In = s_severityLevels.Skip(start)
                    .Select(level => new OpenTelemetryAttributeValueInput { String = level })
                    .ToArray()
            });
        }
    }
}
