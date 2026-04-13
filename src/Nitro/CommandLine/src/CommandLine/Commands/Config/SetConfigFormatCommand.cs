using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Output;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Config;

internal sealed class SetConfigFormatCommand : Command
{
    public SetConfigFormatCommand() : base("format")
    {
        Description = "Set the default output format used by analytical commands.";

        Arguments.Add(Opt<ConfigFormatArgument>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("config set format markdown");

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

        var rawFormat = parseResult.GetRequiredValue(Opt<ConfigFormatArgument>.Instance);

        if (!Enum.TryParse<OutputFormat>(rawFormat, ignoreCase: true, out var format))
        {
            throw new ExitException($"Unknown format '{rawFormat}'.");
        }

        await sessionService.SetDefaultsAsync(
            apiId: null,
            stage: null,
            SessionDefault<OutputFormat>.Set(format),
            cancellationToken);

        console.OkQuestion("Default format", format.ToString().ToLowerInvariant());

        return ExitCodes.Success;
    }
}
