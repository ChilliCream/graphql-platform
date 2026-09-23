using System.Text.Json;
using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Arguments;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;
using ChilliCream.Nitro.CommandLine.Helpers;
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
        var console = services.GetRequiredService<INitroConsole>();
        var client = services.GetRequiredService<ITelemetryClient>();
        var sessionService = services.GetRequiredService<ISessionService>();

        if (!TelemetryCommandOptions.TryGetWorkspaceId(console, parseResult, sessionService, out var workspaceId))
        {
            return ExitCodes.Error;
        }

        var id = parseResult.GetRequiredValue(Opt<LogIdArgument>.Instance);
        var log = await client.GetLogAsync(workspaceId, id, cancellationToken);

        if (log is null)
        {
            return TelemetryErrorRenderer.Render(
                console,
                $"The log '{id}' was not found.",
                "run nitro telemetry logs list");
        }

        var detail = LogDetail.From(log);

        if (console.IsAgentMode || !console.IsHumanReadable)
        {
            console.WriteRawLine(JsonSerializer.Serialize(detail, LogDetailJsonContext.Default.LogDetail));
            return ExitCodes.Success;
        }

        var renderedDetail = new LogDetailRenderer().Render(detail);
        if (renderedDetail.Length > 0)
        {
            foreach (var line in renderedDetail.Split(Environment.NewLine, StringSplitOptions.None))
            {
                console.WriteRawLine(line);
            }
        }

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
                LogPresentation.GetServiceName(log.ResourceAttributes),
                log.Body,
                log.TraceId,
                log.SpanId,
                log.BodyDetail,
                log.LogAttributes.Select(LogAttributeDetail.From).ToArray(),
                log.ResourceAttributes.Select(LogAttributeDetail.From).ToArray(),
                log.Scope is null ? null : LogScopeDetail.From(log.Scope),
                LogPresentation.GetAttributeValue(log.LogAttributes, "code.function"),
                LogPresentation.GetAttributeValue(log.LogAttributes, "code.filepath"),
                log.LogAttributes
                    .FirstOrDefault(static attribute => attribute.Key == "code.lineno")
                    ?.Long);
    }

    internal sealed record LogAttributeDetail(string Key, string Value)
    {
        public static LogAttributeDetail From(TypedTelemetryAttribute attribute)
            => new(attribute.Key, LogPresentation.FormatAttributeValue(attribute));

        public static LogAttributeDetail From(TelemetryAttribute attribute)
            => new(attribute.Key, attribute.Value);
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

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ShowLogCommand.LogDetail))]
internal partial class LogDetailJsonContext : JsonSerializerContext;
