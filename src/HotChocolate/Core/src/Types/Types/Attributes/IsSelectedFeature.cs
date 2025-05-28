#nullable enable
namespace HotChocolate.Types;

internal sealed class IsSelectedFeature
{
    public List<IsSelectedPattern> Patterns { get; } = [];
}
