#pragma warning disable IDE1006 // Naming Styles
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.Descriptors.TypeReference;

#nullable enable

namespace HotChocolate.Types.Introspection;

[Introspection]
internal sealed class __Directive : ObjectType<DirectiveType>
{
    protected override ObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        SyntaxTypeReference stringType = Create(ScalarNames.String);
        SyntaxTypeReference nonNullStringType = Parse($"{ScalarNames.String}!");
        SyntaxTypeReference nonNullBooleanType = Parse($"{ScalarNames.Boolean}!");
        SyntaxTypeReference argumentListType = Parse($"[{nameof(__InputValue)}!]!");
        SyntaxTypeReference locationListType = Parse($"[{nameof(__DirectiveLocation)}!]!");

        return new ObjectTypeDefinition(
            Names.__Directive,
            TypeResources.Directive_Description,
            typeof(DirectiveType))
        {
            Fields =
                {
                    new(Names.Name, type: nonNullStringType, pureResolver: Resolvers.Name),
                    new(Names.Description, type: stringType, pureResolver: Resolvers.Description),
                    new(Names.Locations, type: locationListType, pureResolver: Resolvers.Locations),
                    new(Names.Args, type: argumentListType, pureResolver: Resolvers.Arguments),
                    new(Names.IsRepeatable,
                        type: nonNullBooleanType,
                        pureResolver: Resolvers.IsRepeatable),
                    new(Names.OnOperation,
                        type: nonNullBooleanType,
                        pureResolver: Resolvers.OnOperation)
                    {
                        DeprecationReason = TypeResources.Directive_UseLocation
                    },
                    new(Names.OnFragment,
                        type: nonNullBooleanType,
                        pureResolver: Resolvers.OnFragment)
                    {
                        DeprecationReason = TypeResources.Directive_UseLocation
                    },
                    new(Names.OnField,
                        type: nonNullBooleanType,
                        pureResolver: Resolvers.OnField)
                    {
                        DeprecationReason = TypeResources.Directive_UseLocation
                    },
                }
        };
    }

    private static class Resolvers
    {
        public static string Name(IPureResolverContext context)
            => context.Parent<DirectiveType>().Name;

        public static object? Description(IPureResolverContext context)
            => context.Parent<DirectiveType>().Description;

        public static object IsRepeatable(IPureResolverContext context)
            => context.Parent<DirectiveType>().IsRepeatable;

        public static object Locations(IPureResolverContext context)
            => context.Parent<DirectiveType>().Locations;

        public static object Arguments(IPureResolverContext context)
            => context.Parent<DirectiveType>().Arguments;

        public static object OnOperation(IPureResolverContext context)
        {
            ICollection<DirectiveLocation> locations =
                context.Parent<DirectiveType>().Locations;

            return locations.Contains(DirectiveLocation.Query)
                   || locations.Contains(DirectiveLocation.Mutation)
                   || locations.Contains(DirectiveLocation.Subscription);
        }

        public static object OnFragment(IPureResolverContext context)
        {
            ICollection<DirectiveLocation> locations =
                context.Parent<DirectiveType>().Locations;

            return locations.Contains(DirectiveLocation.InlineFragment)
                   || locations.Contains(DirectiveLocation.FragmentSpread)
                   || locations.Contains(DirectiveLocation.FragmentDefinition);
        }

        public static object OnField(IPureResolverContext context)
        {
            ICollection<DirectiveLocation> locations =
                context.Parent<DirectiveType>().Locations;

            return locations.Contains(DirectiveLocation.Field);
        }
    }

    public static class Names
    {
        public const string __Directive = "__Directive";
        public const string Name = "name";
        public const string Description = "description";
        public const string IsRepeatable = "isRepeatable";
        public const string Locations = "locations";
        public const string Args = "args";
        public const string OnOperation = "onOperation";
        public const string OnFragment = "onFragment";
        public const string OnField = "onField";
    }
}
#pragma warning restore IDE1006 // Naming Styles
