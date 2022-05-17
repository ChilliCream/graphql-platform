using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Types;

public class InputFormatter
{
    private readonly ITypeConverter _converter;

    public InputFormatter() : this(new DefaultTypeConverter()) { }

    public InputFormatter(ITypeConverter converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    public IValueNode FormatValue(object? runtimeValue, IType type, Path? path = null)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return FormatValueInternal(runtimeValue, type, path ?? Path.Root);
    }

    private IValueNode FormatValueInternal(object? runtimeValue, IType type, Path path)
    {
        if (runtimeValue is null)
        {
            if (type.Kind == TypeKind.NonNull)
            {
                throw NonNullInputViolation(type, path);
            }

            return NullValueNode.Default;
        }

        switch (type.Kind)
        {
            case TypeKind.NonNull:
                return FormatValueInternal(runtimeValue, ((NonNullType)type).Type, path);

            case TypeKind.List:
                return FormatValueList(runtimeValue, (ListType)type, path);

            case TypeKind.InputObject:
                return FormatValueObject(runtimeValue, (InputObjectType)type, path);

            case TypeKind.Enum:
            case TypeKind.Scalar:
                return FormatValueLeaf(runtimeValue, (ILeafType)type, path);

            default:
                throw new NotSupportedException();
        }
    }

    private ObjectValueNode FormatValueObject(
        object runtimeValue,
        InputObjectType type,
        Path path)
    {
        var fields = new List<ObjectFieldNode>();
        var fieldValues = new object?[type.Fields.Count];
        type.GetFieldValues(runtimeValue, fieldValues);

        for (var i = 0; i < fieldValues.Length; i++)
        {
            InputField field = type.Fields[i];
            var fieldValue = fieldValues[i];
            Path fieldPath = PathFactory.Instance.Append(path, field.Name);

            if (field.IsOptional)
            {
                IOptional optional = (IOptional)fieldValue!;
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

        void AddField(
            object? fieldValue,
            NameString fieldName,
            IInputType fieldType,
            Path fieldPath)
        {
            IValueNode value = FormatValueInternal(fieldValue, fieldType, fieldPath);
            fields.Add(new ObjectFieldNode(fieldName, value));
        }
    }

    private ListValueNode FormatValueList(object runtimeValue, ListType type, Path path)
    {
        if (runtimeValue is IList runtimeList)
        {
            var items = new List<IValueNode>();

            for (var i = 0; i < runtimeList.Count; i++)
            {
                Path newPath = PathFactory.Instance.Append(path, i);
                items.Add(
                    FormatValueInternal(runtimeList[i], type.ElementType, newPath));
            }

            return new ListValueNode(items);
        }

        if (runtimeValue is IEnumerable enumerable)
        {
            var items = new List<IValueNode>();
            var i = 0;

            foreach (var item in enumerable)
            {
                Path newPath = PathFactory.Instance.Append(path, i);
                items.Add(FormatValueInternal(item, type.ElementType, newPath));
            }

            return new ListValueNode(items);
        }

        throw FormatValueList_InvalidObjectKind(type, runtimeValue.GetType(), path);
    }

    private IValueNode FormatValueLeaf(object runtimeValue, ILeafType type, Path path)
    {
        try
        {
            if (runtimeValue.GetType() != type.RuntimeType &&
                _converter.TryConvert(type.RuntimeType, runtimeValue, out var converted))
            {
                runtimeValue = converted;
            }

            return type.ParseValue(runtimeValue);
        }
        catch (SerializationException ex)
        {
            throw new SerializationException(ex.Errors[0], ex.Type, path);
        }
    }

    public IValueNode FormatResult(object? resultValue, IType type, Path? path = null)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return FormatResultInternal(resultValue, type, path ?? Path.Root);
    }

    private IValueNode FormatResultInternal(object? resultValue, IType type, Path path)
    {
        if (resultValue is null or NullValueNode)
        {
            if (type.Kind == TypeKind.NonNull)
            {
                throw NonNullInputViolation(type, path);
            }

            return NullValueNode.Default;
        }

        switch (type.Kind)
        {
            case TypeKind.NonNull:
                return FormatResultInternal(resultValue, ((NonNullType)type).Type, path);

            case TypeKind.List:
                return FormatResultList(resultValue, (ListType)type, path);

            case TypeKind.InputObject:
                return FormatResultObject(resultValue, (InputObjectType)type, path);

            case TypeKind.Enum:
            case TypeKind.Scalar:
                return FormatResultLeaf(resultValue, (ILeafType)type, path);

            default:
                throw new NotSupportedException();
        }
    }

    private ObjectValueNode FormatResultObject(
        object resultValue,
        InputObjectType type,
        Path path)
    {
        if (resultValue is IReadOnlyDictionary<string, object?> map)
        {
            var fields = new List<ObjectFieldNode>();
            var processed = 0;

            foreach (InputField? field in type.Fields)
            {
                if (map.TryGetValue(field.Name, out var fieldValue))
                {
                    IValueNode value = FormatResultInternal(fieldValue, field.Type, path);
                    fields.Add(new ObjectFieldNode(field.Name, value));
                    processed++;
                }
            }

            if (processed < map.Count)
            {
                var invalidFieldNames = new List<string>();

                foreach (KeyValuePair<string, object?> item in map)
                {
                    if (!type.Fields.ContainsField(item.Key))
                    {
                        invalidFieldNames.Add(item.Key);
                    }
                }

                throw InvalidInputFieldNames(type, invalidFieldNames, path);
            }

            return new ObjectValueNode(fields);
        }

        if (resultValue is ObjectValueNode node)
        {
            return node;
        }

        if (type.RuntimeType != typeof(object) &&
            type.RuntimeType.IsInstanceOfType(resultValue))
        {
            return FormatValueObject(resultValue, type, path);
        }

        throw FormatResultObject_InvalidObjectKind(type, resultValue.GetType(), path);
    }

    private ListValueNode FormatResultList(object resultValue, ListType type, Path path)
    {
        if (resultValue is IList resultList)
        {
            var items = new List<IValueNode>();

            for (var i = 0; i < resultList.Count; i++)
            {
                Path newPath = PathFactory.Instance.Append(path, i);
                items.Add(FormatResultInternal(resultList[i], type.ElementType, newPath));
            }

            return new ListValueNode(items);
        }

        if (resultValue is ListValueNode node)
        {
            return node;
        }

        throw FormatResultList_InvalidObjectKind(type, resultValue.GetType(), path);
    }

    private IValueNode FormatResultLeaf(object resultValue, ILeafType type, Path path)
    {
        if (resultValue is IValueNode node)
        {
            if (type.IsInstanceOfType(node))
            {
                return node;
            }

            throw FormatResultLeaf_InvalidSyntaxKind(type, node.Kind, path);
        }

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
