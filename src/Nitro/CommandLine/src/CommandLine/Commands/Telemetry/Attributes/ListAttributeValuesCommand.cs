using System.Globalization;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Arguments;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Attributes;

internal sealed class ListAttributeValuesCommand : Command
{
    public ListAttributeValuesCommand() : base("values")
    {
        Description = "List telemetry attribute values.";

        Arguments.Add(Opt<AttributeKeyArgument>.Instance);
        Options.Add(Opt<AttributeSignalOption>.Instance);
        Options.Add(Opt<AttributeKindOption>.Instance);
        Options.Add(Opt<AttributeValueSearchOption>.Instance);
        Options.Add(Opt<TelemetrySinceOption>.Instance);
        Options.Add(Opt<TelemetryUntilOption>.Instance);
        Options.Add(Opt<TelemetryLimitOption>.Instance);
        Options.Add(Opt<OptionalCursorOption>.Instance);

        TelemetryCommandOptions.AddOptions(this);

        TelemetryCommandOptions.AddTimeRangeValidator(this);

        this.AddExamples("telemetry attributes values service.name --signal traces");

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

        var key = parseResult.GetRequiredValue(Opt<AttributeKeyArgument>.Instance);
        var signal = parseResult.GetRequiredValue(Opt<AttributeSignalOption>.Instance);
        var kind = parseResult.GetValue(Opt<AttributeKindOption>.Instance);
        var search = parseResult.GetValue(Opt<AttributeValueSearchOption>.Instance);
        var since = parseResult.GetValue(Opt<TelemetrySinceOption>.Instance);
        var until = parseResult.GetValue(Opt<TelemetryUntilOption>.Instance);
        var cursor = parseResult.GetValue(Opt<OptionalCursorOption>.Instance);
        var limit = parseResult.GetValue(Opt<TelemetryLimitOption>.Instance) ?? 50;

        var page = await client.ListAttributeValuesAsync(
            workspaceId,
            signal,
            key,
            kind,
            search,
            since,
            until,
            limit,
            cursor,
            cancellationToken);

        var items = page.Items.Select(AttributeValueListItem.From).ToArray();
        resultHolder.SetResult(new PaginatedListResult<AttributeValueListItem>(items, page.EndCursor));

        return ExitCodes.Success;
    }

    internal sealed record AttributeValueListItem(string Value)
    {
        public static AttributeValueListItem From(AttributeValue value)
        {
            if (value.String is { } stringValue)
            {
                return new AttributeValueListItem(stringValue);
            }

            if (value.Int is { } intValue)
            {
                return new AttributeValueListItem(intValue.ToString(CultureInfo.InvariantCulture));
            }

            if (value.Float is { } floatValue)
            {
                return new AttributeValueListItem(floatValue.ToString(CultureInfo.InvariantCulture));
            }

            return new AttributeValueListItem(
                value.Boolean switch
                {
                    true => "true",
                    false => "false",
                    _ => string.Empty
                });
        }
    }
}
