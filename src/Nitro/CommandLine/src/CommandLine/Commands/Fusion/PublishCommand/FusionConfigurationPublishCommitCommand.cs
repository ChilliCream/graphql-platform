using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;

internal sealed class FusionConfigurationPublishCommitCommand : Command
{
    public FusionConfigurationPublishCommitCommand() : base("commit")
    {
        Description = "Commit a Fusion configuration publish.";
        AddOption(Opt<OptionalRequestIdOption>.Instance);
        AddOption(Opt<FusionArchiveFileOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        ISessionService sessionService,
        CancellationToken ct)
    {
        var requestId =
            context.ParseResult.GetValueForOption(Opt<OptionalRequestIdOption>.Instance) ??
            await FusionConfigurationPublishingState.GetRequestId(ct) ??
            throw new ExitException(
                "No request id was provided and no request id was found in the cache. Please provide a request id.");

        var configurationFile =
            context.ParseResult.GetValueForOption(Opt<FusionArchiveFileOption>.Instance)!;

        console.Title("Commit the composition of a fusion configuration");

        var committed = false;

        if (console.IsHumanReadable())
        {
            await console
                .Status()
                .Spinner(Spinner.Known.BouncingBar)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Committing...", Commit);
        }
        else
        {
            await Commit(null);
        }

        if (!committed)
        {
            throw Exit("The commit has failed.");
        }

        console.Success("Fusion composition was successful.");

        return ExitCodes.Success;

        async Task Commit(StatusContext? ctx)
        {
            var stream = FileHelpers.CreateFileStream(configurationFile);
            committed = await FusionPublishHelpers.UploadFusionArchiveAsync(
                requestId,
                stream,
                ctx,
                console,
                client,
                ct);
        }
    }
}
