#nullable enable

using System.Collections.Immutable;

namespace HotChocolate.Configuration;

internal sealed class TypeSystemConventionFeature
{
    public ImmutableDictionary<ConventionKey, ImmutableList<ConventionRegistration>> Conventions { get; set; } =
        ImmutableDictionary<ConventionKey, ImmutableList<ConventionRegistration>>.Empty;
}
