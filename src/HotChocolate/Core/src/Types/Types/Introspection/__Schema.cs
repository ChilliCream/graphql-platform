#pragma warning disable IDE1006 // Naming Styles
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Types.Descriptors.TypeReference;

#nullable enable

namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __Schema : ObjectType
    {
        protected override ObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
        {
            SyntaxTypeReference stringType = Create(ScalarNames.String);
            SyntaxTypeReference typeListType = Parse($"[{nameof(__Type)}!]!");
            SyntaxTypeReference typeType = Create(nameof(__Type));
            SyntaxTypeReference nonNullTypeType = Parse($"{nameof(__Type)}!");
            SyntaxTypeReference directiveListType = Parse($"[{nameof(__Directive)}!]!");
            SyntaxTypeReference appDirectiveListType = Parse($"[{nameof(__AppliedDirective)}!]!");

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
                }
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
            public static object? Description(IPureResolverContext context)
                => context.Parent<ISchema>().Description;

            public static object Types(IPureResolverContext context)
                => context.Parent<ISchema>().Types;

            public static object QueryType(IPureResolverContext context)
                => context.Parent<ISchema>().QueryType;

            public static object? MutationType(IPureResolverContext context)
                => context.Parent<ISchema>().MutationType;

            public static object? SubscriptionType(IPureResolverContext context)
                => context.Parent<ISchema>().SubscriptionType;

            public static object Directives(IPureResolverContext context)
                => context.Parent<ISchema>().DirectiveTypes;

            public static object AppliedDirectives(IPureResolverContext context)
                => context.Parent<IHasDirectives>().Directives
                    .Where(t => t.Type.IsPublic)
                    .Select(d => d.ToNode());
        }

        public static class Names
        {
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
}
#pragma warning restore IDE1006 // Naming Styles
