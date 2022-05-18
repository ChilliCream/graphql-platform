using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Processing;

internal static class DictionaryDeserializer
{
    public static object? DeserializeResult(
        IType fieldType,
        object? obj,
        InputParser parser,
        Path path)
    {
        INamedType namedType = fieldType.NamedType();

        if (namedType is IInputType && fieldType is IInputType inputType)
        {
            if (namedType.Kind == TypeKind.Enum)
            {
                return DeserializeEnumResult(inputType, obj, parser, path);
            }

            if (namedType.Kind == TypeKind.Scalar)
            {
                return DeserializeScalarResult(inputType, obj, parser, path);
            }
        }

        return obj is NullValueNode ? null : obj;
    }

    private static object? DeserializeEnumResult(
        IInputType inputType,
        object? value,
        InputParser parser,
        Path path)
    {
        switch (value)
        {
            case IReadOnlyList<object> list:
                {
                    var elementType = (IInputType)inputType.ElementType();
                    var deserializedList = (IList)Activator.CreateInstance(inputType.RuntimeType)!;

                    var i = 0;
                    foreach (var item in list)
                    {
                        deserializedList.Add(
                            DeserializeEnumResult(
                                elementType,
                                item,
                                parser,
                                PathFactory.Instance.Append(path, i++)));
                    }

                    return deserializedList;
                }

            case ListValueNode listLiteral:
                {
                    var elementType = (IInputType)inputType.ElementType();
                    var list = new List<object?>();

                    var i = 0;
                    foreach (IValueNode item in listLiteral.Items)
                    {
                        list.Add(
                            DeserializeEnumResult(
                                elementType,
                                item,
                                parser,
                                PathFactory.Instance.Append(path, i++)));
                    }

                    return list;
                }

            case StringValueNode stringLiteral:
                return parser.ParseResult(stringLiteral.Value, inputType, path);

            case IValueNode literal:
                return parser.ParseLiteral(literal, inputType, path);

            default:
                return parser.ParseResult(value, inputType, path);
        }
    }

    private static object? DeserializeScalarResult(
        IInputType inputType,
        object? value,
        InputParser parser,
        Path path)
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

                    var i = 0;
                    foreach (var item in list)
                    {
                        deserializedList.Add(
                            DeserializeScalarResult(
                                elementType,
                                item,
                                parser,
                                PathFactory.Instance.Append(path, i++)));
                    }

                    return deserializedList;
                }

            case ListValueNode listLiteral:
                {
                    var elementType = (IInputType)inputType.ElementType();
                    var list = new List<object?>();

                    var i = 0;
                    foreach (IValueNode item in listLiteral.Items)
                    {
                        list.Add(
                            DeserializeScalarResult(
                                elementType,
                                item,
                                parser,
                                PathFactory.Instance.Append(path, i++)));
                    }

                    return list;
                }

            case IValueNode literal:
                return parser.ParseLiteral(literal, inputType, path);

            default:
                return parser.ParseResult(value, inputType, path);
        }
    }
}
