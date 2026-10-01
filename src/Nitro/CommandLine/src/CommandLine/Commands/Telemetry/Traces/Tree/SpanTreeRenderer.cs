using System.Globalization;
using ChilliCream.Nitro.Client.Telemetry.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces.Tree;

internal sealed class SpanTreeRenderer
{
    public string Render(SpanTree tree, string? spanId)
    {
        IReadOnlyList<SpanTreeNode> roots;
        if (spanId is null)
        {
            roots = tree.Roots;
        }
        else if (tree.Find(spanId) is { } root)
        {
            roots = [root];
        }
        else
        {
            return string.Empty;
        }

        var visited = new HashSet<SpanTreeNode>(ReferenceEqualityComparer.Instance);
        var lines = new List<string>();
        RenderNodes(roots, visited, lines);

        if (spanId is null)
        {
            foreach (var node in tree.Nodes)
            {
                if (!visited.Contains(node))
                {
                    RenderNodes([node], visited, lines);
                }
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static void RenderNodes(
        IReadOnlyList<SpanTreeNode> roots,
        HashSet<SpanTreeNode> visited,
        List<string> lines)
    {
        var pending = new Stack<(SpanTreeNode Node, string ParentPrefix, bool IsRoot, bool IsLast)>();
        for (var i = roots.Count - 1; i >= 0; i--)
        {
            pending.Push((roots[i], string.Empty, roots.Count == 1, i == roots.Count - 1));
        }

        while (pending.TryPop(out var entry))
        {
            if (!visited.Add(entry.Node))
            {
                continue;
            }

            var linePrefix = entry.IsRoot
                ? string.Empty
                : entry.ParentPrefix + (entry.IsLast ? "└─ " : "├─ ");
            lines.Add(linePrefix + FormatSpan(entry.Node.Span));

            var exceptionPrefix = entry.IsRoot
                ? "  "
                : entry.ParentPrefix + (entry.IsLast ? "   " : "│  ");
            RenderExceptions(entry.Node.Span, exceptionPrefix, lines);

            var childPrefix = entry.IsRoot
                ? string.Empty
                : entry.ParentPrefix + (entry.IsLast ? "   " : "│  ");
            var children = entry.Node.Children.Where(child => !visited.Contains(child)).ToArray();

            for (var i = children.Length - 1; i >= 0; i--)
            {
                pending.Push((children[i], childPrefix, false, i == children.Length - 1));
            }
        }
    }

    private static string FormatSpan(TraceSpan span)
    {
        var operation = GetOperationLabel(span);
        var service = GetAttribute(span.ResourceAttributes, "service.name") ?? string.Empty;
        var duration = $"{FormatDuration(span.DurationMs)}ms";
        var parts = new List<string>
        {
            NormalizeLine(operation),
            NormalizeLine(service),
            duration
        };

        if (IsError(span))
        {
            parts.Add("ERROR");
        }

        var function = GetAttribute(span.SpanAttributes, "code.function");
        var filePath = GetAttribute(span.SpanAttributes, "code.filepath");
        var lineNumber = GetAttribute(span.SpanAttributes, "code.lineno");
        if (function is not null)
        {
            parts.Add(NormalizeLine(function));
        }

        if (filePath is not null)
        {
            var source = lineNumber is null
                ? filePath
                : $"{filePath}:{lineNumber}";
            parts.Add(NormalizeLine(source));
        }

        parts.Add(NormalizeLine(span.SpanId));
        return $"{NormalizeLine(span.SpanName)} [{string.Join(" · ", parts)}]";
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

    internal static string FormatDuration(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    internal static bool IsError(TraceSpan span)
        => span.StatusCode.Contains("ERROR", StringComparison.OrdinalIgnoreCase)
            || span.Events.Any(static traceEvent =>
                traceEvent.Name.Equals("exception", StringComparison.OrdinalIgnoreCase));
}
