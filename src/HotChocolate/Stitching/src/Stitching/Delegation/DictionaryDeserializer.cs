using System.Collections;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Execution.PathFactory;

namespace HotChocolate.Stitching.Delegation;

internal static class DictionaryDeserializer
{
    public static object? DeserializeResult(
        IType fieldType,
        object? obj,
        InputParser parser,
        Path path)
    {
        var namedType = fieldType.NamedType();

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

        return obj is NullValueNode
            ? null
            : obj;
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
                            Instance.Append(path, i++)));
                }

                return deserializedList;
            }

            case ListValueNode listLiteral:
            {
                var elementType = (IInputType)inputType.ElementType();
                var list = new List<object?>();

                var i = 0;

                foreach (var item in listLiteral.Items)
                {
                    list.Add(
                        DeserializeEnumResult(
                            elementType,
                            item,
                            parser,
                            Instance.Append(path, i++)));
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
                var elementType = inputType;
                var runtimeType = typeof(List<object>);

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
                            Instance.Append(path, i++)));
                }

                return deserializedList;
            }

            case ListValueNode listLiteral:
            {
                var elementType = (IInputType)inputType.ElementType();
                var list = new List<object?>();

                var i = 0;

                foreach (var item in listLiteral.Items)
                {
                    list.Add(
                        DeserializeScalarResult(
                            elementType,
                            item,
                            parser,
                            Instance.Append(path, i++)));
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
