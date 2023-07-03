using HotChocolate.Fusion;
using HotChocolate.Language;
using static Microsoft.Extensions.DependencyInjection.GatewayConfigurationFileUtils;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class StaticGatewayConfigurationFileObserver : IObservable<GatewayConfiguration>
{
    private readonly string _filename;

    public StaticGatewayConfigurationFileObserver(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(filename));
        }

        _filename = filename;
    }

    public IDisposable Subscribe(IObserver<GatewayConfiguration> observer)
    {
        return new FileConfigurationSession(observer, _filename);
    }

    private class FileConfigurationSession : IDisposable
    {
        public FileConfigurationSession(IObserver<GatewayConfiguration> observer, string filename)
        {
            Task.Run(
                async () =>
                {
                    try
                    {
                        var document = await LoadDocumentAsync(filename, default);
                        observer.OnNext(new GatewayConfiguration(document));
                    }
                    catch(Exception ex)
                    {
                        observer.OnError(ex);
                        observer.OnCompleted();
                    }
                });
        }

        public void Dispose()
        {
            // there is nothing to dispose here.
        }
    }
}

internal static class GatewayConfigurationFileUtils
{
    public static async ValueTask<DocumentNode> LoadDocumentAsync(
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            // We first try to load the file name as a fusion graph package.
            // This might fails as a the file that was provided is a fusion
            // graph document.
            await using var package = FusionGraphPackage.Open(fileName, FileAccess.Read);
            return await package.GetFusionGraphAsync(cancellationToken);
        }
        catch
        {
            // If we fail to load the file as a fusion graph package we will
            // try to load it as a GraphQL schema document.
            var sourceText = await File.ReadAllTextAsync(fileName, cancellationToken);
            return Utf8GraphQLParser.Parse(sourceText);
        }
    }
}
