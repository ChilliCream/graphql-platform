#pragma warning disable IDE1006 // Naming Styles
using System.Runtime.CompilerServices;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Types.Descriptors.TypeReference;

#nullable enable

namespace HotChocolate.Types.Introspection;

// ReSharper disable once InconsistentNaming
[Introspection]
internal sealed class __Field : ObjectType<IOutputFieldDefinition>
{
    protected override ObjectTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
    {
        var stringType = Create(ScalarNames.String);
        var nonNullStringType = Parse($"{ScalarNames.String}!");
        var nonNullTypeType = Parse($"{nameof(__Type)}!");
        var nonNullBooleanType = Parse($"{ScalarNames.Boolean}!");
        var argumentListType = Parse($"[{nameof(__InputValue)}!]!");
        var directiveListType = Parse($"[{nameof(__AppliedDirective)}!]!");

        var def = new ObjectTypeConfiguration(
            Names.__Field,
            TypeResources.Field_Description,
            typeof(IOutputFieldDefinition))
        {
            Fields =
            {
                new(Names.Name, type: nonNullStringType, pureResolver: Resolvers.Name),
                new(Names.Description, type: stringType, pureResolver: Resolvers.Description),
                new(Names.Args, type: argumentListType, pureResolver: Resolvers.Arguments)
                {
                    Arguments =
                    {
                        new(Names.IncludeDeprecated, type: nonNullBooleanType)
                        {
                            DefaultValue = BooleanValueNode.False,
                            RuntimeDefaultValue = false
                        }
                    }
                },
                new(Names.Type, type: nonNullTypeType, pureResolver: Resolvers.Type),
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
            def.Fields.Add(new(Names.AppliedDirectives,
                type: directiveListType,
                pureResolver: Resolvers.AppliedDirectives));
        }

        return def;
    }

    private static class Resolvers
    {
        public static string Name(IResolverContext context)
            => context.Parent<IOutputFieldDefinition>().Name;

        public static string? Description(IResolverContext context)
            => context.Parent<IOutputFieldDefinition>().Description;

        public static object Arguments(IResolverContext context)
        {
            var field = context.Parent<IOutputFieldDefinition>();
            return context.ArgumentValue<bool>(Names.IncludeDeprecated)
                ? field.Arguments
                : field.Arguments.Where(t => !t.IsDeprecated);
        }

        public static IType Type(IResolverContext context)
            => context.Parent<IOutputFieldDefinition>().Type;

        public static object IsDeprecated(IResolverContext context)
            => context.Parent<IOutputFieldDefinition>().IsDeprecated;

        public static string? DeprecationReason(IResolverContext context)
            => context.Parent<IOutputFieldDefinition>().DeprecationReason;

        public static object AppliedDirectives(IResolverContext context) =>
            context.Parent<IOutputFieldDefinition>()
                .Directives
                .Where(t => Unsafe.As<DirectiveType>(t.Definition).IsPublic)
                .Select(d => d.ToSyntaxNode());
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
