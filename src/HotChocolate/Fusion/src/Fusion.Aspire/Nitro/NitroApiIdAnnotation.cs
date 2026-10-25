using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// The id of the Nitro api that carries the fusion configuration of a resource.
/// </summary>
internal sealed class NitroApiIdAnnotation : IResourceAnnotation
{
    public required string ApiId { get; init; }
}
