using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    public static class SchemaSerializer
    {
        public static string Serialize(ISchema schema)
        {
            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            var sb = new StringBuilder();
            using (var stringWriter = new StringWriter(sb))
            using (var documentWriter = new DocumentWriter(stringWriter))
            {
                DocumentNode document = SerializeSchema(schema);
                var serializer = new SchemaSyntaxSerializer(true);
                serializer.Visit(document, documentWriter);
            }
            return sb.ToString();
        }

        public static void Serialize(ISchema schema, TextWriter textWriter)
        {
            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (textWriter is null)
            {
                throw new ArgumentNullException(nameof(textWriter));
            }

            using (var documentWriter = new DocumentWriter(textWriter))
            {
                DocumentNode document = SerializeSchema(schema);
                var serializer = new SchemaSyntaxSerializer(true);
                serializer.Visit(document, documentWriter);
            }
        }

        public static DocumentNode SerializeSchema(
            ISchema schema)
        {
            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            var referenced = new ReferencedTypes();

            var typeDefinitions = GetNonScalarTypes(schema)
                .Select(t => SerializeNonScalarTypeDefinition(t, referenced))
                .OfType<IDefinitionNode>()
                .ToList();

            if (schema.QueryType != null
                || schema.MutationType != null
                || schema.SubscriptionType != null)
            {
                typeDefinitions.Insert(0,
                    SerializeSchemaTypeDefinition(schema, referenced));
            }

            IEnumerable<DirectiveDefinitionNode> directiveTypeDefinitions =
                schema.DirectiveTypes
                .Where(t => referenced.DirectiveNames.Contains(t.Name))
                .OrderBy(t => t.Name.ToString(), StringComparer.Ordinal)
                .Select(t => SerializeDirectiveTypeDefinition(t, referenced));

            typeDefinitions.AddRange(directiveTypeDefinitions);

            IEnumerable<ScalarTypeDefinitionNode> scalarTypeDefinitions =
                schema.Types
                .OfType<ScalarType>()
                .Where(t => referenced.TypeNames.Contains(t.Name))
                .OrderBy(t => t.Name.ToString(), StringComparer.Ordinal)
                .Select(t => SerializeScalarType(t));

            typeDefinitions.AddRange(scalarTypeDefinitions);

            return new DocumentNode(null, typeDefinitions);
        }

        private static IEnumerable<INamedType> GetNonScalarTypes(
            ISchema schema)
        {
            return schema.Types
               .Where(t => IsPublicAndNoScalar(t))
               .OrderBy(t => t.Name.ToString(), StringComparer.Ordinal)
               .GroupBy(t => (int)t.Kind)
               .OrderBy(t => t.Key)
               .SelectMany(t => t);
        }

        private static bool IsPublicAndNoScalar(INamedType type)
        {
            if (type.IsIntrospectionType()
                || type is ScalarType scalarType)
            {
                return false;
            }

            return true;
        }

        private static DirectiveDefinitionNode SerializeDirectiveTypeDefinition(
            DirectiveType directiveType,
            ReferencedTypes referenced)
        {
            var arguments = directiveType.Arguments
               .Select(t => SerializeInputField(t, referenced))
               .ToList();

            var locations = directiveType.Locations
                .Select(l => new NameNode(l.MapDirectiveLocation().ToString()))
                .ToList();

            return new DirectiveDefinitionNode
            (
                null,
                new NameNode(directiveType.Name),
                SerializeDescription(directiveType.Description),
                directiveType.IsRepeatable,
                arguments,
                locations
            );
        }

        private static SchemaDefinitionNode SerializeSchemaTypeDefinition(
            ISchema schema,
            ReferencedTypes referenced)
        {
            var operations = new List<OperationTypeDefinitionNode>();

            if (schema.QueryType != null)
            {
                operations.Add(SerializeOperationType(
                    schema.QueryType,
                    OperationType.Query,
                    referenced));
            }

            if (schema.MutationType != null)
            {
                operations.Add(SerializeOperationType(
                    schema.MutationType,
                    OperationType.Mutation,
                    referenced));
            }

            if (schema.SubscriptionType != null)
            {
                operations.Add(SerializeOperationType(
                    schema.SubscriptionType,
                    OperationType.Subscription,
                    referenced));
            }

            var directives = schema.Directives
                .Select(t => SerializeDirective(t, referenced))
                .ToList();

            return new SchemaDefinitionNode
            (
                null,
                SerializeDescription(schema.Description),
                directives,
                operations
            );
        }

        private static OperationTypeDefinitionNode SerializeOperationType(
           ObjectType type,
           OperationType operation,
           ReferencedTypes referenced)
        {
            return new OperationTypeDefinitionNode
            (
                null,
                operation,
                SerializeNamedType(type, referenced)
            );
        }

        private static ITypeDefinitionNode SerializeNonScalarTypeDefinition(
            INamedType namedType,
            ReferencedTypes referenced)
        {
            switch (namedType)
            {
                case ObjectType type:
                    return SerializeObjectType(type, referenced);

                case InterfaceType type:
                    return SerializeInterfaceType(type, referenced);

                case InputObjectType type:
                    return SerializeInputObjectType(type, referenced);

                case UnionType type:
                    return SerializeUnionType(type, referenced);

                case EnumType type:
                    return SerializeEnumType(type, referenced);

                default:
                    throw new NotSupportedException();
            }
        }

        private static ObjectTypeDefinitionNode SerializeObjectType(
            ObjectType objectType,
            ReferencedTypes referenced)
        {
            var directives = objectType.Directives
                .Select(t => SerializeDirective(t, referenced))
                .ToList();

            var interfaces = objectType.Interfaces
                .Select(t => SerializeNamedType(t, referenced))
                .ToList();

            var fields = objectType.Fields
                .Where(t => !t.IsIntrospectionField)
                .Select(t => SerializeObjectField(t, referenced))
                .ToList();

            return new ObjectTypeDefinitionNode
            (
                null,
                new NameNode(objectType.Name),
                SerializeDescription(objectType.Description),
                directives,
                interfaces,
                fields
            );
        }

        private static InterfaceTypeDefinitionNode SerializeInterfaceType(
            InterfaceType interfaceType,
            ReferencedTypes referenced)
        {
            var directives = interfaceType.Directives
                .Select(t => SerializeDirective(t, referenced))
                .ToList();

            var fields = interfaceType.Fields
                .Select(t => SerializeObjectField(t, referenced))
                .ToList();

            return new InterfaceTypeDefinitionNode
            (
                null,
                new NameNode(interfaceType.Name),
                SerializeDescription(interfaceType.Description),
                directives,
                Array.Empty<NamedTypeNode>(),
                fields
            );
        }

        private static InputObjectTypeDefinitionNode SerializeInputObjectType(
            InputObjectType inputObjectType,
            ReferencedTypes referenced)
        {
            var directives = inputObjectType.Directives
                .Select(t => SerializeDirective(t, referenced))
                .ToList();

            var fields = inputObjectType.Fields
                .Select(t => SerializeInputField(t, referenced))
                .ToList();

            return new InputObjectTypeDefinitionNode
            (
                null,
                new NameNode(inputObjectType.Name),
                SerializeDescription(inputObjectType.Description),
                directives,
                fields
            );
        }

        private static UnionTypeDefinitionNode SerializeUnionType(
            UnionType unionType,
            ReferencedTypes referenced)
        {
            var directives = unionType.Directives
                .Select(t => SerializeDirective(t, referenced))
                .ToList();

            var types = unionType.Types.Values
                .Select(t => SerializeNamedType(t, referenced))
                .ToList();

            return new UnionTypeDefinitionNode
            (
                null,
                new NameNode(unionType.Name),
                SerializeDescription(unionType.Description),
                directives,
                types
            );
        }

        private static EnumTypeDefinitionNode SerializeEnumType(
            EnumType enumType,
            ReferencedTypes referenced)
        {
            var directives = enumType.Directives
                .Select(t => SerializeDirective(t, referenced))
                .ToList();

            var values = enumType.Values
                .Select(t => SerializeEnumValue(t, referenced))
                .ToList();

            return new EnumTypeDefinitionNode
            (
                null,
                new NameNode(enumType.Name),
                SerializeDescription(enumType.Description),
                directives,
                values
            );
        }


        private static EnumValueDefinitionNode SerializeEnumValue(
            IEnumValue enumValue,
            ReferencedTypes referenced)
        {
            var directives = enumValue.Directives
                .Select(t => SerializeDirective(t, referenced))
                .ToList();

            return new EnumValueDefinitionNode
            (
                null,
                new NameNode(enumValue.Name),
                SerializeDescription(enumValue.Description),
                directives
            );
        }

        private static ScalarTypeDefinitionNode SerializeScalarType(
            ScalarType scalarType)
        {
            return new ScalarTypeDefinitionNode
            (
                null,
                new NameNode(scalarType.Name),
                SerializeDescription(scalarType.Description),
                Array.Empty<DirectiveNode>()
            );
        }

        private static FieldDefinitionNode SerializeObjectField(
            IOutputField field,
            ReferencedTypes referenced)
        {
            var arguments = field.Arguments
                .Select(t => SerializeInputField(t, referenced))
                .ToList();

            var directives = field.Directives
                .Select(t => SerializeDirective(t, referenced))
                .ToList();

            return new FieldDefinitionNode
            (
                null,
                new NameNode(field.Name),
                SerializeDescription(field.Description),
                arguments,
                SerializeType(field.Type, referenced),
                directives
            );
        }

        private static InputValueDefinitionNode SerializeInputField(
            IInputField inputValue,
            ReferencedTypes referenced)
        {
            return new InputValueDefinitionNode
            (
                null,
                new NameNode(inputValue.Name),
                SerializeDescription(inputValue.Description),
                SerializeType(inputValue.Type, referenced),
                inputValue.DefaultValue,
                inputValue.Directives.Select(t =>
                    SerializeDirective(t, referenced)).ToList()
            );
        }

        private static ITypeNode SerializeType(
            IType type,
            ReferencedTypes referenced)
        {
            if (type is NonNullType nt)
            {
                return new NonNullTypeNode(null,
                    (INullableTypeNode)SerializeType(
                        nt.Type, referenced));
            }

            if (type is ListType lt)
            {
                return new ListTypeNode(null,
                    SerializeType(lt.ElementType, referenced));
            }

            if (type is INamedType namedType)
            {
                return SerializeNamedType(namedType, referenced);
            }

            throw new NotSupportedException();
        }

        private static NamedTypeNode SerializeNamedType(
            INamedType namedType,
            ReferencedTypes referenced)
        {
            referenced.TypeNames.Add(namedType.Name);
            return new NamedTypeNode(null, new NameNode(namedType.Name));
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
