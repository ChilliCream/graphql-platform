using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;

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

            var linePrefix = entry.IsRoot ? string.Empty : entry.ParentPrefix + (entry.IsLast ? "└─ " : "├─ ");
            lines.Add(linePrefix + FormatSpan(entry.Node.Span));

            var exceptionPrefix = entry.IsRoot ? "  " : entry.ParentPrefix + (entry.IsLast ? "   " : "│  ");
            RenderExceptions(entry.Node.Span, exceptionPrefix, lines);

            var childPrefix = entry.IsRoot ? string.Empty : entry.ParentPrefix + (entry.IsLast ? "   " : "│  ");
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
        var service = span.ResourceAttributes.GetAttribute(WellKnownAttributeNames.ServiceName) ?? string.Empty;
        var duration = $"{span.DurationMs.FormatDuration()}ms";
        var parts = new List<string>
        {
            operation.EscapeControlCharacters(),
            service.EscapeControlCharacters(),
            duration
        };

        if (span.IsError)
        {
            parts.Add("ERROR");
        }

        var function = span.SpanAttributes.GetAttribute(WellKnownAttributeNames.CodeFunction);
        var filePath = span.SpanAttributes.GetAttribute(WellKnownAttributeNames.CodeFilePath);
        var lineNumber = span.SpanAttributes.GetAttribute(WellKnownAttributeNames.CodeLineNumber);
        if (function is not null)
        {
            parts.Add(function.EscapeControlCharacters());
        }

        if (filePath is not null)
        {
            var source = lineNumber is null ? filePath : $"{filePath}:{lineNumber}";
            parts.Add(source.EscapeControlCharacters());
        }

        parts.Add(span.SpanId.EscapeControlCharacters());
        return $"{span.SpanName.EscapeControlCharacters()} [{string.Join(" · ", parts)}]";
    }

    private static string GetOperationLabel(TraceSpan span)
    {
        var http = span.Data as HttpTraceSpanData;
        var method = string.FirstNonEmpty(
            http?.Method,
            span.SpanAttributes.GetAttribute(WellKnownAttributeNames.HttpRequestMethod),
            span.SpanAttributes.GetAttribute(WellKnownAttributeNames.HttpMethod));
        var route = string.FirstNonEmpty(
            span.SpanAttributes.GetAttribute(WellKnownAttributeNames.HttpRoute),
            http?.Url.GetRoute());

        if (method is not null || route is not null)
        {
            return method is not null && route is not null ? $"{method} {route}" : method ?? route!;
        }

        var database = span.Data as DatabaseTraceSpanData;
        var system = string.FirstNonEmpty(
            database?.System,
            span.SpanAttributes.GetAttribute(WellKnownAttributeNames.DbSystem));
        var operation = string.FirstNonEmpty(
            database?.Operation,
            span.SpanAttributes.GetAttribute(WellKnownAttributeNames.DbOperation),
            span.SpanAttributes.GetAttribute(WellKnownAttributeNames.DbOperationName));

        if (system is not null || operation is not null)
        {
            return system is not null && operation is not null ? $"{system} {operation}" : system ?? operation!;
        }

        var graphQl = span.Data as GraphQLOperationTraceSpanData;
        var graphQlName = string.FirstNonEmpty(
            graphQl?.Operation?.Name,
            span.SpanAttributes.GetAttribute(WellKnownAttributeNames.GraphQLOperationName));
        return graphQlName ?? span.SpanKind;
    }

    private static void RenderExceptions(TraceSpan span, string prefix, List<string> lines)
    {
        var exceptionEvents = span.Events.Where(static traceEvent =>
            traceEvent.Name.Equals("exception", StringComparison.OrdinalIgnoreCase)
        );

        foreach (var traceEvent in exceptionEvents)
        {
            var type = traceEvent.Attributes.GetAttribute(WellKnownAttributeNames.ExceptionType) ?? string.Empty;
            var message = traceEvent.Attributes.GetAttribute(WellKnownAttributeNames.ExceptionMessage) ?? string.Empty;
            lines.Add($"{prefix}exception: {type.EscapeControlCharacters()}: {message.EscapeControlCharacters()}");

            var stackTrace = traceEvent.Attributes.GetAttribute(WellKnownAttributeNames.ExceptionStackTrace);
            if (stackTrace is null)
            {
                continue;
            }

            var stackPrefix = prefix + "   ";
            foreach (var stackLine in stackTrace.SplitLines())
            {
                lines.Add(stackPrefix + stackLine.EscapeControlCharacters());
            }
        }
    }
}

file static class Extensions
{
    extension(IReadOnlyList<TelemetryAttribute> attributes)
    {
        public string? GetAttribute(string key) => attributes.FirstOrDefault(attribute => attribute.Key == key)?.Value;
    }

    extension(string value)
    {
        public IEnumerable<string> SplitLines()
            => value
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n', StringSplitOptions.None);

        public static string? FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(static candidate => !string.IsNullOrEmpty(candidate));
    }

    extension(string? url)
    {
        public string? GetRoute()
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
    }
}
