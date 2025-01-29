#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.Descriptors.TypeReference;

#nullable enable

namespace HotChocolate.Types.Introspection;

/// <summary>
/// An OptInFeatureStability object describes the stability level of an opt-in feature.
/// </summary>
[Introspection]
// ReSharper disable once InconsistentNaming
internal sealed class __OptInFeatureStability : ObjectType<DirectiveNode>
{
    protected override ObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        var nonNullStringType = Parse($"{ScalarNames.String}!");

        return new ObjectTypeDefinition(
            Names.__OptInFeatureStability,
            TypeResources.OptInFeatureStability_Description,
            typeof(DirectiveNode))
        {
            Fields =
            {
                new(Names.Feature, type: nonNullStringType, pureResolver: Resolvers.Feature),
                new(Names.Stability, type: nonNullStringType, pureResolver: Resolvers.Stability)
            }
        };
    }

    private static class Resolvers
    {
        public static string Feature(IResolverContext context) =>
            ((StringValueNode)context.Parent<DirectiveNode>()
                .Arguments
                .Single(a => a.Name.Value == Names.Feature).Value).Value;

        public static string Stability(IResolverContext context) =>
            ((StringValueNode)context.Parent<DirectiveNode>()
                .Arguments
                .Single(a => a.Name.Value == Names.Stability).Value).Value;
    }

    public static class Names
    {
        // ReSharper disable once InconsistentNaming
        public const string __OptInFeatureStability = "__OptInFeatureStability";
        public const string Feature = "feature";
        public const string Stability = "stability";
    }
}
#pragma warning restore IDE1006 // Naming Styles
