using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Output;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Config;

internal sealed class UnsetConfigCommand : Command
{
    public UnsetConfigCommand() : base("unset")
    {
        Description = "Clear an analytical command default (api, stage, format).";

        Arguments.Add(Opt<UnsetConfigKeyArgument>.Instance);

        this.AddGlobalNitroOptions();

        this.AddExamples("config unset api");

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

        var key = parseResult.GetRequiredValue(Opt<UnsetConfigKeyArgument>.Instance);

        switch (key)
        {
            case UnsetConfigKeyArgument.Api:
                await sessionService.SetDefaultsAsync(
                    SessionDefault<string>.Unset,
                    stage: null,
                    format: null,
                    cancellationToken);
                break;

            case UnsetConfigKeyArgument.Stage:
                await sessionService.SetDefaultsAsync(
                    apiId: null,
                    SessionDefault<string>.Unset,
                    format: null,
                    cancellationToken);
                break;

            case UnsetConfigKeyArgument.Format:
                await sessionService.SetDefaultsAsync(
                    apiId: null,
                    stage: null,
                    SessionDefault<OutputFormat>.Unset,
                    cancellationToken);
                break;

            default:
                throw new ExitException($"Unknown config key '{key}'.");
        }

        console.OkLine($"Cleared default '{key}'.");

        return ExitCodes.Success;
    }
}
