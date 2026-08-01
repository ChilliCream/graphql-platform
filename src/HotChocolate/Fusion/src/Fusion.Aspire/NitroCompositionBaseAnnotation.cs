using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire;

internal sealed class NitroCompositionBaseAnnotation : IResourceAnnotation
{
    public required NitroStageResource Stage { get; init; }
}
