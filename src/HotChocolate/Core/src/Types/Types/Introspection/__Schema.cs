#pragma warning disable IDE1006 // Naming Styles
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
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

            var def = new ObjectTypeDefinition(Names.__Schema, TypeResources.Schema_Description)
            {
                Name = Names.__Schema,
                Description = TypeResources.Schema_Description,
                RuntimeType = typeof(ISchema),
                Fields =
                {
                    new(Names.Description,
                        type: stringType,
                        pureResolver: Resolvers.Description,
                        inlineResolver: Resolvers.InlineDescription),
                    new(Names.Types,
                        description: TypeResources.Schema_Types,
                        type: typeListType,
                        pureResolver: Resolvers.Types,
                        inlineResolver: Resolvers.InlineTypes),
                    new(Names.QueryType,
                        description: TypeResources.Schema_QueryType,
                        type: nonNullTypeType,
                        pureResolver: Resolvers.QueryType,
                        inlineResolver: Resolvers.InlineQueryType),
                    new(Names.MutationType,
                        description: TypeResources.Schema_MutationType,
                        type: typeType,
                        pureResolver: Resolvers.MutationType,
                        inlineResolver: Resolvers.InlineMutationType),
                    new(Names.SubscriptionType,
                        description: TypeResources.Schema_SubscriptionType,
                        type: typeType,
                        pureResolver: Resolvers.SubscriptionType,
                        inlineResolver: Resolvers.InlineSubscriptionType),
                    new(Names.Directives,
                        description: TypeResources.Schema_Directives,
                        type: directiveListType,
                        pureResolver: Resolvers.Directives,
                        inlineResolver: Resolvers.InlineDirectives),
                }
            };

            if (context.DescriptorContext.Options.EnableDirectiveIntrospection)
            {
                def.Fields.Add(new(Names.AppliedDirectives,
                    type: appDirectiveListType,
                    pureResolver: Resolvers.AppliedDirectives));
            }

            return def;
        }

        private static class Resolvers
        {
            public static object? Description(IResolverContext context)
                => context.Parent<ISchema>().Description;

            public static object? InlineDescription(object? parent)
                => ((ISchema)parent!).Description;

            public static object Types(IResolverContext context)
                => context.Parent<ISchema>().Types;

            public static object InlineTypes(object? parent)
                => ((ISchema)parent!).Types;

            public static object QueryType(IResolverContext context)
                => context.Parent<ISchema>().QueryType;

            public static object InlineQueryType(object? parent)
                => ((ISchema)parent!).QueryType;

            public static object? MutationType(IResolverContext context)
                => context.Parent<ISchema>().MutationType;

            public static object? InlineMutationType(object? parent)
                => ((ISchema)parent!).MutationType;

            public static object? SubscriptionType(IResolverContext context)
                => context.Parent<ISchema>().SubscriptionType;

            public static object? InlineSubscriptionType(object? parent)
                => ((ISchema)parent!).SubscriptionType;

            public static object? Directives(IResolverContext context)
                => context.Parent<ISchema>().DirectiveTypes;

            public static object? InlineDirectives(object? parent)
                => ((ISchema)parent!).DirectiveTypes;

            public static object AppliedDirectives(IResolverContext context)
                => GetAppliedDirectives(context.Parent<ISchema>());

            private static IEnumerable<DirectiveNode> GetAppliedDirectives(ISchema schema)
                => schema is IHasDirectives hasDirectives
                    ? hasDirectives.Directives.Where(t => t.Type.IsPublic).Select(d => d.ToNode())
                    : Enumerable.Empty<DirectiveNode>();
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
