using Microsoft.IO;

namespace HotChocolate.Transport.Http;

internal static class ClientStreamManager
{
    private static readonly RecyclableMemoryStreamManager s_streamManager = new();

    public static RecyclableMemoryStream Rent()
        => s_streamManager.GetStream();
}
