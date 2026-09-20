using System.Globalization;
using ChilliCream.Nitro.Client.Telemetry.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces.Tree;

internal sealed class SpanTreeRenderer
{
    private const int MaximumValueLength = 120;

    public string Render(SpanSelectionResult selection)
    {
        if (selection.Spans.Count == 0)
        {
            return string.Empty;
        }

        var selected = selection.Spans.ToDictionary(static item => item.Node);
        var roots = selection.Spans
            .Where(item => item.Node.Parent is not { } parent || !selected.ContainsKey(parent))
            .OrderBy(static item => item.SelectionIndex)
            .ToArray();
        var lines = new List<string>();

        for (var i = 0; i < roots.Length; i++)
        {
            RenderNode(
                roots[i],
                selected,
                parentPrefix: string.Empty,
                isRoot: roots.Length == 1,
                isLast: i == roots.Length - 1,
                lines);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static void RenderNode(
        SpanSelectionEntry entry,
        IReadOnlyDictionary<SpanTreeNode, SpanSelectionEntry> selected,
        string parentPrefix,
        bool isRoot,
        bool isLast,
        List<string> lines)
    {
        var linePrefix = isRoot
            ? string.Empty
            : parentPrefix + (isLast ? "└─ " : "├─ ");
        lines.Add(linePrefix + FormatSpan(entry.Node.Span));

        var exceptionPrefix = isRoot
            ? "  "
            : parentPrefix + (isLast ? "   " : "│  ");
        RenderExceptions(entry.Node.Span, exceptionPrefix, lines);

        var childPrefix = isRoot
            ? string.Empty
            : parentPrefix + (isLast ? "   " : "│  ");
        var children = entry.Node.Children
            .Where(selected.ContainsKey)
            .Select(child => selected[child])
            .OrderBy(static child => child.SelectionIndex)
            .ToArray();

        for (var i = 0; i < children.Length; i++)
        {
            RenderNode(
                children[i],
                selected,
                childPrefix,
                isRoot: false,
                isLast: i == children.Length - 1,
                lines);
        }
    }

    private static string FormatSpan(TraceSpan span)
    {
        var operation = GetOperationLabel(span);
        var service = GetAttribute(span.ResourceAttributes, "service.name") ?? string.Empty;
        var duration = $"{FormatDuration(span.DurationMs)}ms";
        var parts = new List<string>
        {
            Truncate(operation),
            Truncate(service),
            duration
        };

        if (SpanSelection.IsError(span))
        {
            parts.Add("ERROR");
        }

        var function = GetAttribute(span.SpanAttributes, "code.function");
        var filePath = GetAttribute(span.SpanAttributes, "code.filepath");
        var lineNumber = GetAttribute(span.SpanAttributes, "code.lineno");
        if (function is not null)
        {
            parts.Add(Truncate(function));
        }

        if (filePath is not null)
        {
            var source = lineNumber is null
                ? filePath
                : $"{filePath}:{lineNumber}";
            parts.Add(Truncate(source));
        }

        parts.Add(Truncate(span.SpanId));
        return $"{Truncate(span.SpanName)} [{string.Join(" · ", parts)}]";
    }

    private static string GetOperationLabel(TraceSpan span)
    {
        var http = span.Data as HttpTraceSpanData;
        var method = FirstNonEmpty(
            http?.Method,
            GetAttribute(span.SpanAttributes, "http.request.method"),
            GetAttribute(span.SpanAttributes, "http.method"));
        var route = FirstNonEmpty(
            GetAttribute(span.SpanAttributes, "http.route"),
            GetRoute(http?.Url));
        if (method is not null || route is not null)
        {
            return method is not null && route is not null
                ? $"{method} {route}"
                : method ?? route!;
        }

        var database = span.Data as DatabaseTraceSpanData;
        var system = FirstNonEmpty(
            database?.System,
            GetAttribute(span.SpanAttributes, "db.system"));
        var operation = FirstNonEmpty(
            database?.Operation,
            GetAttribute(span.SpanAttributes, "db.operation"),
            GetAttribute(span.SpanAttributes, "db.operation.name"));
        if (system is not null || operation is not null)
        {
            return system is not null && operation is not null
                ? $"{system} {operation}"
                : system ?? operation!;
        }

        var graphQl = span.Data as GraphQLOperationTraceSpanData;
        var graphQlName = FirstNonEmpty(
            graphQl?.Operation?.Name,
            GetAttribute(span.SpanAttributes, "graphql.operation.name"));
        return graphQlName ?? span.SpanKind;
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(static value => !string.IsNullOrEmpty(value));

    private static string? GetRoute(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return string.IsNullOrEmpty(uri.AbsolutePath) ? "/" : uri.AbsolutePath;
        }

        var queryStart = url.IndexOfAny(['?', '#']);
        return queryStart < 0 ? url : url[..queryStart];
    }

    private static void RenderExceptions(
        TraceSpan span,
        string prefix,
        List<string> lines)
    {
        foreach (var traceEvent in span.Events.Where(static traceEvent =>
                     traceEvent.Name.Equals("exception", StringComparison.OrdinalIgnoreCase)))
        {
            var type = GetAttribute(traceEvent.Attributes, "exception.type") ?? string.Empty;
            var message = GetAttribute(traceEvent.Attributes, "exception.message") ?? string.Empty;
            lines.Add($"{prefix}exception: {type}: {NormalizeLine(message)}");

            var stackTrace = GetAttribute(traceEvent.Attributes, "exception.stacktrace");
            if (stackTrace is null)
            {
                continue;
            }

            var stackPrefix = prefix + "   ";
            foreach (var stackLine in SplitLines(stackTrace))
            {
                lines.Add(stackPrefix + stackLine);
            }
        }
    }

    private static IEnumerable<string> SplitLines(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.None);

    private static string? GetAttribute(
        IReadOnlyList<TelemetryAttribute> attributes,
        string key)
        => attributes.FirstOrDefault(attribute => attribute.Key == key)?.Value;

    private static string NormalizeLine(string value)
        => value.Replace("\r\n", "\\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace("\n", "\\n", StringComparison.Ordinal);

    private static string FormatDuration(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Truncate(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= MaximumValueLength)
        {
            return NormalizeLine(value ?? string.Empty);
        }

        return NormalizeLine(string.Concat(value.AsSpan(0, MaximumValueLength - 1), "…"));
    }
}
