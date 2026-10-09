using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Attributes;

internal sealed class ListAttributeKeysCommand : Command
{
    public ListAttributeKeysCommand() : base("keys")
    {
        Description = "List telemetry attribute keys.";

        Options.Add(Opt<AttributeSignalOption>.Instance);
        Options.Add(Opt<AttributeKindsOption>.Instance);
        Options.Add(Opt<AttributeKeySearchOption>.Instance);
        Options.Add(Opt<TelemetrySinceOption>.Instance);
        Options.Add(Opt<TelemetryUntilOption>.Instance);
        Options.Add(Opt<TelemetryLimitOption>.Instance);
        Options.Add(Opt<OptionalCursorOption>.Instance);

        TelemetryCommandOptions.AddOptions(this);

        TelemetryCommandOptions.AddTimeRangeValidator(this);

        this.AddExamples("telemetry attributes keys --signal traces");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var client = services.GetRequiredService<ITelemetryClient>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        parseResult.AssertHasAuthentication(sessionService);

        var workspaceId = parseResult.GetWorkspaceId(sessionService);

        var signal = parseResult.GetRequiredValue(Opt<AttributeSignalOption>.Instance);
        var kinds = parseResult.GetValue(Opt<AttributeKindsOption>.Instance);
        var search = parseResult.GetValue(Opt<AttributeKeySearchOption>.Instance);
        var since = parseResult.GetValue(Opt<TelemetrySinceOption>.Instance);
        var until = parseResult.GetValue(Opt<TelemetryUntilOption>.Instance);
        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);
        var limit = parseResult.GetValue(Opt<TelemetryLimitOption>.Instance) ?? 50;

        var page = await client.ListAttributeKeysAsync(
            workspaceId,
            signal,
            kinds,
            search,
            since,
            until,
            limit,
            cursor,
            cancellationToken);

        var items = page.Items.Select(AttributeKeyListItem.From).ToArray();
        resultHolder.SetResult(new PaginatedListResult<AttributeKeyListItem>(items, page.EndCursor));

        return ExitCodes.Success;
    }

    internal sealed record AttributeKeyListItem(string Key, string Kind)
    {
        public static AttributeKeyListItem From(AttributeKeyRow key) => new(key.Path, key.Kind);
    }
}
