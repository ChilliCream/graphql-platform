namespace StrawberryShake.Tools;

public class DownloadCommandHandler : CommandHandler<DownloadCommandArguments>
{
    public DownloadCommandHandler(
        IFileSystem fileSystem,
        IHttpClientFactory httpClientFactory,
        IConsoleOutput output)
    {
        FileSystem = fileSystem;
        HttpClientFactory = httpClientFactory;
        Output = output;
    }

    public IFileSystem FileSystem { get; }

    public IHttpClientFactory HttpClientFactory { get; }

    public IConsoleOutput Output { get; }

    public override async Task<int> ExecuteAsync(
        DownloadCommandArguments arguments,
        CancellationToken cancellationToken)
    {
        using var command = Output.WriteCommand();

        var accessToken =
            await arguments.AuthArguments
                .RequestTokenAsync(Output, cancellationToken)
                .ConfigureAwait(false);

        var context = new DownloadCommandContext(
            new Uri(arguments.Uri.Value!),
            FileSystem.ResolvePath(arguments.FileName.Value()?.Trim(), "schema.graphql"),
            accessToken?.Token,
            accessToken?.Scheme,
            CustomHeaderHelper.ParseHeadersArgument(arguments.CustomHeaders.Values),
            arguments.TypeDepth.HasValue() &&
            int.TryParse(arguments.TypeDepth.Value(), out var typeDepth) &&
            typeDepth >= 3 ? typeDepth : 6);

        FileSystem.EnsureDirectoryExists(
            FileSystem.GetDirectoryName(context.FileName)!);

        return await DownloadSchemaAsync(
                context, cancellationToken)
            .ConfigureAwait(false)
            ? 0 : 1;
    }

    private async Task<bool> DownloadSchemaAsync(
        DownloadCommandContext context,
        CancellationToken cancellationToken)
    {
        using var activity = Output.WriteActivity("Download schema");

        var client = HttpClientFactory.Create(
            context.Uri, context.Token, context.Scheme, context.CustomHeaders);

        return await IntrospectionHelper.DownloadSchemaAsync(
                client, FileSystem, activity, context.FileName, context.TypeDepth,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
