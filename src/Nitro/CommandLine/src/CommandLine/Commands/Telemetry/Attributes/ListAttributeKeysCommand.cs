using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Attributes;

internal sealed class ListAttributeKeysCommand : Command
{
    public ListAttributeKeysCommand() : base("keys")
    {
        Description = "List telemetry attribute keys.";

        Options.Add(Opt<AttributeSignalOption>.Instance);
        Options.Add(Opt<AttributeKindsOption>.Instance);
        Options.Add(Opt<TelemetrySearchOption>.Instance);
        Options.Add(Opt<TelemetrySinceOption>.Instance);
        Options.Add(Opt<TelemetryUntilOption>.Instance);
        Options.Add(Opt<TelemetryLimitOption>.Instance);

        TelemetryCommandOptions.AddOptions(this);

        this.AddExamples("telemetry attributes keys --signal traces");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<ITelemetryClient>();
        var sessionService = services.GetRequiredService<ISessionService>();

        TelemetryCommandOptions.ConfigureOutput(console, parseResult);

        if (!TelemetryCommandOptions.TryGetWorkspaceId(console, parseResult, sessionService, out var workspaceId))
        {
            return ExitCodes.Error;
        }

        var signal = parseResult.GetRequiredValue(Opt<AttributeSignalOption>.Instance);
        var kinds = parseResult.GetValue(Opt<AttributeKindsOption>.Instance);
        var search = parseResult.GetValue(Opt<TelemetrySearchOption>.Instance);
        var since = parseResult.GetValue(Opt<TelemetrySinceOption>.Instance);
        var until = parseResult.GetValue(Opt<TelemetryUntilOption>.Instance);
        var limit = parseResult.GetValue(Opt<TelemetryLimitOption>.Instance) ?? 50;
        var page = await client.ListAttributeKeysAsync(
            workspaceId,
            signal,
            kinds,
            search,
            since,
            until,
            limit,
            after: null,
            cancellationToken);

        var items = page.Items.Select(AttributeKeyListItem.From).ToArray();
        var renderer = new TelemetryListRenderer(console);
        renderer.Render(
            items,
            total: null,
            page.HasNextPage,
            "attribute keys",
            AttributeKeyListJsonContext.Default.AttributeKeyListItem,
            new TelemetryListColumn<AttributeKeyListItem>("Key", item => item.Key),
            new TelemetryListColumn<AttributeKeyListItem>("Kind", item => item.Kind));

        return ExitCodes.Success;
    }

    internal sealed record AttributeKeyListItem(string Key, string Kind)
    {
        public static AttributeKeyListItem From(AttributeKeyRow key)
            => new(key.Path, key.Kind);
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ListAttributeKeysCommand.AttributeKeyListItem))]
internal partial class AttributeKeyListJsonContext : JsonSerializerContext;
