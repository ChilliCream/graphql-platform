using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities.Introspection;

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
            using var stringWriter = new StringWriter(sb);
            using var documentWriter = new DocumentWriter(stringWriter);
            DocumentNode document = SerializeSchema(schema);
            var serializer = new SchemaSyntaxSerializer(true);
            serializer.Visit(document, documentWriter);
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

            using var documentWriter = new DocumentWriter(textWriter);
            DocumentNode document = SerializeSchema(schema);
            var serializer = new SchemaSyntaxSerializer(true);
            serializer.Visit(document, documentWriter);
        }

        public static DocumentNode SerializeSchema(
            ISchema schema)
        {
            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            var typeDefinitions = GetNonScalarTypes(schema)
                .Select(SerializeNonScalarTypeDefinition)
                .OfType<IDefinitionNode>()
                .ToList();

            if (schema.QueryType != null
                || schema.MutationType != null
                || schema.SubscriptionType != null)
            {
                typeDefinitions.Insert(0, SerializeSchemaTypeDefinition(schema));
            }

            IEnumerable<DirectiveDefinitionNode> directiveTypeDefinitions =
                schema.DirectiveTypes
                .OrderBy(t => t.Name.ToString(), StringComparer.Ordinal)
                .Select(SerializeDirectiveTypeDefinition);

            typeDefinitions.AddRange(directiveTypeDefinitions);

            IEnumerable<ScalarTypeDefinitionNode> scalarTypeDefinitions =
                schema.Types
                .OfType<ScalarType>()
                .Where(t => !BuiltInTypes.IsBuiltInType(t.Name))
                .OrderBy(t => t.Name.ToString(), StringComparer.Ordinal)
                .Select(SerializeScalarType);

            typeDefinitions.AddRange(scalarTypeDefinitions);

            return new DocumentNode(null, typeDefinitions);
        }

        private static IEnumerable<INamedType> GetNonScalarTypes(
            ISchema schema)
        {
            return schema.Types
               .Where(IsPublicAndNoScalar)
               .OrderBy(t => t.Name.ToString(), StringComparer.Ordinal)
               .GroupBy(t => (int)t.Kind)
               .OrderBy(t => t.Key)
               .SelectMany(t => t);
        }

        private static bool IsPublicAndNoScalar(INamedType type)
        {
            if (type.IsIntrospectionType() || type is ScalarType)
            {
                return false;
            }

            return true;
        }

        private static DirectiveDefinitionNode SerializeDirectiveTypeDefinition(
            DirectiveType directiveType)
        {
            var arguments = directiveType.Arguments
                .Select(SerializeInputField)
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
            ISchema schema)
        {
            var operations = new List<OperationTypeDefinitionNode>();

            if (schema.QueryType != null)
            {
                operations.Add(SerializeOperationType(
                    schema.QueryType,
                    OperationType.Query));
            }

            if (schema.MutationType != null)
            {
                operations.Add(SerializeOperationType(
                    schema.MutationType,
                    OperationType.Mutation));
            }

            if (schema.SubscriptionType != null)
            {
                operations.Add(SerializeOperationType(
                    schema.SubscriptionType,
                    OperationType.Subscription));
            }

            var directives = schema.Directives
                .Select(SerializeDirective)
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
           OperationType operation)
        {
            return new OperationTypeDefinitionNode
            (
                null,
                operation,
                SerializeNamedType(type)
            );
        }

        private static ITypeDefinitionNode SerializeNonScalarTypeDefinition(
            INamedType namedType) =>
            namedType switch
            {
                ObjectType type => SerializeObjectType(type),
                InterfaceType type => SerializeInterfaceType(type),
                InputObjectType type => SerializeInputObjectType(type),
                UnionType type => SerializeUnionType(type),
                EnumType type => SerializeEnumType(type),
                _ => throw new NotSupportedException()
            };

        private static ObjectTypeDefinitionNode SerializeObjectType(
            ObjectType objectType)
        {
            var directives = objectType.Directives
                .Select(SerializeDirective)
                .ToList();

            var interfaces = objectType.Interfaces
                .Select(SerializeNamedType)
                .ToList();

            var fields = objectType.Fields
                .Where(t => !t.IsIntrospectionField)
                .Select(SerializeObjectField)
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
            InterfaceType interfaceType)
        {
            var directives = interfaceType.Directives
                .Select(SerializeDirective)
                .ToList();

            var fields = interfaceType.Fields
                .Select(SerializeObjectField)
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
            InputObjectType inputObjectType)
        {
            var directives = inputObjectType.Directives
                .Select(SerializeDirective)
                .ToList();

            var fields = inputObjectType.Fields
                .Select(SerializeInputField)
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

        private static UnionTypeDefinitionNode SerializeUnionType(UnionType unionType)
        {
            var directives = unionType.Directives
                .Select(SerializeDirective)
                .ToList();

            var types = unionType.Types.Values
                .Select(SerializeNamedType)
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

        private static EnumTypeDefinitionNode SerializeEnumType(EnumType enumType)
        {
            var directives = enumType.Directives
                .Select(SerializeDirective)
                .ToList();

            var values = enumType.Values
                .Select(SerializeEnumValue)
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


        private static EnumValueDefinitionNode SerializeEnumValue(IEnumValue enumValue)
        {
            var directives = enumValue.Directives
                .Select(SerializeDirective)
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

        private static FieldDefinitionNode SerializeObjectField(IOutputField field)
        {
            var arguments = field.Arguments
                .Select(SerializeInputField)
                .ToList();

            var directives = field.Directives
                .Select(SerializeDirective)
                .ToList();

            return new FieldDefinitionNode
            (
                null,
                new NameNode(field.Name),
                SerializeDescription(field.Description),
                arguments,
                SerializeType(field.Type),
                directives
            );
        }

        private static InputValueDefinitionNode SerializeInputField(
            IInputField inputValue)
        {
            return new InputValueDefinitionNode
            (
                null,
                new NameNode(inputValue.Name),
                SerializeDescription(inputValue.Description),
                SerializeType(inputValue.Type),
                inputValue.DefaultValue,
                inputValue.Directives.Select(SerializeDirective).ToList()
            );
        }

        private static ITypeNode SerializeType(IType type)
        {
            if (type is NonNullType nt)
            {
                return new NonNullTypeNode(null, (INullableTypeNode)SerializeType(nt.Type));
            }

            if (type is ListType lt)
            {
                return new ListTypeNode(null, SerializeType(lt.ElementType));
            }

            if (type is INamedType namedType)
            {
                return SerializeNamedType(namedType);
            }

            throw new NotSupportedException();
        }

        private static NamedTypeNode SerializeNamedType(INamedType namedType)
        {
            return new NamedTypeNode(null, new NameNode(namedType.Name));
        }

        private static DirectiveNode SerializeDirective(IDirective directiveType)
        {
            return directiveType.ToNode(true);
        }

        private static StringValueNode SerializeDescription(string description)
        {
            return string.IsNullOrEmpty(description)
                ? null
                : new StringValueNode(description);
        }
    }
}
