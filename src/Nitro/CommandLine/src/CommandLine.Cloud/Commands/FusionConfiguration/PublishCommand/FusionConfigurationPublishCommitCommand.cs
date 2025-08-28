using System.CommandLine.Invocation;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Helpers;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using StrawberryShake;
using static ChilliCream.Nitro.CommandLine.Cloud.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.FusionConfiguration;

internal sealed class FusionConfigurationPublishCommitCommand : Command
{
    public FusionConfigurationPublishCommitCommand() : base("commit")
    {
        Description = "Commit a fusion configuration publish.";
        AddOption(Opt<OptionalRequestIdOption>.Instance);
        AddOption(Opt<ConfigurationFileOption>.Instance);

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
            context.ParseResult.GetValueForOption(Opt<ConfigurationFileOption>.Instance)!;

        console.Title("Commit the composition of a fusion configuration");

        var committed = false;

        if (console.IsHumandReadable())
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

        return ExitCodes.Success;

        async Task Commit(StatusContext? ctx)
        {
            var stream = FileHelpers.CreateFileStream(configurationFile);
            committed = await FusionConfigurationPublishHelpers.UploadConfigurationAsync(
                requestId,
                stream,
                ctx,
                console,
                client,
                ct);
        }
    }
}
