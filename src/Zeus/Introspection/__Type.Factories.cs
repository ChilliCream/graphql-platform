using System;
using System.Collections.Generic;
using System.Linq;
using Zeus.Abstractions;

namespace Zeus.Introspection
{
    internal partial class __Type
    {
        public static IEnumerable<__Type> CreateScalarTypes()
        {
            yield return new __Type(__TypeKind.Scalar, ScalarTypes.Boolean, null, null, null);
            yield return new __Type(__TypeKind.Scalar, ScalarTypes.Float, null, null, null);
            yield return new __Type(__TypeKind.Scalar, ScalarTypes.ID, null, null, null);
            yield return new __Type(__TypeKind.Scalar, ScalarTypes.Integer, null, null, null);
            yield return new __Type(__TypeKind.Scalar, ScalarTypes.String, null, null, null);
        }

        public static __Type CreateType(ISchema schema, IType type)
        {
            if (type.IsNonNullType())
            {
                if (type.IsListType())
                {
                    return CreateNonNullListType(schema, type);
                }
                return CreateNonNullNamedType(schema, type);
            }

            if (type.IsListType())
            {
                return CreateListType(schema, type);
            }

            return CreateFromNamedType(schema, type.NamedType());
        }

        private static __Type CreateNonNullListType(ISchema schema, IType type)
        {
            return new __Type(__TypeKind.NonNull,
                CreateListType(schema, type.InnerType()));
        }

        private static __Type CreateListType(ISchema schema, IType type)
        {
            if (type.IsNonNullElementType())
            {
                return new __Type(__TypeKind.List,
                    CreateNonNullNamedType(schema, type.ElementType()));
            }
            else
            {
                return new __Type(__TypeKind.List,
                    CreateFromNamedType(schema, type.NamedType()));
            }
        }

        private static __Type CreateNonNullNamedType(ISchema schema, IType type)
        {
            return new __Type(__TypeKind.NonNull,
                CreateFromNamedType(schema, type.NamedType()));
        }

        private static __Type CreateFromNamedType(ISchema schema, NamedType namedType)
        {
            if (namedType.IsScalarType())
            {
                return new __Type(__TypeKind.Scalar, namedType.Name, null, null, null);
            }

            ITypeDefinition typeDefinition = schema.FirstOrDefault(t => t.Name == namedType.Name);
            return CreateType(typeDefinition);
        }

        public static __Type CreateType(ITypeDefinition typeDefinition)
        {
            if (typeDefinition is InterfaceTypeDefinition itd)
            {
                return CreateInterfaceType(itd);
            }

            if (typeDefinition is ObjectTypeDefinition otd)
            {
                return CreateObjectType(otd);
            }

            if (typeDefinition is UnionTypeDefinition utd)
            {
                return CreateUnionType(utd);
            }

            if (typeDefinition is EnumTypeDefinition etd)
            {
                return CreateEnumType(etd);
            }

            if (typeDefinition is InputObjectTypeDefinition iotd)
            {
                return CreateInputObjectType(iotd);
            }

            throw new NotSupportedException();
        }

        private static __Type CreateObjectType(ObjectTypeDefinition objectType)
        {
            if (objectType == null)
            {
                throw new System.ArgumentNullException(nameof(objectType));
            }

            return new __Type(__TypeKind.Object,
                objectType.Name, null,
                CreateFields(objectType.Fields.Values),
                objectType.Interfaces.Select(t => new NamedType(t)));
        }

        private static __Type CreateInterfaceType(InterfaceTypeDefinition interfaceType)
        {
            if (interfaceType == null)
            {
                throw new System.ArgumentNullException(nameof(interfaceType));
            }

            return new __Type(__TypeKind.Interface,
                interfaceType.Name, null,
                CreateFields(interfaceType.Fields.Values),
                null);
        }

        public static __Type CreateUnionType(UnionTypeDefinition unionType)
        {
            if (unionType == null)
            {
                throw new System.ArgumentNullException(nameof(unionType));
            }

            return new __Type(__TypeKind.Union, unionType.Name, null, null, null);
        }

        public static __Type CreateEnumType(EnumTypeDefinition enumType)
        {
            if (enumType == null)
            {
                throw new System.ArgumentNullException(nameof(enumType));
            }

            return new __Type(__TypeKind.Enum, enumType.Name, null, null, null);
        }

        public static __Type CreateInputObjectType(InputObjectTypeDefinition inputObjectType)
        {
            if (inputObjectType == null)
            {
                throw new System.ArgumentNullException(nameof(inputObjectType));
            }

            return new __Type(__TypeKind.InputObject, inputObjectType.Name, null, null, null);
        }

        private static IEnumerable<__Field> CreateFields(
            IEnumerable<FieldDefinition> fields)
        {
            foreach (var field in fields)
            {
                yield return new __Field(field.Name, null,
                    CreateInputValues(field.Arguments.Values),
                    field.Type, false, null);
            }
        }

        private static IEnumerable<__InputValue> CreateInputValues(
            IEnumerable<InputValueDefinition> inputValues)
        {
            foreach (var inputValue in inputValues)
            {
                yield return new __InputValue(inputValue.Name, null,
                    inputValue.Type, inputValue.DefaultValue.ToString());
            }
        }
    }
}