using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

internal sealed class FieldDependency
{
    public FieldDependency(string subgraphName)
    {
        SubgraphName = subgraphName;
    }

    public string SubgraphName { get; }

    public Dictionary<string, MemberReference> Arguments { get; } = new();
}

internal sealed record MemberReference(IsDirective Reference, InputField Argument)
{
    public bool IsRequired => Argument.Type.Kind is TypeKind.NonNull;
}
