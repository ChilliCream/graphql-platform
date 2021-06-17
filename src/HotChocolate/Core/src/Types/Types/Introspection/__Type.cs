#pragma warning disable IDE1006 // Naming Styles
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
                        inlineResolver: Resolvers.Kind),
                    new(Names.Name,
                        type: stringType,
                        inlineResolver: Resolvers.Name),
                    new(Names.Description,
                        type: stringType,
                        inlineResolver: Resolvers.Description),
                    new(Names.Fields,
                        type: fieldListType,
                        pureResolver: Resolvers.Fields)
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
                        inlineResolver:Resolvers.Interfaces),
                    new(Names.PossibleTypes,
                        type: typeListType,
                        pureResolver: Resolvers.PossibleTypes),
                    new(Names.EnumValues,
                        type: enumValueListType,
                        pureResolver:  Resolvers.EnumValues)
                    {
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
                        inlineResolver: Resolvers.InputFields),
                    new(Names.OfType,
                        type: typeType,
                        inlineResolver: Resolvers.OfType),
                    new(Names.SpecifiedByUrl,
                        TypeResources.Type_SpecifiedByUrl_Description,
                        stringType,
                        inlineResolver: Resolvers.SpecifiedBy)
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
            public static object? Kind(object? parent)
                => ((IType)parent!).Kind;

            public static object? Name(object? parent)
                => parent is INamedType n ? n.Name.Value : null;

            public static object? Description(object? parent)
                => parent is INamedType n ? n.Description : null;

            public static object? Fields(IResolverContext context)
            {
                IType type = context.Parent<IType>();
                var includeDeprecated = context.ArgumentValue<bool>(Names.IncludeDeprecated);

                if (type is IComplexOutputType ct)
                {
                    return !includeDeprecated
                        ? ct.Fields.Where(t => !t.IsIntrospectionField && !t.IsDeprecated)
                        : ct.Fields.Where(t => !t.IsIntrospectionField);
                }

                return null;
            }

            public static object? Interfaces(object? parent)
                => parent is IComplexOutputType complexType ? complexType.Implements : null;

            public static object? PossibleTypes(IResolverContext context)
            {
                ISchema schema = context.Schema;
                INamedType type = context.Parent<INamedType>();
                return type.IsAbstractType() ? schema.GetPossibleTypes(type) : null;
            }

            public static object? EnumValues(IResolverContext context)
                => context.Parent<IType>() is EnumType et
                    ? context.ArgumentValue<bool>(Names.IncludeDeprecated)
                        ? et.Values
                        : et.Values.Where(t => !t.IsDeprecated)
                    : null;

            public static object? InputFields(object? parent)
                => parent is IInputObjectType iot ? iot.Fields : null;

            public static object? OfType(object? parent)
                => parent switch
                {
                    ListType lt => lt.ElementType,
                    NonNullType nnt => nnt.Type,
                    _ => null
                };

            public static object? SpecifiedBy(object? parent)
                => parent is ScalarType scalar
                    ? scalar.SpecifiedBy?.ToString()
                    : null;

            public static object AppliedDirectives(object? parent) =>
                parent is IHasDirectives hasDirectives
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
