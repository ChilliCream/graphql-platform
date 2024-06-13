using HotChocolate.Features;
using HotChocolate.Utilities;

namespace HotChocolate.Skimmed;

public abstract class ComplexTypeDefinition(string name) : INamedTypeDefinition
{
    private string _name = name.EnsureGraphQLName();
    private List<InterfaceType>? _implements;
    private FeatureCollection? _features;
    private DirectiveCollection? _directives;

    public abstract TypeKind Kind { get; }

    public string Name
    {
        get => _name;
        set => _name = value.EnsureGraphQLName();
    }

    public string? Description { get; set; }

    public DirectiveCollection Directives => _directives ??= [];

    public IList<InterfaceType> Implements => _implements ??= [];

    public FieldCollection<OutputField> Fields { get; } = [];

    public IFeatureCollection Features => _features ??= new FeatureCollection();

    public bool Equals(ITypeDefinition? other) => Equals(other, TypeComparison.Reference);

    public abstract bool Equals(ITypeDefinition? other, TypeComparison comparison);
}
