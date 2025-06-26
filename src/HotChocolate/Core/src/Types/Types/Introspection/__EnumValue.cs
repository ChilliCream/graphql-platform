#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Types.Descriptors.TypeReference;

#nullable enable

namespace HotChocolate.Types.Introspection;

[Introspection]
// ReSharper disable once InconsistentNaming
internal sealed class __EnumValue : ObjectType<EnumValue>
{
    protected override ObjectTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
    {
        var stringType = Create(ScalarNames.String);
        var nonNullStringType = Parse($"{ScalarNames.String}!");
        var nonNullStringListType = Parse($"[{ScalarNames.String}!]");
        var nonNullBooleanType = Parse($"{ScalarNames.Boolean}!");
        var appDirectiveListType = Parse($"[{nameof(__AppliedDirective)}!]!");

        var def = new ObjectTypeConfiguration(
            Names.__EnumValue,
            EnumValue_Description,
            typeof(EnumValue))
        {
            Fields =
                {
                    new(Names.Name, type: nonNullStringType, pureResolver: Resolvers.Name),
                    new(Names.Description, type: stringType, pureResolver: Resolvers.Description),
                    new(Names.IsDeprecated, type: nonNullBooleanType,
                        pureResolver: Resolvers.IsDeprecated),
                    new(Names.DeprecationReason, type: stringType,
                        pureResolver: Resolvers.DeprecationReason)
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
                Names.RequiresOptIn,
                type: nonNullStringListType,
                pureResolver: Resolvers.RequiresOptIn));
        }

        return def;
    }

    private static class Resolvers
    {
        public static object Name(IResolverContext context)
            => context.Parent<EnumValue>().Name;

        public static object? Description(IResolverContext context)
            => context.Parent<EnumValue>().Description;

        public static object IsDeprecated(IResolverContext context)
            => context.Parent<EnumValue>().IsDeprecated;

        public static string? DeprecationReason(IResolverContext context)
            => context.Parent<EnumValue>().DeprecationReason;

        public static object AppliedDirectives(IResolverContext context)
            => context.Parent<EnumValue>().Directives
                .Where(t => t.Type.IsPublic)
                .Select(d => d.ToSyntaxNode());

        public static object RequiresOptIn(IResolverContext context)
            => context.Parent<IEnumValue>().Directives
                .Where(t => t.Definition is RequiresOptInDirectiveType)
                .Select(d => d.ToValue<RequiresOptInDirective>().Feature);
    }

    public static class Names
    {
        // ReSharper disable once InconsistentNaming
        public const string __EnumValue = "__EnumValue";
        public const string Name = "name";
        public const string Description = "description";
        public const string IsDeprecated = "isDeprecated";
        public const string DeprecationReason = "deprecationReason";
        public const string AppliedDirectives = "appliedDirectives";
        public const string RequiresOptIn = "requiresOptIn";
    }
}
#pragma warning restore IDE1006 // Naming Styles
