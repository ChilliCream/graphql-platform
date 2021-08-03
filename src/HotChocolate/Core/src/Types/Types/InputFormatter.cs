using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using ThrowHelper = HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Types
{
    public class InputFormatter
    {
        public IValueNode FormatLiteral(object? runtimeValue, IType type, Path path)
        {
            if (runtimeValue is null)
            {
                if (type.Kind == TypeKind.NonNull)
                {
                    throw ThrowHelper.NonNullInputViolation(type, path);
                }

                return NullValueNode.Default;
            }

            switch (type.Kind)
            {
                case TypeKind.NonNull:
                    return FormatLiteral(runtimeValue, ((NonNullType)type).Type, path);

                case TypeKind.List:
                    return FormatLiteralList((IList)runtimeValue, (ListType)type, path);

                case TypeKind.InputObject:
                    return FormatLiteralObject(runtimeValue, (InputObjectType)type, path);

                case TypeKind.Enum:
                case TypeKind.Scalar:
                    return FormatLiteralLeaf(runtimeValue, (ILeafType)type, path);

                default:
                    throw new NotSupportedException();
            }
        }

        private ObjectValueNode FormatLiteralObject(object runtimeValue, InputObjectType type, Path path)
        {
            var fields = new List<ObjectFieldNode>();
            var fieldValues = new object?[type.Fields.Count];
            type.GetFieldValues(runtimeValue, fieldValues);

            for (var i = 0; i < fieldValues.Length; i++)
            {
                InputField field = type.Fields[i];
                object? fieldValue = fieldValues[i];
                Path fieldPath = path.Append(field.Name);

                if (field.IsOptional)
                {
                    IOptional optional = ((IOptional)fieldValue!);
                    if (optional.HasValue)
                    {
                        AddField(optional.Value, field.Name, field.Type, fieldPath);
                    }
                }
                else
                {
                    AddField(fieldValue, field.Name, field.Type, fieldPath);
                }
            }

            return new ObjectValueNode(fields);

            void AddField(object? fieldValue, NameString fieldName, IInputType fieldType, Path fieldPath)
            {
                IValueNode value = FormatLiteral(fieldValue, fieldType, fieldPath);
                fields.Add(new ObjectFieldNode(fieldName, value));
            }
        }

        private ListValueNode FormatLiteralList(IList runtimeValue, ListType type, Path path)
        {
            var items = new List<IValueNode>();

            for (var i = 0; i < runtimeValue.Count; i++)
            {
                items.Add(FormatLiteral(runtimeValue[i], type.ElementType, path.Append(i)));
            }

            return new ListValueNode(items);
        }

        private IValueNode FormatLiteralLeaf(object runtimeValue, ILeafType type, Path path)
        {
            try
            {
                return type.ParseValue(runtimeValue);
            }
            catch (SerializationException ex)
            {
                throw new SerializationException(ex.Errors[0], ex.Type, path);
            }
        }

        public IValueNode FormatResult(object? resultValue, IType type, Path path)
        {
            if (resultValue is null)
            {
                if (type.Kind == TypeKind.NonNull)
                {
                    throw ThrowHelper.NonNullInputViolation(type, path);
                }

                return NullValueNode.Default;
            }

            switch (type.Kind)
            {
                case TypeKind.NonNull:
                    return FormatResult(resultValue, ((NonNullType)type).Type, path);

                case TypeKind.List:
                    return FormatResultList((IList)resultValue, (ListType)type, path);

                case TypeKind.InputObject:
                    return FormatResultObject((IReadOnlyDictionary<string, object?>)resultValue, (InputObjectType)type, path);

                case TypeKind.Enum:
                case TypeKind.Scalar:
                    return FormatResultLeaf(resultValue, (ILeafType)type, path);

                default:
                    throw new NotSupportedException();
            }
        }

        private ObjectValueNode FormatResultObject(IReadOnlyDictionary<string, object?> resultValue, InputObjectType type, Path path)
        {
            var fields = new List<ObjectFieldNode>();
            var processed = 0;

            foreach (var field in type.Fields)
            {
                if (resultValue.TryGetValue(field.Name, out var fieldValue))
                {
                    IValueNode value = FormatResult(fieldValue, field.Type, path);
                    fields.Add(new ObjectFieldNode(field.Name, value));
                    processed++;
                }
            }

            if (processed < resultValue.Count)
            {
                var invalidFieldNames = new List<string>();

                foreach (KeyValuePair<string, object?> item in resultValue)
                {
                    if (!type.Fields.ContainsField(item.Key))
                    {
                        invalidFieldNames.Add(item.Key);
                    }
                }

                throw ThrowHelper.InvalidInputFieldNames(type, invalidFieldNames, path);
            }

            return new ObjectValueNode(fields);
        }

        private ListValueNode FormatResultList(IList runtimeValue, ListType type, Path path)
        {
            var items = new List<IValueNode>();

            for (var i = 0; i < runtimeValue.Count; i++)
            {
                items.Add(FormatResult(runtimeValue[i], type.ElementType, path.Append(i)));
            }

            return new ListValueNode(items);
        }

        private IValueNode FormatResultLeaf(object resultValue, ILeafType type, Path path)
        {
            try
            {
                return type.ParseResult(resultValue);
            }
            catch (SerializationException ex)
            {
                throw new SerializationException(ex.Errors[0], ex.Type, path);
            }
        }
    }
}
