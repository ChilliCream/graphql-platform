#pragma warning disable IDE1006 // Naming Styles
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.Expressions;
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
            SyntaxTypeReference kindType = Create(nameof(__TypeKind));
            SyntaxTypeReference typeType = Create(nameof(__Type));
            SyntaxTypeReference fieldListType = Parse($"[{nameof(__Field)}!]");
            SyntaxTypeReference typeListType = Parse($"[{nameof(__Type)}!]");
            SyntaxTypeReference enumValueListType = Parse($"[{nameof(__EnumValue)}!]");
            SyntaxTypeReference inputValueListType = Parse($"[{nameof(__InputValue)}!]");


            var typeDefinition = new ObjectTypeDefinition
            {
                Name = Names.__Type,
                Description = TypeResources.Type_Description,
                RuntimeType = typeof(IType)
            };

            typeDefinition.Fields.Add(new()
            {
                Name = Names.Kind,
                Type = kindType,
                PureResolver = Resolvers.PureKind,
                InlineResolver = Resolvers.InlineKind
            });

            typeDefinition.Fields.Add(new()
            {
                Name = Names.Name,
                Type = stringType,
                PureResolver = Resolvers.PureName,
                InlineResolver = Resolvers.InlineName
            });

            typeDefinition.Fields.Add(new()
            {
                Name = Names.Description,
                Type = stringType,
                PureResolver = Resolvers.PureDescription,
                InlineResolver = Resolvers.InlineDescription
            });

            typeDefinition.Fields.Add(new()
            {
                Name = Names.Fields,
                Type = fieldListType,
                PureResolver = Resolvers.PureFields,
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
            });

            typeDefinition.Fields.Add(new()
            {
                Name = Names.Interfaces,
                Type = typeListType,
                PureResolver = Resolvers.PureInterfaces,
                InlineResolver = Resolvers.InlineInterfaces
            });

            typeDefinition.Fields.Add(new()
            {
                Name = Names.PossibleTypes,
                Type = typeListType,
                PureResolver = Resolvers.PurePossibleTypes
            });

            typeDefinition.Fields.Add(new()
            {
                Name = Names.EnumValues,
                Type = enumValueListType,
                Resolver =  Resolvers.EnumValues,
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
            });

            return typeDefinition;
        }





        protected override void Configure(IObjectTypeDescriptor<IType> descriptor)
        {
            descriptor
                .Field(Names.InputFields)
                .Type<ListType<NonNullType<__InputValue>>>()
                .ResolveWith<Resolvers>(t => t.GetInputFields(default!));

            descriptor
                .Field(Names.OfType)
                .Type<__Type>()
                .ResolveWith<Resolvers>(t => t.GetOfType(default!));

            descriptor
                .Field(Names.SpecifiedByUrl)
                .Description(TypeResources.Type_SpecifiedByUrl_Description)
                .Type<StringType>()
                .ResolveWith<Resolvers>(t => t.GetSpecifiedBy(default!));

            if (descriptor.Extend().Context.Options.EnableDirectiveIntrospection)
            {
                descriptor
                    .Field(Names.AppliedDirectives)
                    .Type<NonNullType<ListType<NonNullType<__AppliedDirective>>>>()
                    .ResolveWith<Resolvers>(t => t.GetAppliedDirectives(default!));
            }
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

            public static ValueTask<object?> EnumValues(IResolverContext context)
                => new(GetEnumValues(
                    context.Parent<IType>(),
                    context.ArgumentValue<bool>(Names.IncludeDeprecated)));

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

            public IEnumerable<InputField>? GetInputFields([Parent] IType type) =>
                type is InputObjectType iot ? iot.Fields : null;

            public IType? GetOfType([Parent] IType type) =>
                type switch
                {
                    ListType lt => lt.ElementType,
                    NonNullType nnt => nnt.Type,
                    _ => null
                };

            public string? GetSpecifiedBy([Parent] IType type) =>
                type is ScalarType scalar
                    ? scalar.SpecifiedBy?.ToString()
                    : null;

            public IEnumerable<DirectiveNode> GetAppliedDirectives([Parent] IType type) =>
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
