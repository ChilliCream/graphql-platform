#nullable enable

using System.Collections.Immutable;

namespace HotChocolate.Configuration;

internal sealed class TypeSystemConventionFeature
{
    public ImmutableDictionary<ConventionKey, ImmutableList<ConventionRegistration>> Conventions { get; set; } =
#if NET10_0_OR_GREATER
        [];
#else
        ImmutableDictionary<ConventionKey, ImmutableList<ConventionRegistration>>.Empty;
#endif
}
