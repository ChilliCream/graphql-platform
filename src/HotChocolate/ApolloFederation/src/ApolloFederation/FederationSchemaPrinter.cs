using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;

namespace HotChocolate.ApolloFederation
{
    public static class FederationSchemaPrinter
    {
        public static string Print(ISchema schema)
        {
            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            return SerializeSchema(schema).ToString();
        }

        private static DocumentNode SerializeSchema(
            ISchema schema)
        {
            var referenced = new ReferencedTypes();

            var typeDefinitions = GetNonScalarTypes(schema)
                .Select(
                    t => SerializeNonScalarTypeDefinition(
                        t,
                        referenced))
                .Where(node => node != null)
                .OfType<IDefinitionNode>()
                .ToList();

            return new DocumentNode(
                null,
                typeDefinitions);
        }

        private static IEnumerable<INamedType> GetNonScalarTypes(
            ISchema schema)
        {
            return schema.Types
                .Where(t => IsPublicAndNoScalar(t))
                .Where(t => !IsApolloTypeAddition(t))
                .OrderBy(
                    t => t.Name.ToString(),
                    StringComparer.Ordinal
                )
                .GroupBy(t => (int)t.Kind)
                .OrderBy(t => t.Key)
                .SelectMany(t => t);
        }

        private static bool IsApolloTypeAddition(INamedType type) =>
            type is EntityType || type is ServiceType;

        private static bool IsPublicAndNoScalar(INamedType type) =>
            !IntrospectionTypes.IsIntrospectionType(type.Name) &&
               !(type is ScalarType);


        private static IDefinitionNode? SerializeNonScalarTypeDefinition(
            INamedType namedType,
            ReferencedTypes referenced)
        {
            switch (namedType)
            {
                case ObjectType type:
                    return SerializeObjectType(
                        type,
                        referenced);

                case InterfaceType type:
                    return SerializeInterfaceType(
                        type,
                        referenced);

                case InputObjectType type:
                    return SerializeInputObjectType(
                        type,
                        referenced);

                case UnionType type:
                    return SerializeUnionType(
                        type,
                        referenced);

                case EnumType type:
                    return SerializeEnumType(
                        type,
                        referenced);

                default:
                    throw new NotSupportedException();
            }
        }

        private static IDefinitionNode? SerializeObjectType(
            ObjectType objectType,
            ReferencedTypes referenced)
        {
            var directives = objectType.Directives
                .Select(
                    t => SerializeDirective(
                        t,
                        referenced))
                .ToList();

            var interfaces = objectType.Interfaces
                .Select(
                    t => SerializeNamedType(
                        t,
                        referenced))
                .ToList();

            var fields = objectType.Fields
                .Where(t => !t.IsIntrospectionField)
                .Where(t => !IsApolloTypeAddition(t.Type.NamedType()))
                .Select(
                    t => SerializeObjectField(
                        t,
                        referenced))
                .ToList();

            if (fields.Count == 0)
            {
                return null;
            }

            if (objectType.ContextData.ContainsKey(WellKnownContextData.ExtendMarker))
            {
                return new ObjectTypeExtensionNode(
                    null,
                    new NameNode(objectType.Name),
                    directives,
                    interfaces,
                    fields);
            }

            return new ObjectTypeDefinitionNode(
                null,
                new NameNode(objectType.Name),
                SerializeDescription(objectType.Description),
                directives,
                interfaces,
                fields);
        }

        private static InterfaceTypeDefinitionNode SerializeInterfaceType(
            InterfaceType interfaceType,
            ReferencedTypes referenced)
        {
            var directives = interfaceType.Directives
                .Select(
                    t => SerializeDirective(
                        t,
                        referenced))
                .ToList();

            var fields = interfaceType.Fields
                .Select(
                    t => SerializeObjectField(
                        t,
                        referenced))
                .ToList();

            return new InterfaceTypeDefinitionNode(
                null,
                new NameNode(interfaceType.Name),
                SerializeDescription(interfaceType.Description),
                directives,
                Array.Empty<NamedTypeNode>(),
                fields);
        }

        private static InputObjectTypeDefinitionNode SerializeInputObjectType(
            InputObjectType inputObjectType,
            ReferencedTypes referenced)
        {
            var directives = inputObjectType.Directives
                .Select(
                    t => SerializeDirective(
                        t,
                        referenced))
                .ToList();

            var fields = inputObjectType.Fields
                .Select(
                    t => SerializeInputField(
                        t,
                        referenced))
                .ToList();

            return new InputObjectTypeDefinitionNode(
                null,
                new NameNode(inputObjectType.Name),
                SerializeDescription(inputObjectType.Description),
                directives,
                fields);
        }

