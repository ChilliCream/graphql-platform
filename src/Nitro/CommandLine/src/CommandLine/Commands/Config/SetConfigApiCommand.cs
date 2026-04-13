using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Config;

internal sealed class SetConfigApiCommand : Command
{
    public SetConfigApiCommand() : base("api")
    {
        Description = "Set the default API id used by analytical commands.";

        Arguments.Add(Opt<ConfigApiIdArgument>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("config set api acme-catalog");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var sessionService = services.GetRequiredService<ISessionService>();

        parseResult.AssertHasAuthentication(sessionService);

        var apiId = parseResult.GetRequiredValue(Opt<ConfigApiIdArgument>.Instance);

        await sessionService.SetDefaultsAsync(
            SessionDefault<string>.Set(apiId),
            stage: null,
            format: null,
            cancellationToken);

        console.OkQuestion("Default API", apiId);

        return ExitCodes.Success;
    }
}
