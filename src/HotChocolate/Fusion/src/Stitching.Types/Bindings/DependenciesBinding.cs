using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Bindings;

internal readonly struct DependenciesBinding : IBinding
{
    public DependenciesBinding(SchemaCoordinate target, SelectionSetNode dependsOn)
    {
        Target = target;
        DependsOn = dependsOn;
    }

    public SchemaCoordinate Target { get; }

    public SelectionSetNode DependsOn { get; }
}
