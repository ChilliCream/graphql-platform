using System.Text.Json;
using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
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

        TelemetryCommandOptions.ConfigureOutput(console, parseResult);

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

        Render(console, detail);
        return ExitCodes.Success;
    }

    private static void Render(INitroConsole console, LogDetail detail)
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
        WriteLine(console, code.Length == 0 ? header : $"{header} [{code}]");

        foreach (var attribute in detail.Attributes)
        {
            WriteLine(console, $"{attribute.Key}: {attribute.Value}");
        }

        foreach (var attribute in detail.ResourceAttributes)
        {
            WriteLine(console, $"{attribute.Key}: {attribute.Value}");
        }

        if (detail.Scope is { } scope)
        {
            WriteLine(console, $"scope: {scope.Name ?? string.Empty}");

            if (scope.Version is not null)
            {
                WriteLine(console, $"scope.version: {scope.Version}");
            }

            if (scope.SchemaUrl is not null)
            {
                WriteLine(console, $"scope.schema_url: {scope.SchemaUrl}");
            }

            foreach (var attribute in scope.Attributes)
            {
                WriteLine(console, $"scope.{attribute.Key}: {attribute.Value}");
            }
        }

        WriteLine(console, $"trace id: {detail.TraceId}");
        WriteLine(console, $"span id: {detail.SpanId}");
    }

    private static void WriteLine(INitroConsole console, string value)
    {
        console.Write(new Text(value));
        console.WriteLine();
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

internal sealed class LogIdArgument : Argument<string>
{
    public LogIdArgument() : base("log-id")
    {
        Description = "The log ID";
        Arity = ArgumentArity.ExactlyOne;
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ShowLogCommand.LogDetail))]
internal partial class LogDetailJsonContext : JsonSerializerContext;
