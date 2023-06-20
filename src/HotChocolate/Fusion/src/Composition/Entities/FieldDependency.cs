using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

internal sealed class FieldDependency
{
    public FieldDependency(int id, string subgraphName)
    {
        Id = id;
        SubgraphName = subgraphName;
    }

    public int Id { get; }

    public string SubgraphName { get; }

    public Dictionary<string, MemberReference> Arguments { get; } = new();
}

internal sealed record MemberReference(IsDirective Reference, InputField Argument)
{
    public bool IsRequired => Argument.Type.Kind is TypeKind.NonNull;
}

