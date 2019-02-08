using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Stitching.Introspection.Models;
using Newtonsoft.Json;

namespace HotChocolate.Stitching.Introspection
{
    public static class IntrospectionDeserializer
    {
        public static DocumentNode Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentException(
                    "The json mustn't be null or empty.",
                    nameof(json));
            }

            IntrospectionResult result = JsonConvert
                .DeserializeObject<IntrospectionResult>(json);

            var typeDefinitions = new List<IDefinitionNode>();
            typeDefinitions.AddRange(CreateTypes(result.Data.Schema.Types));

            return new DocumentNode(typeDefinitions);
        }

        private static IEnumerable<ITypeDefinitionNode> CreateTypes(
            ICollection<FullType> types)
        {
            foreach (FullType type in types)
            {
                yield return CreateTypes(type);
            }
        }


        private static ITypeDefinitionNode CreateTypes(FullType type)
        {
            switch (type.Kind)
            {
                case TypeKind.Enum:
                    return CreateEnumType(type);

                case TypeKind.Input_Object:
                    return CreateInputObject(type);

                case TypeKind.Interface:
                    return CreateInterface(type);

                case TypeKind.Object:
                    return CreateObject(type);

                case TypeKind.Scalar:
                    return CreateScalar(type);

                case TypeKind.Union:
                    return CreateUnion(type);

                default:
                    throw new NotSupportedException(
                        "The specified type is not supported.");
            }
        }

        private static EnumTypeDefinitionNode CreateEnumType(FullType type)
        {
            return new EnumTypeDefinitionNode
            (
                null,
                new NameNode(type.Name),
                CreateDescription(type.Description),
                Array.Empty<DirectiveNode>(),
                CreateEnumValues(type.EnumValues)
            );
        }

        private static IReadOnlyList<EnumValueDefinitionNode> CreateEnumValues(
            IEnumerable<EnumValue> enumValues)
        {
            var values = new List<EnumValueDefinitionNode>();

            foreach (EnumValue value in enumValues)
            {

                values.Add(new EnumValueDefinitionNode
                (
                    null,
                    new NameNode(value.Name),
                    CreateDescription(value.Description),
                    CreateDirectives(
                        value.IsDepricated,
                        value.DeprecationReason)
                ));
            }

            return values;
        }

        private static InputObjectTypeDefinitionNode CreateInputObject(
            FullType type)
        {
            return new InputObjectTypeDefinitionNode
            (
                null,
                new NameNode(type.Name),
                CreateDescription(type.Description),
                Array.Empty<DirectiveNode>(),
                CreateInputVals(type.InputFields)
            );
        }

        private static IReadOnlyList<InputValueDefinitionNode> CreateInputVals(
            IEnumerable<InputField> fields)
        {
            var list = new List<InputValueDefinitionNode>();

            foreach (InputField field in fields)
            {
                list.Add(new InputValueDefinitionNode
                (
                    null,
                    new NameNode(field.Name),
                    CreateDescription(field.Description),
                    CreateTypeReference(field.Type),
                    NullValueNode.Default, // TODO : create a default value deserializer
                    Array.Empty<DirectiveNode>()
                ));
            }

            return list;
        }

        private static ITypeNode CreateTypeReference(TypeRef typeRef)
        {
            if (typeRef.Kind == TypeKind.Non_Null)
            {
                return new NonNullTypeNode
                (
                    (INullableTypeNode)CreateTypeReference(typeRef.OfType)
                );
            }

            if (typeRef.Kind == TypeKind.List)
            {
                return new ListTypeNode
                (
                    CreateTypeReference(typeRef.OfType)
                );
            }

            return new NamedTypeNode(new NameNode(typeRef.Name));
        }

        private static InterfaceTypeDefinitionNode CreateInterface(
            FullType type)
        {
            return new InterfaceTypeDefinitionNode
            (
                null,
                new NameNode(type.Name),
                CreateDescription(type.Description),
                Array.Empty<DirectiveNode>(),
                CreateFields(type.Fields)
            );
        }

        private static ObjectTypeDefinitionNode CreateObject(
            FullType type)
        {
            return new ObjectTypeDefinitionNode
            (
                null,
                new NameNode(type.Name),
                CreateDescription(type.Description),
                Array.Empty<DirectiveNode>(),
                CreateNamedTypeRefs(type.Interfaces),
                CreateFields(type.Fields)
            );
        }

        private static IReadOnlyList<FieldDefinitionNode> CreateFields(
            IEnumerable<Field> fields)
        {
            var list = new List<FieldDefinitionNode>();

            foreach (Field field in fields)
            {
                list.Add(new FieldDefinitionNode
                (
                    null,
                    new NameNode(field.Name),
                    CreateDescription(field.Description),
                    CreateInputVals(field.Args),
                    CreateTypeReference(field.Type),
                    CreateDirectives(
                        field.IsDepricated,
                        field.DeprecationReason)
                ));
            }
            return list;
        }

        private static UnionTypeDefinitionNode CreateUnion(
            FullType type)
        {
            return new UnionTypeDefinitionNode
            (
                null,
                new NameNode(type.Name),
                CreateDescription(type.Description),
                Array.Empty<DirectiveNode>(),
                CreateNamedTypeRefs(type.PossibleTypes)
            );
        }

        private static ScalarTypeDefinitionNode CreateScalar(
            FullType type)
        {
            return new ScalarTypeDefinitionNode
            (
                null,
                new NameNode(type.Name),
                CreateDescription(type.Description),
                Array.Empty<DirectiveNode>()
            );
        }

        private static IReadOnlyList<NamedTypeNode> CreateNamedTypeRefs(
            IEnumerable<TypeRef> interfaces)
        {
            var list = new List<NamedTypeNode>();

            foreach (TypeRef typeRef in interfaces)
            {
                list.Add(new NamedTypeNode(new NameNode(typeRef.Name)));
            }

            return list;
        }

        private static IReadOnlyList<DirectiveNode> CreateDirectives(
            bool isDepricated, string deprecationReason)
        {
            if (isDepricated)
            {
                return new List<DirectiveNode>
                {
                    new DirectiveNode
                    (
                        WellKnownDirectives.Deprecated,
                        new ArgumentNode
                        (
                            WellKnownDirectives.DeprecationReasonArgument,
                            new StringValueNode(deprecationReason)
                        )
                    )
                };
            }
            return Array.Empty<DirectiveNode>();
        }

        private static StringValueNode CreateDescription(string description)
        {
            return string.IsNullOrEmpty(description)
                ? null
                : new StringValueNode(description);
        }
    }
}
