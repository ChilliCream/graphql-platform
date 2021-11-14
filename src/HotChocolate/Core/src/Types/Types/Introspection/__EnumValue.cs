#pragma warning disable IDE1006 // Naming Styles
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Types.Descriptors.TypeReference;

#nullable enable

namespace HotChocolate.Types.Introspection;

[Introspection]
internal sealed class __EnumValue : ObjectType<IEnumValue>
{
    protected override ObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        SyntaxTypeReference stringType = Create(ScalarNames.String);
        SyntaxTypeReference nonNullStringType = Parse($"{ScalarNames.String}!");
        SyntaxTypeReference nonNullBooleanType = Parse($"{ScalarNames.Boolean}!");
        SyntaxTypeReference appDirectiveListType = Parse($"[{nameof(__AppliedDirective)}!]!");

        var def = new ObjectTypeDefinition(
            Names.__EnumValue,
            EnumValue_Description,
            typeof(IEnumValue))
        {
            Fields =
                {
                    new(Names.Name, type: nonNullStringType, pureResolver: Resolvers.Name),
                    new(Names.Description, type: stringType, pureResolver: Resolvers.Description),
                    new(Names.IsDeprecated, type: nonNullBooleanType,
                        pureResolver: Resolvers.IsDeprecated),
                    new(Names.DeprecationReason, type: stringType,
                        pureResolver: Resolvers.DeprecationReason),
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
        public static object Name(IPureResolverContext context)
            => context.Parent<IEnumValue>().Name.Value;

        public static object? Description(IPureResolverContext context)
            => context.Parent<IEnumValue>().Description;

        public static object IsDeprecated(IPureResolverContext context)
            => context.Parent<IEnumValue>().IsDeprecated;

        public static string? DeprecationReason(IPureResolverContext context)
            => context.Parent<IEnumValue>().DeprecationReason;

        public static object AppliedDirectives(IPureResolverContext context)
            => context.Parent<IEnumValue>().Directives
                .Where(t => t.Type.IsPublic)
                .Select(d => d.ToNode());
    }

    public static class Names
    {
        public const string __EnumValue = "__EnumValue";
        public const string Name = "name";
        public const string Description = "description";
        public const string IsDeprecated = "isDeprecated";
        public const string DeprecationReason = "deprecationReason";
        public const string AppliedDirectives = "appliedDirectives";
    }
}
#pragma warning restore IDE1006 // Naming Styles
