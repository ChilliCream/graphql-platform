#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Types.Descriptors.TypeReference;

namespace HotChocolate.Types.Introspection;

/// <summary>
/// An OptInFeatureStability object describes the stability level of an opt-in feature.
/// </summary>
[Introspection]
// ReSharper disable once InconsistentNaming
internal sealed class __OptInFeatureStability : ObjectType<DirectiveNode>
{
    protected override ObjectTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
    {
        var nonNullStringType = Parse($"{ScalarNames.String}!");

        return new ObjectTypeConfiguration(
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
        public static string Feature(IResolverContext context)
        {
            var featureArg = context.Parent<DirectiveNode>()
                .Arguments
                .FirstOrDefault(a => a.Name.Value == Names.Feature);

            return featureArg?.Value is StringValueNode stringValue
                ? stringValue.Value
                : throw new InvalidOperationException("Feature argument is missing or has an invalid value.");
        }

        public static string Stability(IResolverContext context)
        {
            var stabilityArg = context.Parent<DirectiveNode>()
                .Arguments
                .FirstOrDefault(a => a.Name.Value == Names.Stability);

            return stabilityArg?.Value is StringValueNode stringValue
                ? stringValue.Value
                : throw new InvalidOperationException("Stability argument is missing or has an invalid value.");
        }
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
