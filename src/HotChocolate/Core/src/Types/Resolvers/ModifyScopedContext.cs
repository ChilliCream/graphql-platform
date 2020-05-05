using System.Collections.Immutable;

#nullable enable

namespace HotChocolate.Resolvers
{
    public delegate IImmutableDictionary<string, object?> ModifyScopedContext(
        IImmutableDictionary<string, object?> contextData);
}
