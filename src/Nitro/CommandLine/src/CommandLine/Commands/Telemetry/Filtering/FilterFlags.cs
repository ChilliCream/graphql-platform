using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal static class FilterFlags
{
    private static readonly string[] s_severityLevels = ["trace", "debug", "info", "warn", "error", "fatal"];

    public static OpenTelemetryFilterInput? Compile(
        string? filter,
        TelemetryFilterSignal signal,
        bool hasError,
        int? minDurationMs,
        string? severity,
        string? traceId,
        string? search,
        string? service)
    {
        var freeTextKey = signal == TelemetryFilterSignal.Traces ? "span.name" : "log.message";
        var clauses = new List<OpenTelemetryFilterInput>();
        var parsed = FilterCompiler.Compile(filter, freeTextKey, signal);
        if (parsed is not null)
        {
            clauses.Add(parsed);
        }

        if (hasError)
        {
            clauses.Add(Attribute("status", new OpenTelemetryAttributeConditionInput
            {
                Eq = new OpenTelemetryAttributeValueInput { String = "error" }
            }));
        }

        if (minDurationMs is not null)
        {
            clauses.Add(Attribute("duration", new OpenTelemetryAttributeConditionInput
            {
                Gte = new OpenTelemetryAttributeValueInput { Int = minDurationMs }
            }));
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            clauses.Add(Severity(severity));
        }

        if (!string.IsNullOrWhiteSpace(traceId))
        {
            clauses.Add(Attribute("trace.id", new OpenTelemetryAttributeConditionInput
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
            clauses.Add(Attribute("@resource.service.name", new OpenTelemetryAttributeConditionInput
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

    private static OpenTelemetryFilterInput Severity(string severity)
    {
        var start = Array.FindIndex(
            s_severityLevels,
            level => string.Equals(level, severity, StringComparison.OrdinalIgnoreCase));
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unsupported severity level.");
        }

        return Attribute("severity", new OpenTelemetryAttributeConditionInput
        {
            In = s_severityLevels[start..]
                .Select(level => new OpenTelemetryAttributeValueInput { String = level })
                .ToArray()
        });
    }

    private static OpenTelemetryFilterInput Attribute(
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
