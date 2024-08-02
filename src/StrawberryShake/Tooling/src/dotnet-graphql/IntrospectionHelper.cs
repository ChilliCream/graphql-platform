using HotChocolate.Utilities.Introspection;
using static HotChocolate.Utilities.Introspection.IntrospectionClient;
using HCErrorBuilder = HotChocolate.ErrorBuilder;

namespace StrawberryShake.Tools;

public static class IntrospectionHelper
{
    public static async Task<bool> DownloadSchemaAsync(
        HttpClient client,
        IFileSystem fileSystem,
        IActivity activity,
        string fileName,
        int typeDepth,
        CancellationToken cancellationToken)
    {
        try
        {
            var options = new IntrospectionOptions { TypeDepth = typeDepth, };
            var document = await IntrospectServerAsync(client, options, cancellationToken).ConfigureAwait(false);
            await fileSystem.WriteTextAsync(fileName, document.ToString(true)).ConfigureAwait(false);
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
