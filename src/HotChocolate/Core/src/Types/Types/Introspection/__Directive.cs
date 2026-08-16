#pragma warning disable IDE1006 // Naming Styles
using System.Runtime.CompilerServices;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Types.Descriptors.TypeReference;

namespace HotChocolate.Types.Introspection;

[Introspection]
// ReSharper disable once InconsistentNaming
internal sealed class __Directive : ObjectType<DirectiveType>
{
    protected override ObjectTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
    {
        var stringType = Create(ScalarNames.String);
        var nonNullStringType = Parse($"{ScalarNames.String}!");
        var nonNullStringListType = Parse($"[{ScalarNames.String}!]");
        var nonNullBooleanType = Parse($"{ScalarNames.Boolean}!");
        var argumentListType = Parse($"[{nameof(__InputValue)}!]!");
        var locationListType = Parse($"[{nameof(__DirectiveLocation)}!]!");
        var directiveListType = Parse($"[{nameof(__AppliedDirective)}!]!");

        var optInFeaturesEnabled = context.DescriptorContext.Options.EnableOptInFeatures;

        var def = new ObjectTypeConfiguration(
            Names.__Directive,
            TypeResources.Directive_Description,
            typeof(DirectiveType))
        {
            Fields =
            {
                new(Names.Name, type: nonNullStringType, pureResolver: Resolvers.Name),
                new(Names.Description, type: stringType, pureResolver: Resolvers.Description),
                new(Names.Locations, type: locationListType, pureResolver: Resolvers.Locations),
                new(
                    Names.Args,
                    type: argumentListType,
                    pureResolver: optInFeaturesEnabled
                        ? Resolvers.ArgumentsWithOptIn
                        : Resolvers.Arguments)
                {
                    Arguments =
                    {
                        new(Names.IncludeDeprecated, type: nonNullBooleanType)
                        {
                            DefaultValue = BooleanValueNode.False,
                            RuntimeDefaultValue = false
                        }
                    }
                },
                new(Names.IsRepeatable,
                    type: nonNullBooleanType,
                    pureResolver: Resolvers.IsRepeatable),
                new(Names.IsDeprecated,
                    type: nonNullBooleanType,
                    pureResolver: Resolvers.IsDeprecated),
                new(Names.DeprecationReason,
                    type: stringType,
                    pureResolver: Resolvers.DeprecationReason),
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
                }
            }
        };

        if (context.DescriptorContext.Options.EnableDirectiveIntrospection)
        {
            def.Fields.Add(new(Names.AppliedDirectives,
                type: directiveListType,
                pureResolver: Resolvers.AppliedDirectives));
        }

        if (optInFeaturesEnabled)
        {
            def.Fields.Single(f => f.Name == Names.Args)
                .Arguments
                .Add(new(Names.IncludeOptIn, type: nonNullStringListType));

            def.Fields.Add(new(Names.RequiresOptIn,
                type: nonNullStringListType,
                pureResolver: Resolvers.RequiresOptIn));
        }

        return def;
    }

    private static class Resolvers
    {
        public static string Name(IResolverContext context)
            => context.Parent<DirectiveType>().Name;

        public static object? Description(IResolverContext context)
            => context.Parent<DirectiveType>().Description;

        public static object IsRepeatable(IResolverContext context)
            => context.Parent<DirectiveType>().IsRepeatable;

        public static object IsDeprecated(IResolverContext context)
            => context.Parent<DirectiveType>().IsDeprecated;

        public static object? DeprecationReason(IResolverContext context)
            => context.Parent<DirectiveType>().DeprecationReason;

        public static object Locations(IResolverContext context)
        {
            var locations = context.Parent<DirectiveType>().Locations;
            return DirectiveLocationUtils.AsEnumerable(locations);
        }

        public static IEnumerable<IInputValueDefinition> ArgumentsWithOptIn(IResolverContext context)
        {
            var includeOptIn = context.ArgumentValue<string[]?>(Names.IncludeOptIn) ?? [];

            return Arguments(context).Where(
                a => OptInIntrospectionHelper.IsIncluded(a.Directives, includeOptIn));
        }

        public static IEnumerable<IInputValueDefinition> Arguments(IResolverContext context)
        {
            var directive = context.Parent<DirectiveType>();
            return context.ArgumentValue<bool>(Names.IncludeDeprecated)
                ? directive.Arguments
                : directive.Arguments.Where(t => !t.IsDeprecated);
        }

        public static object AppliedDirectives(IResolverContext context) =>
            ((IDirectivesProvider)context.Parent<DirectiveType>())
                .Directives
                .Where(t => Unsafe.As<DirectiveType>(t.Definition).IsPublic)
                .Select(d => d.ToSyntaxNode());

        public static object RequiresOptIn(IResolverContext context) =>
            ((IDirectivesProvider)context.Parent<DirectiveType>())
                .Directives
                .Where(t => t.Definition is RequiresOptInDirectiveType)
                .Select(d => d.ToValue<RequiresOptIn>().Feature);

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
        public const string IsDeprecated = "isDeprecated";
        public const string DeprecationReason = "deprecationReason";
        public const string IncludeDeprecated = "includeDeprecated";
        public const string IncludeOptIn = "includeOptIn";
        public const string AppliedDirectives = "appliedDirectives";
        public const string RequiresOptIn = "requiresOptIn";
        public const string Locations = "locations";
        public const string Args = "args";
        public const string OnOperation = "onOperation";
        public const string OnFragment = "onFragment";
        public const string OnField = "onField";
    }
}
#pragma warning restore IDE1006 // Naming Styles
