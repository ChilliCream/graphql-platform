namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Logs;

internal sealed class LogDetailRenderer
{
    public string Render(ShowLogCommand.LogDetail detail)
    {
        var source = detail.CodeFilePath is null
            ? null
            : detail.CodeLineNumber is null
                ? detail.CodeFilePath
                : $"{detail.CodeFilePath}:{detail.CodeLineNumber}";
        var code = string.Join(
            " ",
            new[] { detail.CodeFunction, source }.Where(static value => value is not null));
        var header = $"{LogPresentation.FormatTimestamp(detail.Epoch)} {detail.SeverityText} "
            + $"{detail.ServiceName} {detail.Body}";
        var lines = new List<string> { code.Length == 0 ? header : $"{header} [{code}]" };

        foreach (var attribute in detail.Attributes)
        {
            lines.Add($"{attribute.Key}: {attribute.Value}");
        }

        foreach (var attribute in detail.ResourceAttributes)
        {
            lines.Add($"{attribute.Key}: {attribute.Value}");
        }

        if (detail.Scope is { } scope)
        {
            lines.Add($"scope: {scope.Name ?? string.Empty}");

            if (scope.Version is not null)
            {
                lines.Add($"scope.version: {scope.Version}");
            }

            if (scope.SchemaUrl is not null)
            {
                lines.Add($"scope.schema_url: {scope.SchemaUrl}");
            }

            foreach (var attribute in scope.Attributes)
            {
                lines.Add($"scope.{attribute.Key}: {attribute.Value}");
            }
        }

        lines.Add($"trace id: {detail.TraceId}");
        lines.Add($"span id: {detail.SpanId}");

        return string.Join(Environment.NewLine, lines);
    }
}
