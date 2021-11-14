#pragma warning disable IDE1006 // Naming Styles
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.Descriptors.TypeReference;

#nullable enable

namespace HotChocolate.Types.Introspection;

[Introspection]
internal sealed class __Field : ObjectType<IOutputField>
{
    protected override ObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        SyntaxTypeReference stringType = Create(ScalarNames.String);
        SyntaxTypeReference nonNullStringType = Parse($"{ScalarNames.String}!");
        SyntaxTypeReference nonNullTypeType = Parse($"{nameof(__Type)}!");
        SyntaxTypeReference nonNullBooleanType = Parse($"{ScalarNames.Boolean}!");
        SyntaxTypeReference argumentListType = Parse($"[{nameof(__InputValue)}!]!");
        SyntaxTypeReference directiveListType = Parse($"[{nameof(__AppliedDirective)}!]!");

        var def = new ObjectTypeDefinition(
            Names.__Field,
            TypeResources.Field_Description,
            typeof(IOutputField))
        {

            Fields =
                {
                    new(Names.Name, type: nonNullStringType, pureResolver: Resolvers.Name),
                    new(Names.Description, type: stringType, pureResolver: Resolvers.Description),
                    new(Names.Args, type: argumentListType, pureResolver: Resolvers.Arguments),
                    new(Names.Type, type: nonNullTypeType, pureResolver: Resolvers.Type),
                    new(Names.IsDeprecated, type: nonNullBooleanType,
                        pureResolver: Resolvers.IsDeprecated),
                    new(Names.DeprecationReason, type: stringType,
                        pureResolver: Resolvers.DeprecationReason),
                }
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
        public static string Name(IPureResolverContext context)
            => context.Parent<IOutputField>().Name.Value;

        public static string? Description(IPureResolverContext context)
            => context.Parent<IOutputField>().Description;

        public static IFieldCollection<IInputField> Arguments(IPureResolverContext context)
            => context.Parent<IOutputField>().Arguments;

        public static IType Type(IPureResolverContext context)
            => context.Parent<IOutputField>().Type;

        public static object IsDeprecated(IPureResolverContext context)
            => context.Parent<IOutputField>().IsDeprecated;

        public static string? DeprecationReason(IPureResolverContext context)
            => context.Parent<IOutputField>().DeprecationReason;

        public static object AppliedDirectives(IPureResolverContext context) =>
            context.Parent<IOutputField>().Directives
                .Where(t => t.Type.IsPublic)
                .Select(d => d.ToNode());
    }

    public static class Names
    {
        public const string __Field = "__Field";
        public const string Name = "name";
        public const string Description = "description";
        public const string Args = "args";
        public const string Type = "type";
        public const string IsDeprecated = "isDeprecated";
        public const string DeprecationReason = "deprecationReason";
        public const string AppliedDirectives = "appliedDirectives";
    }
}
#pragma warning restore IDE1006 // Naming Styles
