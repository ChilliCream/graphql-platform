using System.Globalization;
using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Arguments;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Attributes;

internal sealed class ListAttributeValuesCommand : Command
{
    public ListAttributeValuesCommand() : base("values")
    {
        Description = "List telemetry attribute values.";

        Arguments.Add(Opt<AttributeKeyArgument>.Instance);
        Options.Add(Opt<AttributeSignalOption>.Instance);
        Options.Add(Opt<TelemetrySearchOption>.Instance);
        Options.Add(Opt<TelemetrySinceOption>.Instance);
        Options.Add(Opt<TelemetryUntilOption>.Instance);
        Options.Add(Opt<TelemetryLimitOption>.Instance);

        TelemetryCommandOptions.AddOptions(this);

        this.AddExamples("telemetry attributes values service.name --signal traces");

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

        if (!TelemetryCommandOptions.TryGetWorkspaceId(console, parseResult, sessionService, out var workspaceId))
        {
            return ExitCodes.Error;
        }

        var key = parseResult.GetRequiredValue(Opt<AttributeKeyArgument>.Instance);
        var signal = parseResult.GetRequiredValue(Opt<AttributeSignalOption>.Instance);
        var search = parseResult.GetValue(Opt<TelemetrySearchOption>.Instance);
        var since = parseResult.GetValue(Opt<TelemetrySinceOption>.Instance);
        var until = parseResult.GetValue(Opt<TelemetryUntilOption>.Instance);
        var limit = parseResult.GetValue(Opt<TelemetryLimitOption>.Instance) ?? 50;
        var page = await client.ListAttributeValuesAsync(
            workspaceId,
            signal,
            key,
            kind: null,
            search,
            since,
            until,
            limit,
            after: null,
            cancellationToken);

        var items = page.Items.Select(AttributeValueListItem.From).ToArray();
        console.WriteListEnvelope(
            items,
            total: null,
            page.HasNextPage,
            AttributeValueListJsonContext.Default.AttributeValueListItem,
            emptyResultHint: null);

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

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ListAttributeValuesCommand.AttributeValueListItem))]
internal partial class AttributeValueListJsonContext : JsonSerializerContext;
