#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.Descriptors.TypeReference;

#nullable enable

namespace HotChocolate.Types.Introspection;

[Introspection]
// ReSharper disable once InconsistentNaming
internal sealed class __Directive : ObjectType<DirectiveType>
{
    protected override ObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        var stringType = Create(ScalarNames.String);
        var nonNullStringType = Parse($"{ScalarNames.String}!");
        var nonNullBooleanType = Parse($"{ScalarNames.Boolean}!");
        var booleanType = Parse($"{ScalarNames.Boolean}");
        var argumentListType = Parse($"[{nameof(__InputValue)}!]!");
        var locationListType = Parse($"[{nameof(__DirectiveLocation)}!]!");

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
                new(Names.Args, type: argumentListType, pureResolver: Resolvers.Arguments)
                {
                    Arguments =
                    {
                        new(Names.IncludeDeprecated, type: booleanType)
                        {
                            DefaultValue = BooleanValueNode.False,
                            RuntimeDefaultValue = false,
                        },
                    },
                },
                new(Names.IsRepeatable,
                    type: nonNullBooleanType,
                    pureResolver: Resolvers.IsRepeatable),
                new(Names.OnOperation,
                    type: nonNullBooleanType,
                    pureResolver: Resolvers.OnOperation)
                {
                    DeprecationReason = TypeResources.Directive_UseLocation,
                },
                new(Names.OnFragment,
                    type: nonNullBooleanType,
                    pureResolver: Resolvers.OnFragment)
                {
                    DeprecationReason = TypeResources.Directive_UseLocation,
                },
                new(Names.OnField,
                    type: nonNullBooleanType,
                    pureResolver: Resolvers.OnField)
                {
                    DeprecationReason = TypeResources.Directive_UseLocation,
                },
            },
        };
    }

    private static class Resolvers
    {
        public static string Name(IResolverContext context)
            => context.Parent<DirectiveType>().Name;

        public static object? Description(IResolverContext context)
            => context.Parent<DirectiveType>().Description;

        public static object IsRepeatable(IResolverContext context)
            => context.Parent<DirectiveType>().IsRepeatable;

        public static object Locations(IResolverContext context)
        {
            var locations = context.Parent<DirectiveType>().Locations;
            return locations.AsEnumerable();
        }

        public static object Arguments(IResolverContext context)
        {
            var directive = context.Parent<DirectiveType>();
            return context.ArgumentValue<bool>(Names.IncludeDeprecated)
                ? directive.Arguments
                : directive.Arguments.Where(t => !t.IsDeprecated);
        }

        public static object OnOperation(IResolverContext context)
        {
            var locations = context.Parent<DirectiveType>().Locations;
            return (locations & DirectiveLocation.Operation) != 0;
        }

        public static object OnFragment(IResolverContext context)
        {
            var locations = context.Parent<DirectiveType>().Locations;
            return (locations & DirectiveLocation.Fragment) != 0;
        }

        public static object OnField(IResolverContext context)
        {
            var locations = context.Parent<DirectiveType>().Locations;
            return (locations & DirectiveLocation.Field) != 0;
        }
    }

    public static class Names
    {
        // ReSharper disable once InconsistentNaming
        public const string __Directive = "__Directive";
        public const string Name = "name";
        public const string Description = "description";
        public const string IsRepeatable = "isRepeatable";
        public const string IncludeDeprecated = "includeDeprecated";
        public const string Locations = "locations";
        public const string Args = "args";
        public const string OnOperation = "onOperation";
        public const string OnFragment = "onFragment";
        public const string OnField = "onField";
    }
}
#pragma warning restore IDE1006 // Naming Styles
