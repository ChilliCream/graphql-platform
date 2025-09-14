using HotChocolate.Types.Relay;

namespace HotChocolate.Execution.Options;

public struct NodeIdSerializerOptions
{
    public NodeIdSerializerOptions()
    {
    }

    public int MaxIdLength { get; set; } = 1024;
    public bool OutputNewIdFormat { get; set; } = true;
    public NodeIdSerializerFormat Format { get; set; } = NodeIdSerializerFormat.UrlSafeBase64;
    public int MaxCachedTypeNames { get; set; } = 1024;
}
