#pragma warning disable IDE1006 // Naming Styles
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
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
                    new(Names.Description, type: stringType, inlineResolver: Resolvers.Description),
                    new(Names.Types, Schema_Types, typeListType, inlineResolver: Resolvers.Types),
                    new(Names.QueryType,
                        Schema_QueryType,
                        nonNullTypeType,
                        inlineResolver: Resolvers.QueryType),
                    new(Names.MutationType,
                        Schema_MutationType,
                        typeType, inlineResolver:
                        Resolvers.MutationType),
                    new(Names.SubscriptionType,
                        Schema_SubscriptionType,
                        typeType,
                        inlineResolver: Resolvers.SubscriptionType),
                    new(Names.Directives,
                        Schema_Directives,
                        directiveListType,
                        inlineResolver: Resolvers.Directives),
                }
            };

            if (context.DescriptorContext.Options.EnableDirectiveIntrospection)
            {
                def.Fields.Add(new(Names.AppliedDirectives,
                    type: appDirectiveListType,
                    inlineResolver: Resolvers.AppliedDirectives));
            }

            return def;
        }

        private static class Resolvers
        {
            public static object? Description(object? parent)
                => ((ISchema)parent!).Description;

            public static object Types(object? parent)
                => ((ISchema)parent!).Types;

            public static object QueryType(object? parent)
                => ((ISchema)parent!).QueryType;

            public static object? MutationType(object? parent)
                => ((ISchema)parent!).MutationType;

            public static object? SubscriptionType(object? parent)
                => ((ISchema)parent!).SubscriptionType;

            public static object Directives(object? parent)
                => ((ISchema)parent!).DirectiveTypes;

            public static object AppliedDirectives(object? parent)
                => parent is IHasDirectives hasDirectives
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
