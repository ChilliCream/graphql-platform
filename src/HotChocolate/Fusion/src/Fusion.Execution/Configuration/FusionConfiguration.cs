using HotChocolate.Buffers;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Configuration;

/// <summary>
/// The fusion configuration consists of the fusion execution schema document and
/// the fusion execution schema settings.
/// </summary>
/// <param name="Schema">
/// The fusion execution schema document.
/// </param>
/// <param name="Settings">
/// The fusion execution schema settings.
/// </param>
public sealed record FusionConfiguration(
    DocumentNode Schema,
    JsonDocumentOwner Settings)
    : IDisposable
{
    public void Dispose() => Settings.Dispose();
}
