using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
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

            DocumentNode document = SerializeSchema(schema);
            return document.Print();
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

            DocumentNode document = SerializeSchema(schema);
            textWriter.Write(document.Print());
        }

        public static async ValueTask SerializeAsync(
            ISchema schema,
            Stream stream,
            bool indented = true,
            CancellationToken cancellationToken = default)
        {
            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            DocumentNode document = SerializeSchema(schema);
            await document.PrintToAsync(stream, indented, cancellationToken).ConfigureAwait(false);
        }

        public static DocumentNode SerializeSchema(
            ISchema schema,
            bool includeSpecScalars = false,
            bool printResolverKind = false)
        {
            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            var typeDefinitions = GetNonScalarTypes(schema)
                .Select(t => SerializeNonScalarTypeDefinition(t, printResolverKind))
                .OfType<IDefinitionNode>()
                .ToList();

            if (schema.QueryType is not null
                || schema.MutationType is not null
                || schema.SubscriptionType is not null)
            {
                typeDefinitions.Insert(0, SerializeSchemaTypeDefinition(schema));
            }

            var builtInDirectives = new HashSet<NameString>
            {
                WellKnownDirectives.Skip,
                WellKnownDirectives.Include,
                WellKnownDirectives.Deprecated
            };

            IEnumerable<DirectiveDefinitionNode> directiveTypeDefinitions =
                schema.DirectiveTypes
                    .Where(directive => !builtInDirectives.Contains(directive.Name))
                .OrderBy(t => t.Name.ToString(), StringComparer.Ordinal)
                .Select(SerializeDirectiveTypeDefinition);

            typeDefinitions.AddRange(directiveTypeDefinitions);

            IEnumerable<ScalarTypeDefinitionNode> scalarTypeDefinitions =
                schema.Types
                .OfType<ScalarType>()
                .Where(t => includeSpecScalars || !BuiltInTypes.IsBuiltInType(t.Name))
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

        private static SchemaDefinitionNode SerializeSchemaTypeDefinition(ISchema schema)
        {
            var operations = new List<OperationTypeDefinitionNode>();

            if (schema.QueryType is not null)
            {
                operations.Add(SerializeOperationType(
                    schema.QueryType,
                    OperationType.Query));
            }

            if (schema.MutationType is not null)
            {
                operations.Add(SerializeOperationType(
                    schema.MutationType,
                    OperationType.Mutation));
            }

            if (schema.SubscriptionType is not null)
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
            return new(
                null,
                operation,
                SerializeNamedType(type));
        }

        private static ITypeDefinitionNode SerializeNonScalarTypeDefinition(
            INamedType namedType,
            bool printResolverKind) =>
            namedType switch
            {
                ObjectType type => SerializeObjectType(type, printResolverKind),
                InterfaceType type => SerializeInterfaceType(type),
                InputObjectType type => SerializeInputObjectType(type),
                UnionType type => SerializeUnionType(type),
                EnumType type => SerializeEnumType(type),
                _ => throw new NotSupportedException()
            };

        private static ObjectTypeDefinitionNode SerializeObjectType(
            ObjectType objectType,
            bool printResolverKind)
        {
            var directives = objectType.Directives
                .Select(SerializeDirective)
                .ToList();

            var interfaces = objectType.Implements
                .Select(SerializeNamedType)
                .ToList();

            var fields = objectType.Fields
                .Where(t => !t.IsIntrospectionField)
                .Select(f => SerializeObjectField(f, printResolverKind))
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

            var interfaces = interfaceType.Implements
                .Select(SerializeNamedType)
                .ToList();

            var fields = interfaceType.Fields
                .Select(SerializeInterfaceField)
                .ToList();

            return new InterfaceTypeDefinitionNode
            (
                null,
                new NameNode(interfaceType.Name),
                SerializeDescription(interfaceType.Description),
                directives,
                interfaces,
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
            var directives = scalarType.Directives
                .Select(SerializeDirective)
                .ToList();

            return new(
                null,
                new NameNode(scalarType.Name),
                SerializeDescription(scalarType.Description),
                directives);
        }

        private static FieldDefinitionNode SerializeObjectField(
            ObjectField field,
            bool printResolverKind)
        {
            var arguments = field.Arguments
                .Select(SerializeInputField)
                .ToList();

            var directives = field.Directives
                .Select(SerializeDirective)
                .ToList();

            if (printResolverKind && field.PureResolver is not null)
            {
                directives.Add(new DirectiveNode("pureResolver"));
            }

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

        private static FieldDefinitionNode SerializeInterfaceField(
            InterfaceField field)
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
            IInputField inputValue) =>
            new(
                null,
                new NameNode(inputValue.Name),
                SerializeDescription(inputValue.Description),
                SerializeType(inputValue.Type),
                inputValue.DefaultValue,
                inputValue.Directives.Select(SerializeDirective).ToList());

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

        private static NamedTypeNode SerializeNamedType(INamedType namedType) =>
            new(null, new NameNode(namedType.Name));

        private static DirectiveNode SerializeDirective(IDirective directiveType) =>
            directiveType.ToNode(true);

        private static StringValueNode SerializeDescription(string description) =>
            string.IsNullOrEmpty(description)
                ? null
                : new StringValueNode(description);
    }
}
