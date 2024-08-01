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
internal sealed class __Type : ObjectType
{
    protected override ObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        var stringType = Create(ScalarNames.String);
        var booleanType = Create(ScalarNames.Boolean);
        var kindType = Parse($"{nameof(__TypeKind)}!");
        var typeType = Create(nameof(__Type));
        var fieldListType = Parse($"[{nameof(__Field)}!]");
        var typeListType = Parse($"[{nameof(__Type)}!]");
        var enumValueListType = Parse($"[{nameof(__EnumValue)}!]");
        var inputValueListType = Parse($"[{nameof(__InputValue)}!]");
        var directiveListType = Parse($"[{nameof(__AppliedDirective)}!]!");

        var def = new ObjectTypeDefinition(
            Names.__Type,
            TypeResources.Type_Description,
            typeof(IType))
        {
            Fields =
            {
                new(Names.Kind, type: kindType, pureResolver: Resolvers.Kind),
                new(Names.Name, type: stringType, pureResolver: Resolvers.Name),
                new(Names.Description, type: stringType, pureResolver: Resolvers.Description),
                new(Names.Fields, type: fieldListType, pureResolver: Resolvers.Fields)
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
                new(Names.Interfaces, type: typeListType, pureResolver: Resolvers.Interfaces),
                new(Names.PossibleTypes, type: typeListType, pureResolver: Resolvers.PossibleTypes),
                new(Names.EnumValues, type: enumValueListType, pureResolver: Resolvers.EnumValues)
                {
                    Arguments =
                    {
                        new()
                        {
                            Name = Names.IncludeDeprecated,
                            Type = booleanType,
                            DefaultValue = BooleanValueNode.False,
                            RuntimeDefaultValue = false,
                        },
                    },
                },
                new(Names.InputFields,
                    type: inputValueListType,
                    pureResolver: Resolvers.InputFields)
                {
                    Arguments =
                    {
                        new()
                        {
                            Name = Names.IncludeDeprecated,
                            Type = booleanType,
                            DefaultValue = BooleanValueNode.False,
                            RuntimeDefaultValue = false,
                        },
                    },
                },
                new(Names.OfType, type: typeType, pureResolver: Resolvers.OfType),
                new(Names.SpecifiedByUrl,
                    TypeResources.Type_SpecifiedByUrl_Description,
                    stringType,
                    pureResolver: Resolvers.SpecifiedBy),
            },
        };

        if (context.DescriptorContext.Options.EnableOneOf)
        {
            def.Fields.Add(new(Names.OneOf,
                type: booleanType,
                pureResolver: Resolvers.OneOf));
        }

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
        public static object Kind(IResolverContext context)
            => context.Parent<IType>().Kind;

        public static object? Name(IResolverContext context)
            => context.Parent<IType>() is INamedType n ? n.Name : null;

        public static object? Description(IResolverContext context)
            => context.Parent<IType>() is INamedType n ? n.Description : null;

        public static object? Fields(IResolverContext context)
        {
            var type = context.Parent<IType>();
            var includeDeprecated = context.ArgumentValue<bool>(Names.IncludeDeprecated);

            if (type is IComplexOutputType ct)
            {
                return !includeDeprecated
                    ? ct.Fields.Where(t => !t.IsIntrospectionField && !t.IsDeprecated)
                    : ct.Fields.Where(t => !t.IsIntrospectionField);
            }

            return default;
        }

        public static object? Interfaces(IResolverContext context)
            => context.Parent<IType>() is IComplexOutputType complexType
                ? complexType.Implements
                : null;

        public static object? PossibleTypes(IResolverContext context)
            => context.Parent<IType>() is INamedType nt
                ? nt.IsAbstractType()
                    ? context.Schema.GetPossibleTypes(nt)
                    : null
                : null;

        public static object? EnumValues(IResolverContext context)
            => context.Parent<IType>() is EnumType et
                ? context.ArgumentValue<bool>(Names.IncludeDeprecated)
                    ? et.Values
                    : et.Values.Where(t => !t.IsDeprecated)
                : null;

        public static object? InputFields(IResolverContext context)
            => context.Parent<IType>() is IInputObjectType iot
                ? context.ArgumentValue<bool>(Names.IncludeDeprecated)
                    ? iot.Fields
                    : iot.Fields.Where(t => !t.IsDeprecated)
                : null;

        public static object? OfType(IResolverContext context)
            => context.Parent<IType>() switch
            {
                ListType lt => lt.ElementType,
                NonNullType nnt => nnt.Type,
                _ => null,
            };

        public static object? OneOf(IResolverContext context)
            => context.Parent<IType>() is IInputObjectType iot
                ? iot.Directives.ContainsDirective(WellKnownDirectives.OneOf)
                : null;

        public static object? SpecifiedBy(IResolverContext context)
            => context.Parent<IType>() is ScalarType scalar
                ? scalar.SpecifiedBy?.ToString()
                : null;

        public static object AppliedDirectives(IResolverContext context) =>
            context.Parent<IType>() is IHasDirectives hasDirectives
                ? hasDirectives.Directives.Where(t => t.Type.IsPublic).Select(d => d.AsSyntaxNode())
                : [];
    }

    public static class Names
    {
        // ReSharper disable once InconsistentNaming
        public const string __Type = "__Type";
        public const string Kind = "kind";
        public const string Name = "name";
        public const string Description = "description";
        public const string Fields = "fields";
        public const string Interfaces = "interfaces";
        public const string PossibleTypes = "possibleTypes";
        public const string EnumValues = "enumValues";
        public const string InputFields = "inputFields";
        public const string OfType = "ofType";
        public const string OneOf = "oneOf";
        public const string SpecifiedByUrl = "specifiedByURL";
        public const string IncludeDeprecated = "includeDeprecated";
        public const string AppliedDirectives = "appliedDirectives";
    }
}
#pragma warning restore IDE1006 // Naming Styles
