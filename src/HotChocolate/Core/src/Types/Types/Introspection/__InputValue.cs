#pragma warning disable IDE1006 // Naming Styles
using System.Runtime.CompilerServices;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Types.Descriptors.TypeReference;

namespace HotChocolate.Types.Introspection;

[Introspection]
// ReSharper disable once InconsistentNaming
internal sealed class __InputValue : ObjectType
{
    protected override ObjectTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
    {
        var stringType = Create(ScalarNames.String);
        var nonNullStringType = Parse($"{ScalarNames.String}!");
        var nonNullTypeType = Parse($"{nameof(__Type)}!");
        var nonNullBooleanType = Parse($"{ScalarNames.Boolean}!");
        var appDirectiveListType = Parse($"[{nameof(__AppliedDirective)}!]!");

        var def = new ObjectTypeConfiguration(
            Names.__InputValue,
            InputValue_Description,
            typeof(IInputValueDefinition))
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

        return def;
    }

    private static class Resolvers
    {
        public static object Name(IResolverContext context)
            => context.Parent<IInputValueDefinition>().Name;

        public static object? Description(IResolverContext context)
            => context.Parent<IInputValueDefinition>().Description;

        public static object Type(IResolverContext context)
            => context.Parent<IInputValueDefinition>().Type;

        public static object IsDeprecated(IResolverContext context)
            => context.Parent<IInputValueDefinition>().IsDeprecated;

        public static object? DeprecationReason(IResolverContext context)
            => context.Parent<IInputValueDefinition>().DeprecationReason;

        public static object? DefaultValue(IResolverContext context)
        {
            var field = context.Parent<IInputValueDefinition>();
            return field.DefaultValue.IsNull() ? null : field.DefaultValue!.ToString(indented: false);
        }

        public static object AppliedDirectives(IResolverContext context)
            => context.Parent<IInputValueDefinition>()
                .Directives
                .Where(t => Unsafe.As<DirectiveType>(t.Definition).IsPublic)
                .Select(d => d.ToSyntaxNode());
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
