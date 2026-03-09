#pragma warning disable IDE1006 // Naming Styles
using System.Runtime.CompilerServices;
using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Interceptors;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Types.Descriptors.TypeReference;

namespace HotChocolate.Types.Introspection;

[Introspection]
// ReSharper disable once InconsistentNaming
internal sealed class __Schema : ObjectType
{
    protected override ObjectTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
    {
        var stringType = Create(ScalarNames.String);
        var typeListType = Parse($"[{nameof(__Type)}!]!");
        var typeType = Create(nameof(__Type));
        var nonNullTypeType = Parse($"{nameof(__Type)}!");
        var directiveListType = Parse($"[{nameof(__Directive)}!]!");
        var appDirectiveListType = Parse($"[{nameof(__AppliedDirective)}!]!");
        var nonNullStringListType = Parse($"[{ScalarNames.String}!]");
        var optInFeatureStabilityListType = Parse($"[{nameof(__OptInFeatureStability)}!]!");

        var def = new ObjectTypeConfiguration(Names.__Schema, Schema_Description, typeof(ISchemaDefinition))
        {
            Fields =
                {
                    new(Names.Description, type: stringType, pureResolver: Resolvers.Description),
                    new(Names.Types, Schema_Types, typeListType, pureResolver: Resolvers.Types),
                    new(Names.QueryType,
                        Schema_QueryType,
                        nonNullTypeType,
                        pureResolver: Resolvers.QueryType),
                    new(Names.MutationType,
                        Schema_MutationType,
                        typeType,
                        pureResolver: Resolvers.MutationType),
                    new(Names.SubscriptionType,
                        Schema_SubscriptionType,
                        typeType,
                        pureResolver: Resolvers.SubscriptionType),
                    new(Names.Directives,
                        Schema_Directives,
                        directiveListType,
                        pureResolver: Resolvers.Directives)
                }
        };

        if (context.DescriptorContext.Options.EnableDirectiveIntrospection)
        {
            def.Fields.Add(new(
                Names.AppliedDirectives,
                type: appDirectiveListType,
                pureResolver: Resolvers.AppliedDirectives));
        }

        if (context.DescriptorContext.Options.EnableOptInFeatures)
        {
            def.Fields.Add(new(
                Names.OptInFeatures,
                type: nonNullStringListType,
                pureResolver: Resolvers.OptInFeatures));

            def.Fields.Add(new(
                Names.OptInFeatureStability,
                type: optInFeatureStabilityListType,
                pureResolver: Resolvers.OptInFeatureStability));
        }

        return def;
    }

    private static class Resolvers
    {
        public static object? Description(IResolverContext context)
            => context.Parent<ISchemaDefinition>().Description;

        public static object Types(IResolverContext context)
            => context.Parent<ISchemaDefinition>().Types;

        public static object QueryType(IResolverContext context)
            => context.Parent<ISchemaDefinition>().QueryType;

        public static object? MutationType(IResolverContext context)
            => context.Parent<ISchemaDefinition>().MutationType;

        public static object? SubscriptionType(IResolverContext context)
            => context.Parent<ISchemaDefinition>().SubscriptionType;

        public static object Directives(IResolverContext context)
            => context.Parent<ISchemaDefinition>()
                .DirectiveDefinitions
                .Where(t => Unsafe.As<DirectiveType>(t).IsPublic);

        public static object AppliedDirectives(IResolverContext context)
            => context.Parent<ISchemaDefinition>().Directives
                .Where(t => Unsafe.As<DirectiveType>(t).IsPublic)
                .Select(d => d.ToSyntaxNode());

        public static object OptInFeatures(IResolverContext context)
            => context.Parent<ISchemaDefinition>().Features.GetRequired<OptInFeatures>();

        public static object OptInFeatureStability(IResolverContext context)
            => context.Parent<ISchemaDefinition>().Directives
                .Where(t => t.Definition is OptInFeatureStabilityDirectiveType)
                .Select(d => d.ToSyntaxNode());
    }

    public static class Names
    {
        // ReSharper disable once InconsistentNaming
        public const string __Schema = "__Schema";
        public const string Description = "description";
        public const string Types = "types";
        public const string QueryType = "queryType";
        public const string MutationType = "mutationType";
        public const string SubscriptionType = "subscriptionType";
        public const string Directives = "directives";
        public const string AppliedDirectives = "appliedDirectives";
        public const string OptInFeatures = "optInFeatures";
        public const string OptInFeatureStability = "optInFeatureStability";
    }
}
#pragma warning restore IDE1006 // Naming Styles
