#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Types.Descriptors.TypeReference;

#nullable enable

namespace HotChocolate.Types.Introspection;

[Introspection]
// ReSharper disable once InconsistentNaming
internal sealed class __Schema : ObjectType
{
    protected override ObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        var stringType = Create(ScalarNames.String);
        var typeListType = Parse($"[{nameof(__Type)}!]!");
        var typeType = Create(nameof(__Type));
        var nonNullTypeType = Parse($"{nameof(__Type)}!");
        var directiveListType = Parse($"[{nameof(__Directive)}!]!");
        var appDirectiveListType = Parse($"[{nameof(__AppliedDirective)}!]!");

        var def = new ObjectTypeDefinition(Names.__Schema, Schema_Description, typeof(ISchema))
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
                        pureResolver: Resolvers.Directives),
                },
        };

        if (context.DescriptorContext.Options.EnableDirectiveIntrospection)
        {
            def.Fields.Add(new(
                Names.AppliedDirectives,
                type: appDirectiveListType,
                pureResolver: Resolvers.AppliedDirectives));
        }

        return def;
    }

    private static class Resolvers
    {
        public static object? Description(IResolverContext context)
            => context.Parent<ISchema>().Description;

        public static object Types(IResolverContext context)
            => context.Parent<ISchema>().Types;

        public static object QueryType(IResolverContext context)
            => context.Parent<ISchema>().QueryType;

        public static object? MutationType(IResolverContext context)
            => context.Parent<ISchema>().MutationType;

        public static object? SubscriptionType(IResolverContext context)
            => context.Parent<ISchema>().SubscriptionType;

        public static object Directives(IResolverContext context)
            => context.Parent<ISchema>().DirectiveTypes.Where(t => t.IsPublic);

        public static object AppliedDirectives(IResolverContext context)
            => context.Parent<ISchema>().Directives
                .Where(t => t.Type.IsPublic)
                .Select(d => d.AsSyntaxNode());
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
    }
}
#pragma warning restore IDE1006 // Naming Styles
