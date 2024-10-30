#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Types.Descriptors.TypeReference;

#nullable enable

namespace HotChocolate.Types.Introspection;

[Introspection]
// ReSharper disable once InconsistentNaming
internal sealed class __InputValue : ObjectType
{
    protected override ObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        var stringType = Create(ScalarNames.String);
        var nonNullStringType = Parse($"{ScalarNames.String}!");
        var nonNullTypeType = Parse($"{nameof(__Type)}!");
        var nonNullBooleanType = Parse($"{ScalarNames.Boolean}!");
        var appDirectiveListType = Parse($"[{nameof(__AppliedDirective)}!]!");

        var def = new ObjectTypeDefinition(
            Names.__InputValue,
            InputValue_Description,
            typeof(IInputField))
        {
            Fields =
            {
                new(Names.Name, type: nonNullStringType, pureResolver: Resolvers.Name),
                new(Names.Description, type: stringType, pureResolver: Resolvers.Description),
                new(Names.Type, type: nonNullTypeType, pureResolver: Resolvers.Type),
                new(Names.DefaultValue,
                    InputValue_DefaultValue,
                    stringType,
                    pureResolver: Resolvers.DefaultValue),
                new(Names.IsDeprecated,
                    type: nonNullBooleanType,
                    pureResolver: Resolvers.IsDeprecated),
                new(Names.DeprecationReason,
                    type: stringType,
                    pureResolver: Resolvers.DeprecationReason),
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
        public static object Name(IResolverContext context)
            => context.Parent<IInputField>().Name;

        public static object? Description(IResolverContext context)
            => context.Parent<IInputField>().Description;

        public static object Type(IResolverContext context)
            => context.Parent<IInputField>().Type;

        public static object IsDeprecated(IResolverContext context)
            => context.Parent<IInputField>().IsDeprecated;

        public static object? DeprecationReason(IResolverContext context)
            => context.Parent<IInputField>().DeprecationReason;

        public static object? DefaultValue(IResolverContext context)
        {
            var field = context.Parent<IInputField>();
            return field.DefaultValue.IsNull() ? null : field.DefaultValue!.Print();
        }

        public static object AppliedDirectives(IResolverContext context)
            => context.Parent<IInputField>()
                .Directives
                .Where(t => t.Type.IsPublic)
                .Select(d => d.AsSyntaxNode());
    }

    public static class Names
    {
        // ReSharper disable once InconsistentNaming
        public const string __InputValue = "__InputValue";
        public const string Name = "name";
        public const string Description = "description";
        public const string DefaultValue = "defaultValue";
        public const string Type = "type";
        public const string AppliedDirectives = "appliedDirectives";
        public const string IsDeprecated = "isDeprecated";
        public const string DeprecationReason = "deprecationReason";
    }
}
#pragma warning restore IDE1006 // Naming Styles
