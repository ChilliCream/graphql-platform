using System.CommandLine.Invocation;
using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Option;
using ChilliCream.Nitro.CLI.Option.Binders;
using StrawberryShake;

namespace ChilliCream.Nitro.CLI.Commands.FusionConfiguration;

internal sealed class FusionConfigurationDownloadCommand : Command
{
    public FusionConfigurationDownloadCommand() : base("download")
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
        IHttpClientFactory clientFactory,
        CancellationToken cancellationToken)
    {
        var stageName = context.ParseResult.GetValueForOption(Opt<StageNameOption>.Instance)!;
        var apiId = context.ParseResult.GetValueForOption(Opt<ApiIdOption>.Instance)!;
        var outputFile =
            context.ParseResult.GetValueForOption(Opt<OptionalOutputFileOption>.Instance) ??
            new FileInfo(Path.Combine(System.Environment.CurrentDirectory, "gateway.fgp"));

        console.Title($"Download the fusion configuration {apiId}/{stageName}");

        var result =
            await client.FetchConfiguration.ExecuteAsync(apiId, stageName, cancellationToken);

        result.EnsureNoErrors();

        var downloadUrl = result.Data?.FusionConfigurationByApiId?.DownloadUrl ??
            throw new ExitException(
                "The api with the given id does not exist or does not have a download url.");

        var httpClient = clientFactory.CreateClient(ApiClient.ClientName);
        var downloadResult = await httpClient.GetAsync(downloadUrl, cancellationToken);

        downloadResult.EnsureSuccessStatusCode();

        await File.WriteAllBytesAsync(
            outputFile.FullName,
            await downloadResult.Content.ReadAsByteArrayAsync(cancellationToken),
            cancellationToken);

        console.MarkupLine($"Downloaded fusion configuration to: {outputFile.FullName}");

        return ExitCodes.Success;
    }
}
