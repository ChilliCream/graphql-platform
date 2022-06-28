using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors;

public sealed class NameDirectiveReference : IDirectiveReference
{
    public NameDirectiveReference(string name)
    {
        Name = name.EnsureGraphQLName();
    }

    public string Name { get; }
}
