using static HotChocolate.Fetching.Properties.FetchingResources;

namespace HotChocolate.Fetching.Utilities;

internal static class ThrowHelper
{
    public static SchemaException DataLoader_InvalidType(
        Type dataLoaderType) =>
        new(SchemaErrorBuilder.New()
            .SetMessage(
                ThrowHelper_DataLoader_InvalidType,
                dataLoaderType.FullName ?? dataLoaderType.Name)
                .Build());
}
