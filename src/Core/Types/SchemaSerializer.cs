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
            using (var documentWriter = new DocumentWriter(textWriter))
            {
                DocumentNode document = SerializeSchema(schema);
                var serializer = new SchemaSyntaxSerializer(true);
                serializer.Visit(document, documentWriter);
            }
        }

        private static DocumentNode SerializeSchema(
            ISchema schema)
        {
            var referenced = new HashSet<string>();

            var typeDefinitions = GetNonScalarTypes(schema)
                .Select(t => SerializeNonScalarTypeDefinition(t, referenced))
                .OfType<IDefinitionNode>()
                .ToList();

            typeDefinitions.Insert(0,
                SerializeSchemaTypeDefinition(schema, referenced));

            var scalarTypeDefinitions = schema.Types
                .OfType<ScalarType>()
                .Where(t => referenced.Contains(t.Name))
                .Select(t => SerializeScalarType(t));

            typeDefinitions.AddRange(scalarTypeDefinitions);

            return new DocumentNode(null, typeDefinitions);
        }

        private static IEnumerable<INamedType> GetNonScalarTypes(
            ISchema schema)
        {
            return schema.Types
               .Where(t => IsPublicAndNoScalar(t))
               .OrderBy(t => t.Name.ToString())
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

        private static SchemaDefinitionNode SerializeSchemaTypeDefinition(
            ISchema schema,
            HashSet<string> referenced)
        {
            var operations = new List<OperationTypeDefinitionNode>();

            operations.Add(SerializeOperationType(
                schema.QueryType,
                OperationType.Query,
                referenced));

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

            return new SchemaDefinitionNode
            (
                null,
                Array.Empty<DirectiveNode>(),
                operations
            );
        }

        private static OperationTypeDefinitionNode SerializeOperationType(
           ObjectType type,
           OperationType operation,
           HashSet<string> referenced)
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
            HashSet<string> referenced)
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
                    return SerializeEnumType(type);

                default:
                    throw new NotSupportedException();
            }
        }

        private static ObjectTypeDefinitionNode SerializeObjectType(
            ObjectType objectType,
            HashSet<string> referenced)
        {
            var directives = objectType.Directives
                .Select(t => t.ToNode())
                .ToList();

            var interfaces = objectType.Interfaces.Values
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
            HashSet<string> referenced)
        {
            var directives = interfaceType.Directives
                .Select(t => t.ToNode())
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
                fields
            );
        }

        private static InputObjectTypeDefinitionNode SerializeInputObjectType(
            InputObjectType inputObjectType,
            HashSet<string> referenced)
        {
            var directives = inputObjectType.Directives
                .Select(t => t.ToNode())
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
            HashSet<string> referenced)
        {
            var directives = unionType.Directives
                .Select(t => t.ToNode())
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
            EnumType enumType)
        {
            var directives = enumType.Directives
                .Select(t => t.ToNode())
                .ToList();

            var values = enumType.Values
                .Select(t => SerializeEnumValue(t))
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
            EnumValue enumValue)
        {
            return new EnumValueDefinitionNode
            (
                null,
                new NameNode(enumValue.Name),
                SerializeDescription(enumValue.Description),
                Array.Empty<DirectiveNode>()
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
            HashSet<string> referenced)
        {
            var arguments = field.Arguments
                .Select(t => SerializeInputField(t, referenced))
                .ToList();

            var directives = field.Directives
                .Select(t => t.ToNode())
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
            HashSet<string> referenced)
        {
            return new InputValueDefinitionNode
            (
                null,
                new NameNode(inputValue.Name),
                SerializeDescription(inputValue.Description),
                SerializeType(inputValue.Type, referenced),
                inputValue.DefaultValue,
                inputValue.Directives.Select(t => t.ToNode()).ToList()
            );
        }

        private static ITypeNode SerializeType(
            IType type,
            HashSet<string> referenced)
        {
            if (type is NonNullType nt)
            {
                return new NonNullTypeNode(null,
                    (Language.INullableType)SerializeType(nt.Type, referenced));
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
            HashSet<string> referenced)
        {
            referenced.Add(namedType.Name);
            return new NamedTypeNode(null, new NameNode(namedType.Name));
        }

        private static StringValueNode SerializeDescription(string description)
        {
            return string.IsNullOrEmpty(description)
                ? null
                : new StringValueNode(description);
        }
    }
}
