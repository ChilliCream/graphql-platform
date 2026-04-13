using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Config;

internal sealed class GetConfigCommand : Command
{
    public GetConfigCommand() : base("get")
    {
        Description = "Show the analytical command defaults (api, stage, format, workspace).";

        this.AddGlobalNitroOptions();

        this.AddExamples("config get");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        var session = sessionService.Session;

        console.MarkupLine($"[bold]api[/]:       {FormatValue(session?.DefaultApiId)}");
        console.MarkupLine($"[bold]stage[/]:     {FormatValue(session?.DefaultStage)}");
        console.MarkupLine(
            $"[bold]format[/]:    {FormatValue(session?.DefaultFormat?.ToString().ToLowerInvariant())}");
        console.MarkupLine($"[bold]workspace[/]: {FormatValue(session?.Workspace?.Name)}");

        return Task.FromResult(ExitCodes.Success);
    }

    private static string FormatValue(string? value)
        => string.IsNullOrEmpty(value) ? "[dim](not set)[/]" : value;
}
