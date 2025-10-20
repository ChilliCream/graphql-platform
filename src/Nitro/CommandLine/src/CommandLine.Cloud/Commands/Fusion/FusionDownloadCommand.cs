using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Fusion;

internal sealed class FusionDownloadCommand : Command
{
    public FusionDownloadCommand() : base("download")
    {
        Description = "Downloads the most recent gateway configuration";

        AddOption(Opt<StageNameOption>.Instance);
        AddOption(Opt<ApiIdOption>.Instance);
        AddOption(Opt<OptionalOutputFileOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<ISessionService>(),
            Bind.FromServiceProvider<IHttpClientFactory>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
        ISessionService sessionService,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        var stageName = context.ParseResult.GetValueForOption(Opt<StageNameOption>.Instance)!;
        var apiId = context.ParseResult.GetValueForOption(Opt<ApiIdOption>.Instance)!;
        var outputFile =
            context.ParseResult.GetValueForOption(Opt<OptionalOutputFileOption>.Instance) ??
            new FileInfo(Path.Combine(System.Environment.CurrentDirectory, "gateway.fgp"));

        console.Title($"Download the fusion configuration {apiId}/{stageName}");

        await using var stream = await FusionPublishHelpers.DownloadConfigurationAsync(
            apiId,
            stageName,
            client,
            httpClientFactory,
            cancellationToken);

        if (stream is null)
        {
            throw new ExitException("The api with the given id does not exist or does not have a download url.");
        }

        await using var fileStream = outputFile.OpenWrite();

        await stream.CopyToAsync(fileStream, cancellationToken);

        console.MarkupLine($"Downloaded fusion configuration to: {outputFile.FullName}");

        return ExitCodes.Success;
    }
}
