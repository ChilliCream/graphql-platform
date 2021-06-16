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
    internal sealed class __Type : ObjectType
    {
        protected override ObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
        {
            SyntaxTypeReference stringType = Create(ScalarNames.String);
            SyntaxTypeReference booleanType = Create(ScalarNames.Boolean);
            SyntaxTypeReference kindType = Parse($"{nameof(__TypeKind)}!");
            SyntaxTypeReference typeType = Create(nameof(__Type));
            SyntaxTypeReference fieldListType = Parse($"[{nameof(__Field)}!]");
            SyntaxTypeReference typeListType = Parse($"[{nameof(__Type)}!]");
            SyntaxTypeReference enumValueListType = Parse($"[{nameof(__EnumValue)}!]");
            SyntaxTypeReference inputValueListType = Parse($"[{nameof(__InputValue)}!]");
            SyntaxTypeReference directiveListType = Parse($"[{nameof(__AppliedDirective)}!]!");

            var def = new ObjectTypeDefinition(Names.__Type, TypeResources.Type_Description)
            {
                Name = Names.__Type,
                Description = TypeResources.Type_Description,
                RuntimeType = typeof(IType),
                Fields =
                {
                    new(Names.Kind,
                        type: kindType,
                        pureResolver: Resolvers.PureKind,
                        inlineResolver: Resolvers.InlineKind),
                    new(Names.Name,
                        type: stringType,
                        pureResolver: Resolvers.PureName,
                        inlineResolver: Resolvers.InlineName),
                    new(Names.Description,
                        type: stringType,
                        pureResolver: Resolvers.PureDescription,
                        inlineResolver: Resolvers.InlineDescription),
                    new(Names.Fields,
                        type: fieldListType,
                        pureResolver: Resolvers.PureFields)
                    {
                        Arguments =
                        {
                            new(Names.IncludeDeprecated, type: booleanType)
                            {
                                DefaultValue = BooleanValueNode.False,
                                RuntimeDefaultValue = false,
                            }
                        }
                    },
                    new(Names.Interfaces,
                        type: typeListType,
                        pureResolver:Resolvers.PureInterfaces,
                        inlineResolver:Resolvers.InlineInterfaces),
                    new(Names.PossibleTypes,
                        type: typeListType,
                        pureResolver: Resolvers.PurePossibleTypes),
                    new(Names.EnumValues, type: enumValueListType)
                    {
                        PureResolver = Resolvers.PureEnumValues,
                        Arguments =
                        {
                            new()
                            {
                                Name = Names.IncludeDeprecated,
                                Type = booleanType,
                                DefaultValue = BooleanValueNode.False,
                                RuntimeDefaultValue = false,
                            }
                        }
                    },
                    new(Names.InputFields,
                        type: inputValueListType,
                        pureResolver: Resolvers.PureInputFields),
                    new()
                    {
                        Name = Names.OfType,
                        Type = typeType,
                        PureResolver = Resolvers.PureOfType
                    },
                    new()
                    {
                        Name = Names.SpecifiedByUrl,
                        Description = TypeResources.Type_SpecifiedByUrl_Description,
                        Type = stringType,
                        PureResolver = Resolvers.SpecifiedBy
                    },

                }
            };

            if (context.DescriptorContext.Options.EnableDirectiveIntrospection)
            {
                def.Fields.Add(new()
                {
                    Name = Names.AppliedDirectives,
                    Type = directiveListType,
                    PureResolver = Resolvers.AppliedDirectives
                });
            }

            return def;
        }

        private static class Resolvers
        {
            public static object PureKind(IResolverContext context)
                => context.Parent<IType>().Kind;

            public static object? InlineKind(object? parent) => ((IType)parent!).Kind;

            public static object? PureName(IResolverContext context)
                => GetName(context.Parent<IType>());

            public static object? InlineName(object? parent)
                => GetName((IType)parent!);

            private static string? GetName(IType type)
                => type is INamedType n ? n.Name.Value : null;

            public static object? PureDescription(IResolverContext context)
                => GetDescription(context.Parent<IType>());

            public static object? InlineDescription(object? parent)
                => GetDescription((IType)parent!);

            private static string? GetDescription(IType type)
                => type is INamedType n ? n.Description : null;

            public static object? PureFields(IResolverContext context)
                => GetFields(
                    context.Parent<IType>(),
                    context.ArgumentValue<bool>(Names.IncludeDeprecated));

            private static IEnumerable<IOutputField>? GetFields(
                IType type,
                bool includeDeprecated)
            {
                if (type is IComplexOutputType ct)
                {
                    return !includeDeprecated
                        ? ct.Fields.Where(t => !t.IsIntrospectionField && !t.IsDeprecated)
                        : ct.Fields.Where(t => !t.IsIntrospectionField);
                }

                return null;
            }

            public static object? PureInterfaces(IResolverContext context)
                => GetInterfaces(context.Parent<IType>());

            public static object? InlineInterfaces(object? parent)
                => GetInterfaces((IType)parent!);

            private static IEnumerable<IInterfaceType>? GetInterfaces(IType type)
                => type is IComplexOutputType complexType ? complexType.Implements : null;

            public static object? PurePossibleTypes(IResolverContext context)
                => GetPossibleTypes(context.Schema, context.Parent<INamedType>());

            private static IEnumerable<IType>? GetPossibleTypes(ISchema schema, INamedType type)
                => type.IsAbstractType() ? schema.GetPossibleTypes(type) : null;

            public static object? PureEnumValues(IResolverContext context)
                => GetEnumValues(
                    context.Parent<IType>(),
                    context.ArgumentValue<bool>(Names.IncludeDeprecated));

            private static IEnumerable<IEnumValue>? GetEnumValues(
                IType type,
                bool includeDeprecated)
            {
                return type is EnumType et
                    ? includeDeprecated ? et.Values : et.Values.Where(t => !t.IsDeprecated)
                    : null;
            }

            public static object? PureInputFields(IResolverContext context)
                => GetInputFields(context.Parent<IType>());

            private static IEnumerable<InputField>? GetInputFields(IType type)
                => type is InputObjectType iot ? iot.Fields : null;

            public static object? PureOfType(IResolverContext context)
                => GetOfType(context.Parent<IType>());

            private static IType? GetOfType(IType type)
                => type switch
                {
                    ListType lt => lt.ElementType,
                    NonNullType nnt => nnt.Type,
                    _ => null
                };

            public static object? SpecifiedBy(IResolverContext context)
                => GetSpecifiedBy(context.Parent<IType>());

            private static string? GetSpecifiedBy(IType type)
                => type is ScalarType scalar
                    ? scalar.SpecifiedBy?.ToString()
                    : null;

            public static object AppliedDirectives(IResolverContext context)
                => GetAppliedDirectives(context.Parent<IType>());

            private static IEnumerable<DirectiveNode> GetAppliedDirectives(IType type) =>
                type is IHasDirectives hasDirectives
                    ? hasDirectives.Directives.Where(t => t.Type.IsPublic).Select(d => d.ToNode())
                    : Enumerable.Empty<DirectiveNode>();
        }

        public static class Names
        {
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
            public const string SpecifiedByUrl = "specifiedByURL";
            public const string IncludeDeprecated = "includeDeprecated";
            public const string AppliedDirectives = "appliedDirectives";
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
