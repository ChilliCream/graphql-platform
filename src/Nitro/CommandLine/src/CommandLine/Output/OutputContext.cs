using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Output;

/// <summary>
/// The resolved output context for an analytical command invocation. Captures everything a
/// formatter needs to render an envelope: the api/stage being queried, the time window, and
/// the format mode.
/// </summary>
internal sealed record OutputContext(
    string ApiId,
    string Stage,
    DateTimeOffset From,
    DateTimeOffset To,
    OutputFormat Format)
{
    public OutputEnvelopeWindow ToWindow() => new(From, To);
}

/// <summary>
/// Resolves the output context for an analytical command from the parse result, the active
/// session, and the console. Centralises the "explicit flag → session default → error"
/// resolution rules so every command behaves identically.
/// </summary>
internal static class OutputContextResolver
{
    /// <summary>
    /// The default lookback applied when neither <c>--from</c> nor <c>--to</c> are supplied.
    /// </summary>
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromDays(7);

    public static OutputContext Resolve(
        ParseResult parseResult,
        INitroConsole console,
        ISessionService sessionService)
    {
        var session = sessionService.Session;

        var apiId = parseResult.GetValue(Opt<AnalyticsApiIdOption>.Instance)
            ?? session?.DefaultApiId
            ?? throw new ExitException(
                "No API specified. Pass '--api-id' or run 'nitro config set api <id>'.");

        var stage = parseResult.GetValue(Opt<AnalyticsStageNameOption>.Instance)
            ?? session?.DefaultStage
            ?? throw new ExitException(
                "No stage specified. Pass '--stage' or run 'nitro config set stage <name>'.");

        var (from, to) = ResolveWindow(parseResult);

        var format = ResolveFormat(parseResult, console, session);

        return new OutputContext(apiId, stage, from, to, format);
    }

    /// <summary>
    /// Resolves the format from explicit flag, session default, then auto-detection.
    /// Auto-detection mirrors <c>gh</c> and <c>kubectl</c>: table for a TTY, json otherwise.
    /// </summary>
    public static OutputFormat ResolveFormat(
        ParseResult parseResult,
        INitroConsole console,
        Session? session)
    {
        var explicitFormat = parseResult.GetValue(Opt<FormatOption>.Instance);
        if (explicitFormat is { } value)
        {
            return value;
        }

        if (session?.DefaultFormat is { } defaultFormat)
        {
            return defaultFormat;
        }

        return console.IsInteractive ? OutputFormat.Table : OutputFormat.Json;
    }

    private static (DateTimeOffset From, DateTimeOffset To) ResolveWindow(
        ParseResult parseResult)
    {
        var from = parseResult.GetValue(Opt<FromOption>.Instance);
        var to = parseResult.GetValue(Opt<ToOption>.Instance);

        var resolvedTo = to ?? DateTimeOffset.UtcNow;
        var resolvedFrom = from ?? resolvedTo.Subtract(DefaultWindow);

        if (resolvedFrom > resolvedTo)
        {
            throw new ExitException(
                "Invalid time window: '--from' must be earlier than or equal to '--to'.");
        }

        return (resolvedFrom, resolvedTo);
    }
}
