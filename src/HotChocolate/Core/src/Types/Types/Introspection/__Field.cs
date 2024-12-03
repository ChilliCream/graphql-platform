#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.Descriptors.TypeReference;

#nullable enable

namespace HotChocolate.Types.Introspection;

[Introspection]
// ReSharper disable once InconsistentNaming
internal sealed class __Field : ObjectType<IOutputField>
{
    protected override ObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        var stringType = Create(ScalarNames.String);
        var nonNullStringType = Parse($"{ScalarNames.String}!");
        var nonNullTypeType = Parse($"{nameof(__Type)}!");
        var nonNullBooleanType = Parse($"{ScalarNames.Boolean}!");
        var booleanType = Parse($"{ScalarNames.Boolean}");
        var argumentListType = Parse($"[{nameof(__InputValue)}!]!");
        var directiveListType = Parse($"[{nameof(__AppliedDirective)}!]!");

        var def = new ObjectTypeDefinition(
            Names.__Field,
            TypeResources.Field_Description,
            typeof(IOutputField))
        {
            Fields =
            {
                new(Names.Name, type: nonNullStringType, pureResolver: Resolvers.Name),
                new(Names.Description, type: stringType, pureResolver: Resolvers.Description),
                new(Names.Args, type: argumentListType, pureResolver: Resolvers.Arguments)
                {
                    Arguments =
                    {
                        new(Names.IncludeDeprecated, type: booleanType)
                        {
                            DefaultValue = BooleanValueNode.False,
                            RuntimeDefaultValue = false,
                        },
                    },
                },
                new(Names.Type, type: nonNullTypeType, pureResolver: Resolvers.Type),
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
            def.Fields.Add(new(Names.AppliedDirectives,
                type: directiveListType,
                pureResolver: Resolvers.AppliedDirectives));
        }

        return def;
    }

    private static class Resolvers
    {
        public static string Name(IResolverContext context)
            => context.Parent<IOutputField>().Name;

        public static string? Description(IResolverContext context)
            => context.Parent<IOutputField>().Description;

        public static object Arguments(IResolverContext context)
        {
            var field = context.Parent<IOutputField>();
            return context.ArgumentValue<bool>(Names.IncludeDeprecated)
                ? field.Arguments
                : field.Arguments.Where(t => !t.IsDeprecated);
        }

        public static IType Type(IResolverContext context)
            => context.Parent<IOutputField>().Type;

        public static object IsDeprecated(IResolverContext context)
            => context.Parent<IOutputField>().IsDeprecated;

        public static string? DeprecationReason(IResolverContext context)
            => context.Parent<IOutputField>().DeprecationReason;

        public static object AppliedDirectives(IResolverContext context) =>
            context.Parent<IOutputField>()
                .Directives
                .Where(t => t.Type.IsPublic)
                .Select(d => d.AsSyntaxNode());
    }

    public static class Names
    {
        // ReSharper disable once InconsistentNaming
        public const string __Field = "__Field";
        public const string Name = "name";
        public const string Description = "description";
        public const string Args = "args";
        public const string Type = "type";
        public const string IsDeprecated = "isDeprecated";
        public const string IncludeDeprecated = "includeDeprecated";
        public const string DeprecationReason = "deprecationReason";
        public const string AppliedDirectives = "appliedDirectives";
    }
}
#pragma warning restore IDE1006 // Naming Styles
