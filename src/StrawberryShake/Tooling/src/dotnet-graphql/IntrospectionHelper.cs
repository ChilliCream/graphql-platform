using HotChocolate.Utilities.Introspection;
using HCErrorBuilder = HotChocolate.ErrorBuilder;

namespace StrawberryShake.Tools;

public static class IntrospectionHelper
{
    public static async Task<bool> DownloadSchemaAsync(
        HttpClient client,
        IFileSystem fileSystem,
        IActivity activity,
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            await fileSystem.WriteToAsync(
                    fileName,
                    stream => IntrospectionClient.DownloadSchemaAsync(
                        client, stream, cancellationToken))
                .ConfigureAwait(false);
            return true;
        }
        catch (IntrospectionException ex)
        {
            activity.WriteError(
                HCErrorBuilder.New()
                    .SetMessage(ex.Message)
                    .SetCode("INTROSPECTION_ERROR")
                    .Build());
            return false;
        }
        catch (HttpRequestException ex)
        {
            activity.WriteError(
                HCErrorBuilder.New()
                    .SetMessage(ex.Message)
                    .SetCode("HTTP_ERROR")
                    .Build());
            return false;
        }
    }
}