        private static UnionTypeDefinitionNode SerializeUnionType(
            UnionType unionType,
            ReferencedTypes referenced)
        {
            var directives = unionType.Directives
                .Select(
                    t => SerializeDirective(
                        t,
                        referenced))
                .ToList();

            var types = unionType.Types.Values
                .Select(
                    t => SerializeNamedType(
                        t,
                        referenced))
                .ToList();

            return new UnionTypeDefinitionNode(
                null,
                new NameNode(unionType.Name),
                SerializeDescription(unionType.Description),
                directives,
                types);
        }

        private static EnumTypeDefinitionNode SerializeEnumType(
            EnumType enumType,
            ReferencedTypes referenced)
        {
            var directives = enumType.Directives
                .Select(
                    t => SerializeDirective(
                        t,
                        referenced))
                .ToList();

            var values = enumType.Values
                .Select(
                    t => SerializeEnumValue(
                        t,
                        referenced))
                .ToList();

            return new EnumTypeDefinitionNode(
                null,
                new NameNode(enumType.Name),
                SerializeDescription(enumType.Description),
                directives,
                values);
        }


        private static EnumValueDefinitionNode SerializeEnumValue(
            IEnumValue enumValue,
            ReferencedTypes referenced)
        {
            var directives = enumValue.Directives
                .Select(
                    t => SerializeDirective(
                        t,
                        referenced))
                .ToList();

            return new EnumValueDefinitionNode(
                null,
                new NameNode(enumValue.Name),
                SerializeDescription(enumValue.Description),
                directives
            );
        }

        private static FieldDefinitionNode SerializeObjectField(
            IOutputField field,
            ReferencedTypes referenced)
        {
            var arguments = field.Arguments
                .Select(
                    t => SerializeInputField(
                        t,
                        referenced))
                .ToList();

            var directives = field.Directives
                .Select(
                    t => SerializeDirective(
                        t,
                        referenced))
                .ToList();

            return new FieldDefinitionNode(
                null,
                new NameNode(field.Name),
                SerializeDescription(field.Description),
                arguments,
                SerializeType(
                    field.Type,
                    referenced),
                directives);
        }

        private static InputValueDefinitionNode SerializeInputField(
            IInputField inputValue,
            ReferencedTypes referenced)
        {
            return new InputValueDefinitionNode(
                null,
                new NameNode(inputValue.Name),
                SerializeDescription(inputValue.Description),
                SerializeType(
                    inputValue.Type,
                    referenced),
                inputValue.DefaultValue,
                inputValue.Directives
                    .Select(
                        t =>
                            SerializeDirective(
                                t,
                                referenced))
                    .ToList()
            );
        }

        private static ITypeNode SerializeType(
            IType type,
            ReferencedTypes referenced)
        {
            if (type is NonNullType nt)
            {
                return new NonNullTypeNode(
                    null,
                    (INullableTypeNode)SerializeType(
                        nt.Type,
                        referenced));
            }

            if (type is ListType lt)
            {
                return new ListTypeNode(
                    null,
                    SerializeType(
                        lt.ElementType,
                        referenced));
            }

            if (type is INamedType namedType)
            {
                return SerializeNamedType(
                    namedType,
                    referenced);
            }

            throw new NotSupportedException();
        }

        private static NamedTypeNode SerializeNamedType(
            INamedType namedType,
            ReferencedTypes referenced)
        {
            referenced.TypeNames.Add(namedType.Name);
            return new NamedTypeNode(
                null,
                new NameNode(namedType.Name));
        }

        private static DirectiveNode SerializeDirective(
            IDirective directiveType,
            ReferencedTypes referenced)
        {
            referenced.DirectiveNames.Add(directiveType.Name);
            return directiveType.ToNode(true);
        }

        private static StringValueNode SerializeDescription(string description)
        {
            return string.IsNullOrEmpty(description)
                ? null
                : new StringValueNode(description);
        }

        private class ReferencedTypes
        {
            public ISet<string> TypeNames { get; } = new HashSet<string>();
            public ISet<string> DirectiveNames { get; } = new HashSet<string>();
        }
    }
}
