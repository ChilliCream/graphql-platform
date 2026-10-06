using System.Globalization;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Arguments;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Logs;

internal sealed class ShowLogCommand : Command
{
    public ShowLogCommand() : base("show")
    {
        Description = "Show a telemetry log.";

        Arguments.Add(Opt<LogIdArgument>.Instance);

        TelemetryCommandOptions.AddOptions(this);

        this.AddExamples("telemetry logs show \"<log-id>\"");

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

        var id = parseResult.GetRequiredValue(Opt<LogIdArgument>.Instance);

        var log = await client.GetLogAsync(workspaceId, id, cancellationToken);

        if (log is null)
        {
            throw ThrowHelper.Exit($"The log '{id.EscapeMarkup()}' was not found.");
        }

        var detail = LogDetail.From(log);

        resultHolder.SetResult(new ObjectResult(detail));

        return ExitCodes.Success;
    }

    internal sealed record LogDetail(
        string Id,
        double Epoch,
        string SeverityText,
        int SeverityNumber,
        string ServiceName,
        string Body,
        string TraceId,
        string SpanId,
        LogBodyDetail BodyDetail,
        IReadOnlyList<LogAttributeDetail> Attributes,
        IReadOnlyList<LogAttributeDetail> ResourceAttributes,
        LogScopeDetail? Scope,
        string? CodeFunction,
        string? CodeFilePath,
        long? CodeLineNumber)
    {
        public static LogDetail From(Log log)
            => new(
                log.Id,
                log.Start,
                log.SeverityText,
                log.SeverityNumber,
                log.ResourceAttributes.GetServiceName(),
                log.Body,
                log.TraceId,
                log.SpanId,
                log.BodyDetail,
                log.LogAttributes.Select(LogAttributeDetail.From).ToArray(),
                log.ResourceAttributes.Select(LogAttributeDetail.From).ToArray(),
                log.Scope is null ? null : LogScopeDetail.From(log.Scope),
                log.LogAttributes.GetAttributeValue(WellKnownAttributeNames.CodeFunction),
                log.LogAttributes.GetAttributeValue(WellKnownAttributeNames.CodeFilePath),
                log.LogAttributes.FirstOrDefault(static attribute =>
                    attribute.Key == WellKnownAttributeNames.CodeLineNumber
                )?.Long);
    }

    internal sealed record LogAttributeDetail(string Key, string Value)
    {
        public static LogAttributeDetail From(TypedTelemetryAttribute attribute)
            => new(attribute.Key, attribute.FormatAttributeValue());

        public static LogAttributeDetail From(TelemetryAttribute attribute) => new(attribute.Key, attribute.Value);
    }

    internal sealed record LogScopeDetail(
        string? Name,
        string? SchemaUrl,
        string? Version,
        IReadOnlyList<LogAttributeDetail> Attributes)
    {
        public static LogScopeDetail From(TelemetryScope scope)
            => new(
                scope.Name,
                scope.SchemaUrl,
                scope.Version,
                scope.Attributes.Select(LogAttributeDetail.From).ToArray());
    }
}

file static class Extensions
{
    extension(IReadOnlyList<TelemetryAttribute> resourceAttributes)
    {
        public string GetServiceName()
            => resourceAttributes
                .FirstOrDefault(static attribute => attribute.Key == WellKnownAttributeNames.ServiceName)
                ?.Value
            ?? string.Empty;
    }

    extension(IReadOnlyList<TypedTelemetryAttribute> attributes)
    {
        public string? GetAttributeValue(string key)
        {
            return attributes.FirstOrDefault(attribute => attribute.Key == key)?.FormatAttributeValue();
        }
    }

    extension(TypedTelemetryAttribute attribute)
    {
        public string FormatAttributeValue()
        {
            if (attribute.String is { } stringValue)
            {
                return stringValue;
            }

            if (attribute.Long is { } longValue)
            {
                return longValue.ToString(CultureInfo.InvariantCulture);
            }

            if (attribute.Float is { } floatValue)
            {
                return floatValue.ToString(CultureInfo.InvariantCulture);
            }

            return attribute.Boolean switch
            {
                true => "true",
                false => "false",
                _ => string.Empty
            };
        }
    }
}
