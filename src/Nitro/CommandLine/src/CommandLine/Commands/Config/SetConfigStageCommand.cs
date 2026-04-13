using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Config;

internal sealed class SetConfigStageCommand : Command
{
    public SetConfigStageCommand() : base("stage")
    {
        Description = "Set the default stage name used by analytical commands.";

        Arguments.Add(Opt<ConfigStageNameArgument>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("config set stage prod");

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

        var stage = parseResult.GetRequiredValue(Opt<ConfigStageNameArgument>.Instance);

        await sessionService.SetDefaultsAsync(
            apiId: null,
            SessionDefault<string>.Set(stage),
            format: null,
            cancellationToken);

        console.OkQuestion("Default stage", stage);

        return ExitCodes.Success;
    }
}
