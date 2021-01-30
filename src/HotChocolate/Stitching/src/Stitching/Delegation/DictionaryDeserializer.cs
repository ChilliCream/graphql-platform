using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Delegation
{
    internal static class DictionaryDeserializer
    {
        public static object? DeserializeResult(
            IType fieldType,
            object? obj)
        {
            INamedType namedType = fieldType.NamedType();

            if (namedType is IInputType && fieldType is IInputType inputType)
            {
                if (namedType.Kind == TypeKind.Enum)
                {
                    return DeserializeEnumResult(inputType, obj);
                }

                if (namedType.Kind == TypeKind.Scalar)
                {
                    return DeserializeScalarResult(inputType, obj);
                }
            }

            return obj is NullValueNode ? null : obj;
        }

        private static object? DeserializeEnumResult(IInputType inputType, object? value)
        {
            switch (value)
            {
                case IReadOnlyList<object> list:
                {
                    var elementType = (IInputType)inputType.ElementType();
                    var deserializedList = (IList)Activator.CreateInstance(inputType.RuntimeType)!;

                    foreach (object? item in list)
                    {
                        deserializedList.Add(DeserializeEnumResult(elementType, item));
                    }

                    return deserializedList;
                }

                case ListValueNode listLiteral:
                {
                    var elementType = (IInputType)inputType.ElementType();
                    var list = new List<object?>();

                    foreach (IValueNode item in listLiteral.Items)
                    {
                        list.Add(DeserializeEnumResult(elementType, item));
                    }

                    return list;
                }

                case StringValueNode stringLiteral:
                    return inputType.Deserialize(stringLiteral.Value);

                case IValueNode literal:
                    return inputType.ParseLiteral(literal);

                default:
                    return inputType.Deserialize(value);
            }
        }

        private static object? DeserializeScalarResult(IInputType inputType, object? value)
        {
            switch (value)
            {
                case IReadOnlyList<object> list:
                {
                    IInputType elementType = inputType;
                    Type runtimeType = typeof(List<object>);
                    if (inputType.IsListType())
                    {
                        elementType = (IInputType)inputType.ElementType();
                        runtimeType = inputType.RuntimeType;
                    }

                    var deserializedList =
                        (IList)Activator.CreateInstance(runtimeType)!;

                    foreach (object? item in list)
                    {
                        deserializedList.Add(DeserializeScalarResult(elementType, item));
                    }

                    return deserializedList;
                }

                case ListValueNode listLiteral:
                {
                    var elementType = (IInputType)inputType.ElementType();
                    var list = new List<object?>();

                    foreach (IValueNode item in listLiteral.Items)
                    {
                        list.Add(DeserializeScalarResult(elementType, item));
                    }

                    return list;
                }

                case IValueNode literal:
                    return inputType.ParseLiteral(literal);

                default:
                    return inputType.Deserialize(value);
            }
        }
    }
}
