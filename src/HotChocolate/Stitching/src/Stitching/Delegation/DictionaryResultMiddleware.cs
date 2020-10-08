using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Delegation
{
    public class DictionaryResultMiddleware
    {
        private readonly FieldDelegate _next;

        public DictionaryResultMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public ValueTask InvokeAsync(IMiddlewareContext context)
        {
            if (context.Result is SerializedData s)
            {
                context.Result = s.Data is IDictionary<string, object> d
                    ? d
                    : DeserializeResult(context.Field, s.Data);
            }
            else if (context.Result is null &&
                !context.Field.Directives.Contains(DirectiveNames.Computed) &&
                context.Parent<object>() is IDictionary<string, object> dict)
            {
                string responseName = context.FieldSelection.Alias == null
                    ? context.FieldSelection.Name.Value
                    : context.FieldSelection.Alias.Value;

                dict.TryGetValue(responseName, out object? obj);
                context.Result = DeserializeResult(context.Field, obj);
            }

            return _next.Invoke(context);
        }

        private static object? DeserializeResult(
            IOutputField field,
            object? obj)
        {
            INamedType namedType = field.Type.NamedType();

            if (field.Type is IInputType inputType)
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

            return obj;
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

                case IValueNode literal:
                    return inputType.ParseLiteral(literal);

                default:
                    return inputType.Deserialize(value);
            }
        }
    }
}
