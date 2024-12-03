#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.Descriptors.TypeReference;

#nullable enable

namespace HotChocolate.Types.Introspection;

/// <summary>
/// Directive arguments can have names and values.
/// The values are in graphql SDL syntax printed as a string.
/// This type is NOT specified by the graphql specification presently.
/// </summary>
[Introspection]
// ReSharper disable once InconsistentNaming
internal sealed class __DirectiveArgument : ObjectType<ArgumentNode>
{
    protected override ObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        var nonNullStringType = Parse($"{ScalarNames.String}!");

        return new ObjectTypeDefinition(
            Names.__DirectiveArgument,
            TypeResources.DirectiveArgument_Description,
            runtimeType: typeof(ArgumentNode))
        {
            Fields =
            {
                new(Names.Name, type: nonNullStringType, pureResolver: Resolvers.Name),
                new(Names.Value, type: nonNullStringType, pureResolver: Resolvers.Value),
            },
        };
    }

    private static class Resolvers
    {
        public static string Name(IResolverContext context)
            => context.Parent<ArgumentNode>().Name.Value;

        public static string Value(IResolverContext context)
            => context.Parent<ArgumentNode>().Value.Print(indented: false);
    }

    public static class Names
    {
        // ReSharper disable once InconsistentNaming
        public const string __DirectiveArgument = "__DirectiveArgument";
        public const string Name = "name";
        public const string Value = "value";
    }
}
#pragma warning restore IDE1006 // Naming Styles
