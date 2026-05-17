using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire;

internal sealed class NitroConfigurationAnnotation : IResourceAnnotation
{
    public required string ApiId { get; init; }

    public required string Stage { get; init; }

    public bool AlwaysDownload { get; init; }
}
