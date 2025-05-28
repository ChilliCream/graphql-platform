#nullable enable

using HotChocolate.Resolvers;

namespace HotChocolate.Configuration;

internal sealed class ResolverFeature
{
    public object? RootInstance { get; set; }
    public List<FieldResolverConfiguration> FieldResolvers { get; } = [];
    public List<(string TypeName, Type ResolverType)> ResolverTypes { get; } = [];
}
