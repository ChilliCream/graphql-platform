using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal static class TelemetryListFilter
{
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
            filter = FilterFlags.Compile(
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
            console.RenderParseError(signal, filterText!, exception);
            filter = null;
            parsedFilter = null;
            return false;
        }
    }

    public static async Task<string?> GetEmptyResultHintAsync(
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

        var signalKind = signal == TelemetryFilterSignal.Traces
            ? OpenTelemetrySignalKind.Traces
            : OpenTelemetrySignalKind.Logs;

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
}

file static class Extensions
{
    extension(INitroConsole console)
    {
        public void RenderParseError(
            TelemetryFilterSignal signal,
            string filter,
            FilterParseException exception)
        {
            var examples = signal == TelemetryFilterSignal.Traces
                ? "hint: examples: `status:error`, `duration:>=100`, or `@resource.service.name:checkout`"
                : "hint: examples: `severity:error`, `@resource.service.name:checkout`, "
                    + "or `exception.type:TimeoutException`";

            console.Error.Write(new Text($"filter: {exception.Message} at column {exception.Column}"));
            console.Error.WriteLine();
            console.Error.Write(new Text(filter));
            console.Error.WriteLine();
            console.Error.Write(new Text($"{new string(' ', exception.Column - 1)}^"));
            console.Error.WriteLine();
            console.Error.Write(new Text(examples));
            console.Error.WriteLine();
        }
    }
}
